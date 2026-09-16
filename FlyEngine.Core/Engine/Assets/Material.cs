using FlyEngine.Core.Serialization.MemoryPack;
using MemoryPack;

namespace FlyEngine.Core.Assets;

[MemoryPackable]
public partial class Material(Guid guid) : Asset(guid)
{
    public static string Extension => ".material";

    [AssetFormatter<Texture>]
    public Texture? Albedo { get; set; }
}