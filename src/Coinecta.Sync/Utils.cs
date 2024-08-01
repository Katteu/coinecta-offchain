using PallasDotnet.Models;
using TransactionOutputEntity = Cardano.Sync.Data.Models.TransactionOutput;
using ValueEntity = Cardano.Sync.Data.Models.Value;
using DatumEntity = Cardano.Sync.Data.Models.Datum;
using DatumType = Cardano.Sync.Data.Models.DatumType;
using Hash = PallasDotnet.Models.Hash;

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

    public static Dictionary<Hash, Dictionary<Hash, ulong>> FilterAssetByPolicyId
    (
       Dictionary<Hash, Dictionary<Hash, ulong>> multiAsset,
       string policyIdFilter
    )
    {
        return multiAsset
            .Where(ma => ma.Key.ToHex() == policyIdFilter)
            .ToDictionary();
    }
}