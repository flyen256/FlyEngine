using System.Numerics;

namespace FlyEngine.Renderer.Lighting;

public struct DeferredEnvironment
{
    public bool ShadowEnabled;
    public int ShadowDirectionalLightIndex;
    public Matrix4x4 LightSpaceMatrix;

    public Vector3 SunDirectionWorld;

    public bool FogEnabled;
    public float FogDensity;
    public float FogHeight;
    public float FogHeightFalloff;
    public float FogScattering;
    public Vector3 FogColor;
}
