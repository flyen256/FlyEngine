using System.Numerics;
using FlyEngine.Core.Engine.CustomAttributes;
using FlyEngine.Core.Engine.Math;

namespace FlyEngine.Core.Engine.Components.Renderer._3D;

public class Camera3D : Camera
{
    public float NearPlane { get; set; } = 0.01f;
    public float FarPlane { get; set; } = 5000.0f;
    [PropertyRange(1f, 179f)]
    public float Fov { get; set; } = 90f;

    public Matrix4x4 ViewMatrix { get; private set; }
    public Matrix4x4 ProjectionMatrix { get; private set; }

    public void UpdateMatrices(float aspectRatio)
    {
        var fov = MathHelper.DegreesToRadians(System.Math.Clamp(Fov, 1f, 179f));
        
        ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
            fov, 
            aspectRatio, 
            NearPlane, 
            FarPlane);
        var cameraWorldMatrix = Matrix4x4.CreateFromQuaternion(Transform.Rotation)
                                * Matrix4x4.CreateTranslation(Transform.Position);

        Matrix4x4.Invert(cameraWorldMatrix, out var view);
        ViewMatrix = view;
    }
}