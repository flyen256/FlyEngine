using FlyEngine.Core.Assets;
using MemoryPack;

namespace FlyEngine.Core.Serialization.MemoryPack;

public class AssetFormatterAttribute<TAsset> : MemoryPackCustomFormatterAttribute<TAsset> where TAsset : Asset
{
    private readonly AssetFormatter<TAsset> _formatter = new();

    public override IMemoryPackFormatter<TAsset> GetFormatter() => _formatter;
}