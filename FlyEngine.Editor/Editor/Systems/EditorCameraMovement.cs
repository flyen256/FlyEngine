using System.Numerics;
using FlyEngine.Core;
using FlyEngine.Core.Extensions;
using FlyEngine.Editor.Systems.Gui;
using Silk.NET.Maths;

namespace FlyEngine.Editor.Systems;

public class EditorCameraMovement : EditorSystem
{
    private Vector3 _rotation = Vector3.Zero;
    
    public override void OnUpdate(double deltaTime)
    {
        if (!EditorScene.ScenePressed) return;
        _rotation.X += Input.MouseInput.Y * 0.1f;
        _rotation.Y -= Input.MouseInput.X * 0.1f;
        _rotation.X = Math.Clamp(_rotation.X, -90f, 90f);
        Editor.SetCameraRotation(QuaternionUtils.FromVector3(_rotation)); 
        var moveInput = Input.GetMoveInput();
        if (moveInput == Vector2D<float>.Zero) return;
        var moveSpeed = 5.0f * (float)deltaTime;

        var inputVector = new Vector3(moveInput.X, 0, -moveInput.Y);

        var direction = Vector3.Transform(inputVector, Editor.GetCameraRotation());

        Editor.SetCameraPosition(Editor.GetCameraPosition() + direction * moveSpeed);
    }
}