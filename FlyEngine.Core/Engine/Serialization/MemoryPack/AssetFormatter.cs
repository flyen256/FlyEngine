using System;
using FlyEngine.Core.Assets;
using MemoryPack;

namespace FlyEngine.Core.Serialization.MemoryPack;

public class AssetFormatter<TAsset> : MemoryPackFormatter<TAsset> where TAsset : Asset
{
    public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TAsset? value)
    {
        if (value == null)
        {
            writer.WriteNullObjectHeader();
            return;
        }

        writer.WriteString(value.Guid.ToString());
    }

    public override void Deserialize(ref MemoryPackReader reader, scoped ref TAsset? value)
    {
        if (!reader.TryReadObjectHeader(out _))
        {
            value = null;
            return;
        }

        var guid = reader.ReadString();
        if (guid == null)
        {
            value = null;
            return;
        }

        value = AssetsManager.GetAsset<TAsset>(Guid.Parse(guid));
    }
}