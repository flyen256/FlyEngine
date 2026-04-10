using FlyEngine.Components.Common;
using FlyEngine.Components.Renderer._2D;
using FlyEngine.Physics;
using Silk.NET.Maths;

namespace FlyEngine.Test;

public class PlayerMovement : Behaviour
{
    private Vector2D<float> _velocity = Vector2D<float>.Zero;
    private readonly float _speed = 1f;
    private readonly Camera2D? _camera;
    private CollisionResult _collisionResult;

    public PlayerMovement(Camera2D? camera, float speed = 1f)
    {
        _speed = speed;
        _camera = camera;
    }

    public override void OnLoad()
    {

    }

    public override void OnUpdate(double deltaTime)
    {
        _velocity = Input.GetMoveInput() * _speed;
        var targetPosition = Position;
        var currentPosition = _camera != null ? _camera.Position : Vector3D<float>.Zero;

        var lerpFactor = 0.05f;
        if(_camera != null && _camera.GameObject != null)
            _camera.GameObject.Transform.Position = currentPosition + (targetPosition - currentPosition) * lerpFactor;

        var newPosition = _velocity * (float)deltaTime;
        if(newPosition.Length > 0)
            Transform.Position += new Vector3D<float>(newPosition.X, newPosition.Y, 0f);
    }

    public override void OnCollision(CollisionResult result)
    {
        _collisionResult = result;
    }
}
