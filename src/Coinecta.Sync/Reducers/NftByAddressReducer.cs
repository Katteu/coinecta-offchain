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
using TransactionOutputEntity = Cardano.Sync.Data.Models.TransactionOutput;

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

        foreach (TransactionBody tx in transactions)
        {
            List<TransactionOutputEntity> resolvedTxInputs = await Utils.ResolveTransactionInputsAsync(_dbContext, tx);

            await ProcessInputAsync(response.Block, tx, resolvedTxInputs);
            await ProcessOutputAsync(response.Block, tx);
        }

        await _dbContext.SaveChangesAsync();
        await _dbContext.DisposeAsync();
    }

    private async Task ProcessInputAsync(Block block, TransactionBody tx, List<TransactionOutputEntity> resolvedTxInputs)
    {
        List<string> addresses = resolvedTxInputs.Select(txInput => txInput.Address).ToList();

        List<NftByAddress> nftByAddresses = await _dbContext.NftsByAddress
            .AsNoTracking()
            .Where(s => addresses.Contains(s.Address))
            .GroupBy(g => g.Address)
            .Select(g => g.OrderByDescending(s => s.Slot).First())
            .ToListAsync();

        if (nftByAddresses.Any())
        {
            List<NftByAddress> nftByAddressesLocal = _dbContext.NftsByAddress.Local
                .Where(s => addresses.Contains(s.Address))
                .GroupBy(g => g.Address)
                .Select(g => g.OrderByDescending(s => s.Slot).First())
                .ToList();

            nftByAddresses = nftByAddressesLocal.Any() ? nftByAddressesLocal : nftByAddresses;
            
            List<ByteArray> txOutputRefs = tx.Inputs
                .Select(input => Utils.OutputRefToByteArray(input.Id, input.Index))
                .ToList();

            nftByAddresses.ForEach(nftByAddress => 
            {
                List<ByteArray> keys = nftByAddress.Assets.Keys
                    .Where(nftByAddressKey => txOutputRefs.Any(txOutRefKey => txOutRefKey.Value.SequenceEqual(nftByAddressKey.Value)))
                    .ToList();
                    
                if (keys.Any())
                {
                    Dictionary<ByteArray> assets = nftByAddress.Assets;

                    keys.ForEach(key => 
                    {
                        assets.Remove(key);
                    });

                    NftByAddress newNftByAddress = new()
                    {
                        Address = nftByAddress.Address,
                        Slot = block.Slot,
                        Assets = assets
                    };

                    _dbContext.NftsByAddress.Add(newNftByAddress);
                }
            });
        }
    }

    private async Task ProcessOutputAsync(Block block, TransactionBody tx)
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
            List<string> addresses = transactionOutputs.Select(txOutput => txOutput.Address.ToBech32()).ToList();   

            List<NftByAddress> nftByAddresses = _dbContext.NftsByAddress.Local
                .Where(s => addresses.Contains(s.Address))
                .GroupBy(g => g.Address)
                .Select(g => g.OrderByDescending(s => s.Slot).First())
                .ToList();

            nftByAddresses = nftByAddresses.Any() ? nftByAddresses : await _dbContext.NftsByAddress
                .AsNoTracking()
                .Where(s => addresses.Contains(s.Address))
                .GroupBy(g => g.Address)
                .Select(g => g.OrderByDescending(s => s.Slot).First())
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

                ByteArray key = Utils.OutputRefToByteArray(tx.Id, output.Index);

                Dictionary<ByteArray> assets = nftByAddresses.FirstOrDefault(s => s.Address == outputAddress)?.Assets ?? [];

                userTokens.ForEach(token => 
                {
                    assets.Add(key, token);
                });

                NftByAddress newNftByAddress = new()
                {
                    Address = outputAddress,
                    Slot = block.Slot,
                    Assets = assets
                };

                _dbContext.NftsByAddress.Add(newNftByAddress);
            });
        }
    }
}
