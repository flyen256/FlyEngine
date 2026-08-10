using System.Numerics;
using MemoryPack;

namespace FlyEngine.Core.Renderer;

[MemoryPackable]
public partial struct DeferredEnvironment
{
    public Vector3 AmbientColor;
    public bool ShadowEnabled;

    [MemoryPackIgnore]
    public static DeferredEnvironment Default => new()
    {
        AmbientColor = new Vector3(0.04f, 0.045f, 0.06f),
        ShadowEnabled = true,
    };
}