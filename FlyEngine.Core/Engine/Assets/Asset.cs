using MemoryPack;
using Silk.NET.OpenGL;

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
    [MemoryPackIgnore]
    public bool Loaded { get; protected set; }

    public virtual void Load(GL? gl = null)
    {
        Loaded = true;
    }

    public virtual void Unload()
    {
        Loaded = false;
    }
}