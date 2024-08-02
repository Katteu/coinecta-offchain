using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Cardano.Sync.Data.Models;
using Cardano.Sync.Data.Models.Datums;
using Crashr.Data.Models.Datums;

namespace Coinecta.Data.Models.Reducers;

public record NftByAddress
{
    public string Address { get; init; } = default!;

    public ulong Slot { get; init; } = default!;

    public byte[] AssetsCbor { get; set; } = [];

    [NotMapped]
    public Dictionary<ByteArray> Assets 
    {
        get => CborConverter.Deserialize<Dictionary<ByteArray>>(AssetsCbor);
        set => AssetsCbor = CborConverter.Serialize(value);
    }
}