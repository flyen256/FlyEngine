using MemoryPack;

namespace FlyEngine.Core.Engine.Components.Common;

[MemoryPackable]
public partial struct ComponentDataHolder
{
    public required string TypeName { get; set; }
    public string JsonPayload { get; set; }
}