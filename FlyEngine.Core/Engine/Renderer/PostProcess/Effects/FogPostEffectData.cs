using System.Numerics;

namespace FlyEngine.Core.Renderer;

public struct FogPostEffectData
{
    public bool FogEnabled;
    public float FogDensity;
    public float FogHeight;
    public float FogHeightFalloff;
    public float FogScattering;
    public Vector3 FogColor;
    public int FogSteps;

    public static FogPostEffectData Default => new()
    {
        FogEnabled = true,
        FogSteps = 1,
        FogDensity = 0.005f,
        FogColor = new Vector3(0.7f, 0.75f, 0.8f)
    };
}