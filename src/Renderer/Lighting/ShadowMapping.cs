using System.Numerics;

namespace FlyEngine.Renderer.Lighting;

public static class ShadowMapping
{
    public static Matrix4x4 CreateLightSpaceMatrix(
        Vector3 lightDir,
        Vector3 center,
        float size,
        float nearPlane,
        float farPlane)
    {
        lightDir = Vector3.Normalize(lightDir);

        var lightPos = center - lightDir * size;
        var up = System.Math.Abs(lightDir.Y) > 0.99f
            ? Vector3.UnitZ
            : Vector3.UnitY;

        var view = Matrix4x4.CreateLookAt(
            lightPos,
            center,
            up
        );

        var proj = Matrix4x4.CreateOrthographicOffCenter(
            -size, size,
            -size, size,
            nearPlane,
            farPlane
        );

        return view * proj;
    }
}
