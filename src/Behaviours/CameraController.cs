using System.Numerics;
using FlyEngine.Components.Common;
using FlyEngine.Extensions;
using Silk.NET.Maths;

namespace FlyEngine.Behaviours;

public class CameraController : Behaviour
{
    public float Sensitivity = 0.1f;

    private Vector3 _rotation = Vector3.Zero;

    public override void OnLoad()
    {
        Input.CursorLocked = true;
        Input.CursorVisible = false;
    }

    public override void OnUpdate(double deltaTime)
    {
        _rotation.X += Input.MouseInput.Y * Sensitivity;
        _rotation.Y -= Input.MouseInput.X * Sensitivity;
        _rotation.X = System.Math.Clamp(_rotation.X, -90f, 90f);
        Transform.Rotation = QuaternionUtils.FromVector3(_rotation);
        var moveInput = Input.GetMoveInput();
        if (moveInput == Vector2D<float>.Zero) return;
        var moveSpeed = 5.0f * (float)deltaTime;

        var inputVector = new Vector3(moveInput.X, 0, -moveInput.Y);

        var direction = Vector3.Transform(inputVector, Transform.Rotation);

        Transform.Position += direction * moveSpeed;
    }
}