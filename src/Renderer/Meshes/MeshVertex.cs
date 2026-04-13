using System.Numerics;
using System.Runtime.InteropServices;

namespace FlyEngine.Renderer.Meshes;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MeshVertex
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector2 TextureCoordinates;
    public Vector3 Tangent;
    public Vector3 Bitangent;
}