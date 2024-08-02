using Cardano.Sync.Data.Models;
using Cardano.Sync.Data.Models.Datums;
using Cardano.Sync.Reducers;
using CardanoSharp.Wallet.Extensions;
using Coinecta.Data;
using Coinecta.Data.Models.Reducers;
using Crashr.Data.Models.Datums;
using Microsoft.EntityFrameworkCore;
using PallasDotnet.Models;
using Block = PallasDotnet.Models.Block;
using TransactionOutput = PallasDotnet.Models.TransactionOutput;

namespace Coinecta.Sync.Reducers;

[ReducerDepends(typeof(TransactionOutputReducer<CoinectaDbContext>))]
public class NftByAddressReducer(
    IDbContextFactory<CoinectaDbContext> dbContextFactory,
    IConfiguration configuration,
    ILogger<NftByAddressReducer> logger
) : IReducer
{
    private readonly string _stakeKeyPolicyId = configuration["CoinectaStakeKeyPolicyId"]!;
    private readonly string _stakeKeyPrefix = configuration["StakeKeyPrefix"]!;
    private CoinectaDbContext _dbContext = default!;
    private readonly ILogger<NftByAddressReducer> _logger = logger;

    private enum Transaction
    {
        Input,
        Output
    }

    public async Task RollBackwardAsync(NextResponse response)
    {
        using CoinectaDbContext _dbContext = dbContextFactory.CreateDbContext();

        // Remove all entries with slot greater than the rollback slot
        ulong rollbackSlot = response.Block.Slot;
        IQueryable<NftByAddress> rollbackEntries = _dbContext.NftsByAddress.AsNoTracking().Where(lba => lba.Slot > rollbackSlot);
        _dbContext.NftsByAddress.RemoveRange(rollbackEntries);

        // Save changes
        await _dbContext.SaveChangesAsync();
        await _dbContext.DisposeAsync();
    }

    public async Task RollForwardAsync(NextResponse response)
    {
        _dbContext = dbContextFactory.CreateDbContext();
        IEnumerable<TransactionBody> transactions = response.Block.TransactionBodies;

        List<string> resolvedTxInputAddresses = await ResolveTransactionAddressesAsync(response.Block, Transaction.Input);
        List<string> resolvedTxOutputAddresses = await ResolveTransactionAddressesAsync(response.Block, Transaction.Output);

        foreach (TransactionBody tx in transactions)
        {
            await ProcessInputAsync(response.Block, tx, resolvedTxInputAddresses);
            await ProcessOutputAsync(response.Block, tx, resolvedTxOutputAddresses);
        }

        await _dbContext.SaveChangesAsync();
        await _dbContext.DisposeAsync();
    }

    private async Task ProcessInputAsync(Block block, TransactionBody tx, List<string> resolvedTxInputAddresses)
    {
        List<NftByAddress> nftByAddresses = _dbContext.NftsByAddress.Local
            .Where(s => resolvedTxInputAddresses.Contains(s.Address))
            .OrderByDescending(s => s.Slot)
            .ToList();

        nftByAddresses = nftByAddresses.Any() ? nftByAddresses : await _dbContext.NftsByAddress
            .AsNoTracking()
            .Where(s => resolvedTxInputAddresses.Contains(s.Address))
            .GroupBy(g => g.Address)
            .Select(g => g.OrderByDescending(s => s.Slot).First())
            .ToListAsync();
        
        if (nftByAddresses.Any())
        {
            List<ByteArray> txOutputRefs = tx.Inputs.Select(input => OutputRefByteArray(input.Id.Bytes, BitConverter.GetBytes(input.Index))).ToList();

            nftByAddresses.ForEach(nftByAddress => {
                List<ByteArray> keys = nftByAddress.Assets.Keys
                    .Where(nftByAddressKey => txOutputRefs.Any(txOutRefKey => txOutRefKey.Value.SequenceEqual(nftByAddressKey.Value)))
                    .ToList();
                    
                if (keys.Any())
                {
                    Dictionary<ByteArray> assets = nftByAddress.Assets;

                    keys.ForEach(key => {
                        assets.Remove(key);
                    });

                    NftByAddress newNftByAddress = new()
                    {
                        Address = nftByAddress.Address,
                        Slot = block.Slot,
                        Assets = assets
                    };

                    _dbContext.Entry(newNftByAddress).State = EntityState.Detached;
                    _dbContext.NftsByAddress.Add(newNftByAddress);
                }
            });
        }
    }

    private async Task ProcessOutputAsync(Block block, TransactionBody tx, List<string> resolvedTxOutputAddresses)
    {
        List<TransactionOutput> transactionOutputs = tx.Outputs
            .Where(output => output.Address.ToBech32().StartsWith("addr"))
            .Where(output => Utils
                .FilterAssetByPolicyId(output.Amount.MultiAsset, _stakeKeyPolicyId)
                .Any(value => value.Key.ToHex().StartsWith(_stakeKeyPrefix)
            ))
            .ToList();

        if (transactionOutputs.Any())
        {        
            List<NftByAddress> nftByAddresses = _dbContext.NftsByAddress.Local
                .Where(s => resolvedTxOutputAddresses.Contains(s.Address))
                .ToList();

            nftByAddresses = nftByAddresses.Count > 0 ? nftByAddresses : await _dbContext.NftsByAddress
                .AsNoTracking()
                .Where(s => resolvedTxOutputAddresses.Contains(s.Address))
                .ToListAsync();
            
            transactionOutputs.ForEach(output =>
            {
                string outputAddress = output.Address.ToBech32();

                List<ByteArray> userTokens = Utils
                    .FilterAssetByPolicyId (output.Amount.MultiAsset, _stakeKeyPolicyId)
                    .Where(ma => ma.Key.ToHex().StartsWith(_stakeKeyPrefix))
                    .Select(ma => 
                    {
                        string userToken = _stakeKeyPolicyId + ma.Key.ToHex();
                        return new ByteArray(Convert.FromHexString(userToken));
                    })
                    .ToList();

                NftByAddress? nftByAddress = nftByAddresses.FirstOrDefault(s => s.Address == outputAddress);

                ByteArray key = OutputRefByteArray(tx.Id.Bytes, BitConverter.GetBytes(output.Index));

                Dictionary<ByteArray> assets = nftByAddress?.Assets ?? [];
                
                userTokens.ForEach(token => {
                    assets.Add(key, token);
                });

                NftByAddress newNftByAddress = new()
                {
                    Address = outputAddress,
                    Slot = block.Slot,
                    Assets = assets
                };

                _dbContext.Entry(newNftByAddress).State = EntityState.Detached;
                _dbContext.NftsByAddress.Add(newNftByAddress);
            });
        }
    }

    private async Task<List<string>> ResolveTransactionAddressesAsync(Block block, Transaction txType)
    {
        List<string> txOutputRefs = [];

        if (txType == Transaction.Input)
        {
            txOutputRefs = block.TransactionBodies.SelectMany(tx => tx.Inputs
                .Select(input => input.Id.ToHex() + input.Index))
                .ToList();
        }
        else if (txType == Transaction.Output)
        {
            txOutputRefs = block.TransactionBodies.SelectMany(tx => tx.Outputs
                .Select(output => tx.Id.ToHex() + output.Index))
                .ToList();
        }

        return await _dbContext.TransactionOutputs
            .AsNoTracking()
            .Where(s => txOutputRefs.Contains(s.Id + s.Index))
            .Select(s => s.Address)
            .ToListAsync(); 
    }

    private ByteArray OutputRefByteArray(byte[] txHash, byte[] txIndex)
    {
        byte[] outputRefBytes = txHash.Concat(txIndex).ToArray();

        return new ByteArray(outputRefBytes);
    }
}
