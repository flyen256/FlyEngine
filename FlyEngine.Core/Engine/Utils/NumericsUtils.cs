using System.Numerics;
using FlyEngine.Core.Components.Renderer.Lighting;
using FlyEngine.Core.Math;

namespace FlyEngine.Core.Extensions;

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
            nearPlane,
            farPlane
        );

        return view * proj;
    }

		public static Matrix4x4 CreateLightSpaceMatrix(LightSource light) {
				var position = light.Transform.Position;
				var direction = Vector3.Normalize(Vector3.Transform(-Vector3.UnitZ, light.Transform.Rotation));

				float near = 0.1f;
				float far = light.Range * 1.2f;
				float fov = light.SpotOuterDegrees * 2f;

				Matrix4x4 proj = Matrix4x4.CreatePerspectiveFieldOfView(
						MathHelper.DegreesToRadians(fov), 1.0f, near, far);

				Matrix4x4 view = Matrix4x4.CreateLookAt(position, position + direction, Vector3.UnitY);

				return view * proj;
		}
}
