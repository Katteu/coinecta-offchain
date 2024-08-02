using PallasDotnet.Models;
using TransactionOutputEntity = Cardano.Sync.Data.Models.TransactionOutput;
using ValueEntity = Cardano.Sync.Data.Models.Value;
using DatumEntity = Cardano.Sync.Data.Models.Datum;
using DatumType = Cardano.Sync.Data.Models.DatumType;
using Cardano.Sync.Data.Models.Datums;
using Coinecta.Data;
using Microsoft.EntityFrameworkCore;

namespace Coinecta;

public static class Utils
{
    public static TransactionOutputEntity MapTransactionOutputEntity(string TransactionId, ulong slot, TransactionOutput output)
    {
        return new TransactionOutputEntity
        {
            Id = TransactionId,
            Address = output.Address.ToBech32(),
            Slot = slot,
            Index = Convert.ToUInt32(output.Index),
            Datum = output.Datum is null ? null : new DatumEntity((DatumType)output.Datum.Type, output.Datum.Data),
            Amount = new ValueEntity
            {
                Coin = output.Amount.Coin,
                MultiAsset = output.Amount.MultiAsset.ToDictionary(
                    k => k.Key.ToHex(),
                    v => v.Value.ToDictionary(
                        k => k.Key.ToHex(),
                        v => v.Value
                    )
                )
            }
        };
    }

    public static Dictionary<Hash, ulong> FilterAssetByPolicyId ( Dictionary<Hash, Dictionary<Hash, ulong>> multiAsset, string policyIdFilter)
    {
        return multiAsset
            .Where(ma => ma.Key.ToHex() == policyIdFilter)
            .Select(ma => ma.Value)
            .FirstOrDefault() ?? [];
    }
    
    public static ByteArray OutputRefToByteArray(Hash txHash, ulong txIndex)
    {
        byte[] txHashBytes = txHash.Bytes;
        byte[] txIndexBytes = BitConverter.GetBytes(txIndex);
        byte[] outputRefBytes = txHashBytes.Concat(txIndexBytes).ToArray();

        return new ByteArray(outputRefBytes);
    }

    public static async Task<List<TransactionOutputEntity>> ResolveTransactionInputsAsync(CoinectaDbContext _dbContext, TransactionBody tx)
    {
        List<string> txOutputRefs = tx.Inputs
            .Select(txInput => txInput.Id.ToHex() + txInput.Index)
            .ToList();

        return await _dbContext.TransactionOutputs
            .AsNoTracking()
            .Where(txOutput => txOutputRefs.Contains(txOutput.Id + txOutput.Index))
            .Select(txOutput => txOutput)
            .ToListAsync();
    }
}