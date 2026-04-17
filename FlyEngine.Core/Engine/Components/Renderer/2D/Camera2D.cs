using System.Numerics;

namespace FlyEngine.Core.Engine.Components.Renderer._2D;

public class Camera2D : Camera
{
    public float Zoom { get; set; } = 1.0f;

    public Matrix4x4 ViewMatrix { get; private set; }
    public Matrix4x4 ProjectionMatrix { get; private set; }

    public void UpdateMatrices(int windowWidth, int windowHeight)
    {
        var aspectRatio = (float)windowWidth / windowHeight;
        var viewHeight = 2.0f / Zoom;
        var viewWidth = viewHeight * aspectRatio;

        ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(
            -viewWidth / 2, viewWidth / 2,
            -viewHeight / 2, viewHeight / 2,
            -1.0f, 1.0f);

        var translationMatrix = Matrix4x4.CreateTranslation(-GameObject.Transform.Position.X, -GameObject.Transform.Position.Y, 0);
        var rotationMatrix = Matrix4x4.CreateFromQuaternion(Transform.Rotation);

        ViewMatrix = rotationMatrix * translationMatrix;
    }
}