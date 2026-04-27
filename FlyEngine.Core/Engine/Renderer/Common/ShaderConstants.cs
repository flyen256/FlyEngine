namespace FlyEngine.Core.Renderer.Common;

public static class ShaderConstants
{
    public const string Model = "uModel";
    public const string Projection = "uProjection";
    public const string View = "uView";
    public const string LightMatrix = "uLightMatrix";
    public const string NumLights = "uNumLights";
    public const string ShadowMap = "uShadowMap";
    public const string Skybox = "uSkybox";
    public const string ShadowEnabled = "uShadowEnabled";
    public const string ShadowDirIndex = "uShadowDirIndex";
    public const string LightSpaceMatrix = "uLightSpaceMatrix";
    public const string SunDirWorld = "uSunDirWorld";
    public const string FogDensity = "uFogDensity";
    public const string FogHeight = "uFogHeight";
    public const string FogFalloff = "uFogFalloff";
    public const string FogScatter = "uFogScatter";
    public const string FogColor = "uFogColor";
    public const string FogEnabled = "uFogEnabled";
    public const string AlbedoMetallic = "uGAlbedoMetallic";
    public const string NormalSmoothness = "uGNormalSmoothness";
    public const string Depth = "uDepth";
    public const string ViewportSize = "uViewportSize";
    public const string InverseProjection = "uInvProjection";
    public const string InverseView = "uInverseView";
    public const string CameraPosition = "uCameraPos";
    public const string AmbientColor = "uAmbientColor";
    public const string DitherStrength = "uDitherStrength";
    public const string AlbedoTint = "uAlbedoTint";
    public const string Metallic = "uMetallic";
    public const string Smoothness = "uSmoothness";

    public static string Pack(int packIndex, int index) => $"uPack{packIndex}[{index}]";
}