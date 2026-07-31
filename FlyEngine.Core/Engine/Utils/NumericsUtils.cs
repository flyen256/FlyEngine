using System.Numerics;
using FlyEngine.Core.Components;
using FlyEngine.Core.Math;

namespace FlyEngine.Core.Utils;

public static class NumericsUtils
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
            System.Math.Clamp(nearPlane, 0.01f, float.MaxValue),
            System.Math.Clamp(farPlane, 0.01f, float.MaxValue)
        );

        return view * proj;
    }

    public static Matrix4x4 CreateLightSpaceMatrix(LightSource light)
    {
	    var position = light.Transform.Position;
	    var direction = Vector3.Normalize(Vector3.Transform(-Vector3.UnitZ, light.Transform.Rotation));

	    const float near = 0.1f;
	    var far = System.Math.Clamp(light.Range * 1.2f, 0.1f, float.MaxValue);
    
	    var fov = System.Math.Clamp(light.SpotOuterDegrees * 2f, 1f, 170f);

	    var proj = Matrix4x4.CreatePerspectiveFieldOfView(
		    MathHelper.DegreesToRadians(fov), 1.0f, near, far);

	    var up = System.Math.Abs(direction.Y) > 0.99f ? Vector3.UnitZ : Vector3.UnitY;
	    var view = Matrix4x4.CreateLookAt(position, position + direction, up);

	    return view * proj;
    }
}
