using System.Numerics;
using Silk.NET.Maths;

namespace Flyeng
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

        private Vector3D<float> _initialPosition;

        public Camera2D(Vector3D<float> initialPosition)
        {
            if (initialPosition != null)
                _initialPosition = initialPosition;
        }

        protected override void OnInitialize()
        {
            if(Transform == null) return;
            Transform.Position = _initialPosition;
        }

        public void UpdateMatrices(int windowWidth, int windowHeight)
        {
            if (GameObject == null) return;
            float aspectRatio = (float)windowWidth / windowHeight;
            float viewHeight = 2.0f / Zoom;
            float viewWidth = viewHeight * aspectRatio;

            _projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(
                -viewWidth / 2, viewWidth / 2,
                -viewHeight / 2, viewHeight / 2,
                -1.0f, 1.0f);

            Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(-GameObject.Transform.Position.X, -GameObject.Transform.Position.Y, 0);
            Matrix4x4 rotationMatrix = Matrix4x4.CreateRotationZ(MathHelper.DegreesToRadians(Rotation));

            _viewMatrix = rotationMatrix * translationMatrix;
        }
    }
}
