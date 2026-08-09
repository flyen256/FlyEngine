using FlyEngine.Core.Assets;
using MemoryPack;

namespace FlyEngine.Core.Serialization.MemoryPack;

public class AssetArrayFormatter : MemoryPackFormatter<List<Asset>>
{
    public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref List<Asset>? value)
    {
        writer.WriteVarInt(value?.Count ?? 0);
        if (value == null) return;
        foreach (var asset in value)
            writer.WriteString(asset.Guid.ToString());
    }

    public override void Deserialize(ref MemoryPackReader reader, scoped ref List<Asset>? value)
    {
        var count = reader.ReadVarIntInt32();
        value = [];
        for (var i = 0; i < count; i++)
        {
            var guid = reader.ReadString();
            if (guid == null) continue;
            var asset = AssetsManager.GetAsset(Guid.Parse(guid));
            if (asset == null) continue;
            value.Add(asset);
        }
    }
}