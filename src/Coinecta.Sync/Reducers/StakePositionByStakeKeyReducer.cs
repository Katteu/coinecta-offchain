using Microsoft.EntityFrameworkCore;
using PallasDotnet.Models;
using Coinecta.Data;
using Address = CardanoSharp.Wallet.Models.Addresses.Address;
using CardanoSharp.Wallet.Extensions.Models;
using Coinecta.Data.Models.Datums;
using Coinecta.Data.Models.Reducers;
using Cardano.Sync.Data.Models.Datums;
using Cardano.Sync.Reducers;
using Coinecta.Data.Models.Enums;
using Cardano.Sync.Data.Models;
using TransactionOutput = PallasDotnet.Models.TransactionOutput;
using CardanoSharp.Wallet.Models;

namespace Coinecta.Sync.Reducers;

public class StakePositionByStakeKeyReducer(
    IDbContextFactory<CoinectaDbContext> dbContextFactory,
    IConfiguration configuration,
    ILogger<StakePositionByStakeKeyReducer> logger
) : IReducer
{
    private readonly string _stakeKeyPolicyId = configuration["CoinectaStakeKeyPolicyId"]!;
    private readonly string _stakeKeyPrefix = configuration["StakeKeyPrefix"]!;
    private readonly string _referencePrefix = configuration["ReferencePrefix"]!;
    private readonly string _timelockValidatorHash = configuration["CoinectaTimelockValidatorHash"]!;
    private CoinectaDbContext _dbContext = default!;
    private readonly ILogger<StakePositionByStakeKeyReducer> _logger = logger;

    public async Task RollBackwardAsync(NextResponse response)
    {
        _dbContext = dbContextFactory.CreateDbContext();
        ulong rollbackSlot = response.Block.Slot;

        _dbContext.StakePositionByStakeKeys.RemoveRange(_dbContext.StakePositionByStakeKeys.Where(s => s.Slot > rollbackSlot));

        List<StakePositionHistory> stakePositionsToRestore = await _dbContext.StakePositionsHistory
            .AsNoTracking()
            .Where(s => s.Slot > rollbackSlot)
            .GroupBy(s => s.TxOutputRef)
            .Where(g => g.Count() == 1 && g.Any(s => s.UtxoStatus == UtxoStatus.Spent))
            .Select(g => g.First())
            .ToListAsync();

        _dbContext.StakePositionsHistory.RemoveRange(_dbContext.StakePositionsHistory.Where(s => s.Slot > rollbackSlot));

        List<StakePositionByStakeKey> stakePositionsToAdd = [];

        if (stakePositionsToRestore.Any())
        {
            foreach (StakePositionHistory stakePosition in stakePositionsToRestore)
            {
                StakePositionByStakeKey stakePositionByKey = new()
                {
                    StakeKey = stakePosition.StakeKey,
                    Slot = stakePosition.Slot,
                    TxHash = stakePosition.TxHash,
                    TxIndex = stakePosition.TxIndex,
                    TxOutputRef = stakePosition.TxOutputRef,
                    Interest = stakePosition.Interest,
                    StakePosition = stakePosition.StakePosition,
                    Amount = stakePosition.Amount,
                };
                stakePositionsToAdd.Add(stakePositionByKey);
            }

            _dbContext.StakePositionByStakeKeys.AddRange(stakePositionsToAdd);
        }

        await _dbContext.SaveChangesAsync();
        _dbContext.Dispose();
    }

    public async Task RollForwardAsync(NextResponse response)
    {
        _dbContext = dbContextFactory.CreateDbContext();

        List<StakeRequestByAddress> stakeRequestsForBlock = await GetStakeRequestsForBlockAsync(response.Block);

        foreach (TransactionBody txBody in response.Block.TransactionBodies)
        {
            await ProcessInputsAsync(response.Block, txBody);
            await ProcessOutputsAsync(response.Block, txBody, stakeRequestsForBlock);
        }

        await _dbContext.SaveChangesAsync();
        _dbContext.Dispose();
    }

    private async Task ProcessInputsAsync(PallasDotnet.Models.Block block, TransactionBody tx)
    {
        string[] txOutputRefs = tx.Inputs.Select(i => i.Id.ToHex() + i.Index.ToString()).ToArray();

        List<StakePositionByStakeKey> stakePositions = _dbContext.StakePositionByStakeKeys.Local
            .Where(s => txOutputRefs.Contains(s.TxOutputRef))
            .ToList();

        if (!stakePositions.Any())
        {
            stakePositions = await _dbContext.StakePositionByStakeKeys
                .AsNoTracking()
                .Where(s => txOutputRefs.Contains(s.TxOutputRef))
                .ToListAsync();
        }

        List<StakePositionHistory> stakePositionsHistoryToAdd = [];
        List<StakePositionByStakeKey> stakePositionsToRemove = [];

        if (stakePositions.Any())
        {
            foreach (StakePositionByStakeKey stakePosition in stakePositions)
            {

                StakePositionHistory stakePositionHistory = new()
                {
                    StakeKey = stakePosition.StakeKey,
                    Slot = block.Slot,
                    TxHash = stakePosition.TxHash,
                    TxIndex = stakePosition.TxIndex,
                    TxOutputRef = stakePosition.TxOutputRef,
                    Interest = stakePosition.Interest,
                    UtxoStatus = UtxoStatus.Spent,          
                    StakePosition = stakePosition.StakePosition,
                    Amount = stakePosition.Amount,
                };

                stakePositionsHistoryToAdd.Add(stakePositionHistory);
                stakePositionsToRemove.Add(stakePosition);
            }

            _dbContext.StakePositionByStakeKeys.RemoveRange(stakePositionsToRemove);
            _dbContext.StakePositionsHistory.AddRange(stakePositionsHistoryToAdd);
        }
    }

    private Task ProcessOutputsAsync(PallasDotnet.Models.Block block, TransactionBody tx, List<StakeRequestByAddress> stakeRequests)
    {
        if (stakeRequests.Any())
        {
            foreach (TransactionOutput output in tx.Outputs)
            {
                if (output.Address.ToBech32().StartsWith("addr"))
                {
                    Address address = new(output.Address.ToBech32());
                    string pkh = Convert.ToHexString(address.GetPublicKeyHash()).ToLowerInvariant();

                    if (pkh == _timelockValidatorHash)
                    {
                        if (output.Datum is not null && output.Datum.Type == PallasDotnet.Models.DatumType.InlineDatum)
                        {
                            byte[] datum = output.Datum.Data;

                            try
                            {
                                CIP68<Timelock> timelockDatum = CborConverter.Deserialize<CIP68<Timelock>>(datum);
                                Cardano.Sync.Data.Models.TransactionOutput entityUtxo = Utils.MapTransactionOutputEntity(tx.Id.ToHex(), block.Slot, output);

                                if (entityUtxo.Amount.MultiAsset.TryGetValue(_stakeKeyPolicyId, out Dictionary<string, ulong>? stakeKeyBundle))
                                {
                                    string? assetName = stakeKeyBundle.Keys.FirstOrDefault(key => key.StartsWith(_referencePrefix));

                                    if (assetName is not null)
                                    {
                                        string _assetName = assetName.Replace(_referencePrefix, string.Empty);

                                        string? userAddress = tx.Outputs
                                            .Where(output => output.Amount.MultiAsset is not null)
                                            .Where(output => Utils.FilterAssetByPolicyId(output.Amount.MultiAsset, _stakeKeyPolicyId)
                                                .Any(ma => ma.Value.Any(v => v.Key.ToHex().StartsWith(_stakeKeyPrefix) && v.Key.ToHex().Contains(_assetName))
                                            ))
                                            .Select(output => output.Address.ToBech32())
                                            .FirstOrDefault();

                                        StakeRequestByAddress? matchingStakeRequest = stakeRequests.FirstOrDefault(s => s.Address == userAddress);

                                        if (matchingStakeRequest is not null)
                                        {
                                            StakePositionByStakeKey stakePositionByKey = new()
                                            {
                                                StakeKey = _stakeKeyPolicyId + _assetName,
                                                Slot = block.Slot,
                                                TxOutputRef = tx.Id.ToHex() + output.Index.ToString(),
                                                TxHash = tx.Id.ToHex(),
                                                TxIndex = output.Index,
                                                Interest = matchingStakeRequest.StakePoolProxy.RewardMultiplier,
                                                StakePosition = timelockDatum,
                                                Amount = entityUtxo.Amount,

                                            };

                                            StakePositionHistory stakePositionHistory = new()
                                            {
                                                StakeKey = _stakeKeyPolicyId + _assetName,
                                                Slot = block.Slot,
                                                TxOutputRef = tx.Id.ToHex() + output.Index.ToString(),
                                                TxHash = tx.Id.ToHex(),
                                                TxIndex = output.Index,
                                                Interest = matchingStakeRequest.StakePoolProxy.RewardMultiplier,
                                                UtxoStatus = UtxoStatus.Unspent,
                                                StakePosition = timelockDatum,
                                                Amount = entityUtxo.Amount,
                                            };

                                            _dbContext.StakePositionByStakeKeys.Add(stakePositionByKey);
                                            _dbContext.StakePositionsHistory.Add(stakePositionHistory);
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                _logger.LogError("Error deserializing timelock datum: {datum} for {txHash}#{txIndex}",
                                    Convert.ToHexString(datum).ToLowerInvariant(),
                                    tx.Id.ToHex(),
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

    private async Task<List<StakeRequestByAddress>> GetStakeRequestsForBlockAsync(PallasDotnet.Models.Block block)
    {
        string[] allTxOutputRefs = block.TransactionBodies.SelectMany(tx => tx.Inputs.Select(i => i.Id.ToHex() + i.Index.ToString())).ToArray();

        return await _dbContext.StakeRequestByAddresses
            .AsNoTracking()
            .Where(s => allTxOutputRefs.Contains(s.TxHash + s.TxIndex.ToString()))
            .ToListAsync();
    }
}