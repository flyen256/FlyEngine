using FlyEngine.Core.Assets;
using MemoryPack;

namespace FlyEngine.Core.Serialization.MemoryPack;

public class AssetArrayFormatterAttribute : MemoryPackCustomFormatterAttribute<List<Asset>>
{
    private readonly AssetArrayFormatter _formatter = new();

    public override IMemoryPackFormatter<List<Asset>> GetFormatter() => _formatter;
}