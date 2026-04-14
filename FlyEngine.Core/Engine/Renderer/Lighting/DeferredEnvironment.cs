using System.Numerics;

namespace FlyEngine.Core.Renderer.Lighting;

public struct DeferredEnvironment
{
    public Vector3 AmbientColor;
    public bool ShadowEnabled;

    public bool FogEnabled;
    public float FogDensity;
    public float FogHeight;
    public float FogHeightFalloff;
    public float FogScattering;
    public Vector3 FogColor;

    public static DeferredEnvironment Default => new DeferredEnvironment
    {
        AmbientColor = new Vector3(0.04f, 0.045f, 0.06f),
        ShadowEnabled = true,
        FogEnabled = false,
        FogDensity = 0.2f,
        FogHeight = 1.5f,
        FogHeightFalloff = 0.7f,
        FogScattering = 0.5f,
        FogColor = new Vector3(1f, 1f, 1f)
    };
}
