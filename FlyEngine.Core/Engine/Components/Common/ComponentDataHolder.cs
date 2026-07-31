using MemoryPack;

namespace FlyEngine.Core.Components;

[MemoryPackable]
public partial struct ComponentDataHolder
{
    public required string TypeName { get; set; }
    public string JsonPayload { get; set; }
}