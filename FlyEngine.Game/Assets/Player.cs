using System.Numerics;
using FlyEngine.Core.Components;
using FlyEngine.Core.Debugging;
using FlyEngine.Core.Input;
using FlyEngine.Core.Serialization;
using FlyEngine.Core.Utils;
using JoltPhysicsSharp;
using Character = FlyEngine.Core.Components.Character;

namespace FlyEngine.Game;

public class Player : Character
{
    public ComponentRef<Camera>? Camera { get; set; }
    
    public float Sensitivity = 0.1f;

    private Vector3 _rotation = Vector3.Zero;

    public override void OnLoad()
    {
        base.OnLoad();
        Input.LockAndHideCursor();
    }

    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);
        
        UpdateCameraRotation();
        UpdatePhysics(deltaTime);
        UpdateMovement();
    }

    private void UpdatePhysics(float deltaTime)
    {
        const float gravity = -9.81f;
        if (GroundState == GroundState.InAir)
        {
            var verticalVelocity = Velocity.Y + (gravity * deltaTime);
            Velocity.Y = verticalVelocity;
        }
        else
            Velocity.Y = gravity;
    }

    private void UpdateMovement()
    {
        var moveInput = Input.GetMoveInput();
        const float moveSpeed = 5.0f;

        var playerRotation = Transform.Rotation;

        var forward = Vector3.Transform(-Vector3.UnitZ, playerRotation);
        var right = Vector3.Transform(Vector3.UnitX, playerRotation);

        forward.Y = 0;
        right.Y = 0;
        
        forward = Vector3.Normalize(forward);
        right = Vector3.Normalize(right);

        var direction = (forward * moveInput.Y) + (right * moveInput.X);
        var velocity = direction * moveSpeed;

        Velocity.X = velocity.X;
        Velocity.Z = velocity.Z;
    }

    private void UpdateCameraRotation()
    {
        _rotation.X += Input.MouseInput.Y * Sensitivity;
        _rotation.Y -= Input.MouseInput.X * Sensitivity;
        _rotation.X = Math.Clamp(_rotation.X, -89.9f, 89.9f);

        Transform.Rotation = QuaternionUtils.FromVector3(new Vector3(0, _rotation.Y, 0));

        if (Camera?.Value == null) return;
        Camera.Value.Transform.LocalRotation = QuaternionUtils.FromVector3(new Vector3(_rotation.X, 0, 0));
    }
}