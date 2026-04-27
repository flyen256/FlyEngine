using System.Numerics;
using System.Text.Json.Serialization;
using FlyEngine.Core.CustomAttributes;
using FlyEngine.Core.Math;

namespace FlyEngine.Core.Components.Renderer._3D;

public class Camera3D : Camera
{
    [PropertyRange<float>(0.01f, 100f)]
    public float NearPlane { get; set; } = 0.01f;
    [PropertyRange<float>(1f, float.MaxValue)]
    public float FarPlane { get; set; } = 5000.0f;
    [PropertyRange<float>(1f, 179f)]
    public float Fov { get; set; } = 90f;

    [JsonIgnore]
    public Matrix4x4 ViewMatrix { get; private set; }
    [JsonIgnore]
    private Matrix4x4 _projectionMatrix = Matrix4x4.Identity;
    [JsonIgnore]
    public Matrix4x4 ProjectionMatrix
    {
        get => _projectionMatrix;
        private set => _projectionMatrix = value;
    }

    public void UpdateMatrices(float aspectRatio)
    {
        var fov = MathHelper.DegreesToRadians(System.Math.Clamp(Fov, 1f, 179f));
        
        ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
            fov, 
            aspectRatio, 
            NearPlane, 
            FarPlane);
        _projectionMatrix.M22 *= -1;

        Matrix4x4.Invert(Transform.WorldMatrix, out var view);
        ViewMatrix = view;
    }
}