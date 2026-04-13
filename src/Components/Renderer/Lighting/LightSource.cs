using System.Numerics;
using FlyEngine.Components.Common;
using FlyEngine.Math;
using FlyEngine.Renderer.Lighting;

namespace FlyEngine.Components.Renderer.Lighting;

public class LightSource : Component
{
    public LightType Type { get; set; } = LightType.Point;
    public Vector3 Color { get; set; } = Vector3.One;
    public float Intensity { get; set; } = 1f;
    public float Range { get; set; } = 10f;
    public float SpotOuterDegrees { get; set; } = 80f;
    public float SpotInnerDegrees { get; set; } = 35f;
    public Vector2 AreaHalfSize { get; set; } = new(1f, 0.5f);
    public bool CastShadows { get; set; } = true;

    protected override void OnInitialize()
    {
        Application.Instance.Lights.Add(this);
    }

    protected internal override void OnRemovingFromStore()
    {
        Application.Instance.Lights.Remove(this);
    }

    public DeferredLightPacked BuildPacked()
    {
        var p = Transform.Position;
        var pos = new Vector3(p.X, p.Y, p.Z);
        var rot = Matrix4x4.CreateFromQuaternion(Transform.Rotation);
        var forward = Vector3.Normalize(Vector3.Transform(-Vector3.UnitZ, rot));
        var right = Vector3.Normalize(Vector3.Transform(Vector3.UnitX, rot));
        var up = Vector3.Normalize(Vector3.Transform(Vector3.UnitY, rot));

        var radiance = Color * Intensity;
        var type = (float)Type;

        return Type switch
        {
            LightType.Point => new DeferredLightPacked
            {
                Pack0 = new Vector4(pos, type),
                Pack1 = new Vector4(radiance, Range),
                Pack2 = Vector4.Zero,
                Pack3 = Vector4.Zero,
                Pack4 = Vector4.Zero
            },
            LightType.Spot => BuildSpotPack(pos, type, radiance, forward),
            LightType.Directional => new DeferredLightPacked
            {
                Pack0 = new Vector4(pos, type),
                Pack1 = new Vector4(radiance, 0f),
                Pack2 = new Vector4(forward, 0f),
                Pack3 = Vector4.Zero,
                Pack4 = Vector4.Zero
            },
            LightType.Area => new DeferredLightPacked
            {
                Pack0 = new Vector4(pos, type),
                Pack1 = new Vector4(radiance, AreaHalfSize.Y),
                Pack2 = new Vector4(forward, 0f),
                Pack3 = new Vector4(right, AreaHalfSize.X),
                Pack4 = new Vector4(up, 0f)
            },
            _ => default
        };
    }

    private DeferredLightPacked BuildSpotPack(Vector3 pos, float type, Vector3 radiance, Vector3 forward)
    {
        var outerDeg = System.Math.Max(SpotOuterDegrees, 0.5f);
        var innerDeg = System.Math.Clamp(SpotInnerDegrees, 0.5f, outerDeg - 0.5f);
        var outerHalf = MathHelper.DegreesToRadians(outerDeg) * 0.5f;
        var innerHalf = MathHelper.DegreesToRadians(innerDeg) * 0.5f;
        var cosOuter = MathF.Cos(outerHalf);
        var cosInner = MathF.Cos(innerHalf);
        var cosHi = System.Math.Max(cosInner, cosOuter);
        var cosLo = System.Math.Min(cosInner, cosOuter);
        cosInner = cosHi;
        cosOuter = cosLo;
        return new DeferredLightPacked
        {
            Pack0 = new Vector4(pos, type),
            Pack1 = new Vector4(radiance, Range),
            Pack2 = new Vector4(forward, cosOuter),
            Pack3 = new Vector4(cosInner, 0f, 0f, 0f),
            Pack4 = Vector4.Zero
        };
    }
}
