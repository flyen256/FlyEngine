using System.Numerics;
using FlyEngine.Components.Common;
using Silk.NET.Maths;

namespace FlyEngine.Components.Renderer._2D
{
    public static class MathHelper
    {
        public const float Pi = (float)Math.PI;
        public static float DegreesToRadians(float degrees) => degrees * Pi / 180f;
    }

    public class Camera2D : Component
    {
        public float Rotation { get; set; }
        public float Zoom { get; set; } = 1.0f;

        private Matrix4x4 _viewMatrix;
        private Matrix4x4 _projectionMatrix;

        public Matrix4x4 ViewMatrix => _viewMatrix;
        public Matrix4x4 ProjectionMatrix => _projectionMatrix;

        private readonly Vector3D<float> _initialPosition;

        public Camera2D(Vector3D<float> initialPosition)
        {
            _initialPosition = initialPosition;
        }

        protected override void OnInitialize()
        {
            Transform.Position = _initialPosition;
        }

        public void UpdateMatrices(int windowWidth, int windowHeight)
        {
            var aspectRatio = (float)windowWidth / windowHeight;
            var viewHeight = 2.0f / Zoom;
            var viewWidth = viewHeight * aspectRatio;

            _projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(
                -viewWidth / 2, viewWidth / 2,
                -viewHeight / 2, viewHeight / 2,
                -1.0f, 1.0f);

            var translationMatrix = Matrix4x4.CreateTranslation(-GameObject.Transform.Position.X, -GameObject.Transform.Position.Y, 0);
            var rotationMatrix = Matrix4x4.CreateRotationZ(MathHelper.DegreesToRadians(Rotation));

            _viewMatrix = rotationMatrix * translationMatrix;
        }
    }
}
