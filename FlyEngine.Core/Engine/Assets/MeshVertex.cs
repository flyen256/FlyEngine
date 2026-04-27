using System.Numerics;
using System.Runtime.InteropServices;
using MemoryPack;

namespace FlyEngine.Core.Assets;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
[MemoryPackable]
public partial struct MeshVertex
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector2 TextureCoordinates;
    public Vector3 Tangent;
    public Vector3 Bitangent;
}