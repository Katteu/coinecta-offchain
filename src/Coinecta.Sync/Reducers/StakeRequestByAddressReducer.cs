using CardanoSharp.Wallet.Extensions.Models;
using Coinecta.Data;
using Coinecta.Data.Models.Datums;
using Coinecta.Data.Models.Reducers;
using Cardano.Sync.Reducers;
using Microsoft.EntityFrameworkCore;
using PallasDotnet.Models;
using Address = CardanoSharp.Wallet.Models.Addresses.Address;
using Cardano.Sync.Data.Models.Datums;
using CardanoSharp.Wallet.Utilities;
using CardanoSharp.Wallet.Enums;
using Coinecta.Data.Utils;
using Cardano.Sync.Data.Models;

namespace Coinecta.Sync.Reducers;

public class StakeRequestByAddressReducer(
    IDbContextFactory<CoinectaDbContext> dbContextFactory,
    IConfiguration configuration,
    ILogger<StakeRequestByAddressReducer> logger
) : IReducer
{
    private CoinectaDbContext _dbContext = default!;
    private readonly ILogger<StakeRequestByAddressReducer> _logger = logger;

    public async Task RollBackwardAsync(NextResponse response)
    {
        _dbContext = dbContextFactory.CreateDbContext();
        ulong rollbackSlot = response.Block.Slot;
        _dbContext.StakeRequestByAddresses.RemoveRange(_dbContext.StakeRequestByAddresses.Where(s => s.Slot > rollbackSlot));
        await _dbContext.SaveChangesAsync();
        _dbContext.Dispose();
    }

    public async Task RollForwardAsync(NextResponse response)
    {
        _dbContext = dbContextFactory.CreateDbContext();
        await ProcessInputAync(response);
        await ProcessOutputAync(response);
        await _dbContext.SaveChangesAsync();
        _dbContext.Dispose();
    }

    private async Task ProcessInputAync(NextResponse response)
    {
        // Collect input id and index as tuples
        List<(string Id, ulong Index)> inputTuples = response.Block.TransactionBodies
            .SelectMany(txBody => txBody.Inputs.Select(input => (Id: input.Id.ToHex(), input.Index)))
            .ToList();

        // Define the base query
        IQueryable<StakeRequestByAddress>? query = _dbContext.StakeRequestByAddresses.AsQueryable();

        // Build the query dynamically with OR conditions
        IQueryable<StakeRequestByAddress>? combinedQuery = null;
        foreach ((string Id, ulong Index) tuple in inputTuples)
        {
            IQueryable<StakeRequestByAddress>? currentQuery = query.Where(s => s.TxHash == tuple.Id && s.TxIndex == tuple.Index);
            combinedQuery = combinedQuery == null ? currentQuery : combinedQuery.Union(currentQuery);
        }

        // Execute the query if there are conditions
        List<StakeRequestByAddress> stakeRequestsByAddress = [];
        if (combinedQuery != null)
        {
            stakeRequestsByAddress = await combinedQuery.ToListAsync();
        }

        foreach (TransactionBody txBody in response.Block.TransactionBodies)
        {
            foreach (TransactionInput input in txBody.Inputs)
            {
                StakeRequestByAddress? stakeRequest = stakeRequestsByAddress.FirstOrDefault(s => s.TxHash == input.Id.ToHex() && s.TxIndex == input.Index);
                if (stakeRequest is not null)
                {
                    PallasDotnet.Models.TransactionOutput? timelockOutput = txBody.Outputs
                        .Where(o => new Address(o.Address.ToBech32()).GetPublicKeyHash().ToHex() == configuration["CoinectaTimelockValidatorHash"])
                        .FirstOrDefault();

                    if (timelockOutput is not null)
                    {
                        Cardano.Sync.Data.Models.TransactionOutput timelockOutputEntity = Utils.MapTransactionOutputEntity(txBody.Id.ToHex(), response.Block.Slot, timelockOutput);
                        if (IsAssetAmountLocked(stakeRequest.Amount.MultiAsset, timelockOutputEntity.Amount.MultiAsset))
                        {
                            stakeRequest.Status = StakeRequestStatus.Confirmed;
                        }
                        else
                        {
                            stakeRequest.Status = StakeRequestStatus.Error;
                        }
                    }
                    else
                    {
                        stakeRequest.Status = StakeRequestStatus.Cancelled;
                    }
                }
            }
        }
    }

    private Task ProcessOutputAync(NextResponse response)
    {
        foreach (TransactionBody txBody in response.Block.TransactionBodies)
        {
            foreach (PallasDotnet.Models.TransactionOutput output in txBody.Outputs)
            {
                string addressBech32 = output.Address.ToBech32();
                if (addressBech32.StartsWith("addr"))
                {
                    Address address = new Address(addressBech32);
                    string pkh = Convert.ToHexString(address.GetPublicKeyHash()).ToLowerInvariant();
                    if (pkh == configuration["CoinectaStakeProxyValidatorHash"])
                    {
                        if (output.Datum is not null && output.Datum.Type == PallasDotnet.Models.DatumType.InlineDatum)
                        {
                            byte[] datum = output.Datum.Data;
                            try
                            {
                                StakePoolProxy<NoDatum> stakeProxyDatum = CborConverter.Deserialize<StakePoolProxy<NoDatum>>(datum);
                                Cardano.Sync.Data.Models.TransactionOutput entityUtxo = Utils.MapTransactionOutputEntity(txBody.Id.ToHex(), response.Block.Slot, output);
                                StakeRequestByAddress stakeRequestByAddress = new StakeRequestByAddress
                                {
                                    Address = AddressUtility.GetBaseAddress(
                                        stakeProxyDatum.Destination.Address.Credential.Hash,
                                        stakeProxyDatum.Destination.Address.StakeCredential!.Credential.Hash,
                                        CoinectaUtils.GetNetworkType(configuration)).ToString(),
                                    Slot = response.Block.Slot,
                                    TxHash = txBody.Id.ToHex(),
                                    TxIndex = output.Index,
                                    Amount = entityUtxo.Amount,
                                    Status = StakeRequestStatus.Pending,
                                    StakePoolProxy = stakeProxyDatum
                                };

                                _dbContext.StakeRequestByAddresses.Add(stakeRequestByAddress);
                            }
                            catch
                            {
                                _logger.LogError("Error deserializing stake pool proxy datum: {datum} for {txHash}#{txIndex}",
                                    Convert.ToHexString(datum).ToLowerInvariant(),
                                    txBody.Id.ToHex(),
                                    output.Index
                                );
                            }
                        }
                    }
                }
            }
        }
        return Task.CompletedTask;
    }

    private static bool IsAssetAmountLocked(Dictionary<string, Dictionary<string, ulong>> source, Dictionary<string, Dictionary<string, ulong>> target)
    {
        foreach (KeyValuePair<string, Dictionary<string, ulong>> outerPair in source)
        {
            string outerKey = outerPair.Key;

            // Check if the outer key exists in the target dictionary
            if (!target.ContainsKey(outerKey))
            {
                return false;
            }

            foreach (KeyValuePair<string, ulong> innerPair in outerPair.Value)
            {
                string innerKey = innerPair.Key;
                ulong innerValue = innerPair.Value;

                // Check if the inner key exists and if the value is the same
                if (!target[outerKey].ContainsKey(innerKey) || target[outerKey][innerKey] < innerValue)
                {
                    return false;
                }
            }
        }

        return true; // All keys and values exist
    }
}