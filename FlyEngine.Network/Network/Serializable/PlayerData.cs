using MemoryPack;

namespace FlyEngine.Network.Serializable;

[MemoryPackable]
public partial class PlayerData
{
    public uint Id { get; set; }
    public int PeerId { get; set; }
    public bool IsHost { get; set; }
}