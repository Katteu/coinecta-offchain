using Cardano.Sync.Data.Models;
using Cardano.Sync.Data.Models.Datums;
using Coinecta.Data.Models.Datums;
using Coinecta.Data.Models.Enums;
using Value = Cardano.Sync.Data.Models.Value;
using ValueDatum = Cardano.Sync.Data.Models.Datums.Value;
using Coinecta.Data.Utils;

namespace Coinecta.Data.Models.Reducers;

public record StakePositionHistory
{
    public string StakeKey { get; init; } = default!;
    public ulong Slot { get; init; }
    public string TxHash { get; init; } = default!;
    public ulong TxIndex { get; init; }
    public string TxOutputRef { get; init; } = default!;
    public byte[] AmountCbor { get; set; } = [];
    public byte[] InterestCbor { get; private set; } = [];
    public byte[] StakePositionCbor { get; set; } = [];
    public UtxoStatus UtxoStatus { get; set; } = UtxoStatus.Unspent;

    public CIP68<Timelock> StakePosition
    {
        get => CborConverter.Deserialize<CIP68<Timelock>>(StakePositionCbor);
        set => StakePositionCbor = CborConverter.Serialize(value);
    }

    public Rational Interest 
    {
        get => CborConverter.Deserialize<Rational>(InterestCbor);
        set => InterestCbor = CborConverter.Serialize(value);
    }

    public ValueDatum AmountDatum
    {
        get => CborConverter.Deserialize<ValueDatum>(AmountCbor);
        set => AmountCbor = CborConverter.Serialize(value);
    }

    public Value Amount
    {
        get => CoinectaUtils.ConvertValueDatumToValue(AmountDatum);
        set => AmountDatum = CoinectaUtils.ConvertValueToValueDatum(value);
    }
}