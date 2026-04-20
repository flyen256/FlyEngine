using System.Numerics;
using MemoryPack;

namespace FlyEngine.Network.Packets;

[MemoryPackable]
public partial struct TransformPacket
{
    public int NetworkObjectId;
    public Vector3 Position;
    public Vector3 Scale;
    public Quaternion Rotation;
}