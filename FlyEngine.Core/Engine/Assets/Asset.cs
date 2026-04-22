using MemoryPack;

namespace FlyEngine.Core.Engine.Assets;

[MemoryPackable]
public partial class Asset(Guid guid)
{
    [MemoryPackInclude]
    public Guid Guid { get; private set; } = guid;
    [MemoryPackInclude]
    public string? Path { get; private set; }
}