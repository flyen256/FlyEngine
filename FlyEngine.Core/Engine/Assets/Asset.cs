using MemoryPack;

namespace FlyEngine.Core.Assets;

[MemoryPackable]
public partial class Asset(Guid guid)
{
    [MemoryPackInclude]
    public Guid Guid { get; private set; } = guid;
    [MemoryPackInclude]
    public string Name { get; set; } = string.Empty;
    [MemoryPackIgnore]
    public int AssetIndex { get; set; } = -1;
    [MemoryPackInclude]
    public string? Path { get; set; }
}