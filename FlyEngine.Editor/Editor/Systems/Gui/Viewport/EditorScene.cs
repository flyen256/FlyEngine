using System.Numerics;
using FlyEngine.Core;
using FlyEngine.Core.Extensions;
using ImGuiNET;
using Silk.NET.Maths;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Editor.Systems.Gui;

public class EditorScene : EditorGuiWindow
{
    protected override string Title => "Scene";

    private bool _scenePressed;
    private Vector3 _rotation = Vector3.Zero;

    protected override void BeforeBegin()
    {
        ImGuiNet.SetNextWindowDockID(EditorGui.CenterDockId);
    }

    protected internal override void OnUpdate(double deltaTime)
    {
        if (!_scenePressed) return;
        _rotation.X += Input.MouseInput.Y * 0.1f;
        _rotation.Y -= Input.MouseInput.X * 0.1f;
        _rotation.X = System.Math.Clamp(_rotation.X, -90f, 90f);
        Editor.SetCameraRotation(QuaternionUtils.FromVector3(_rotation)); 
        var moveInput = Input.GetMoveInput();
        if (moveInput == Vector2D<float>.Zero) return;
        var moveSpeed = 5.0f * (float)deltaTime;

        var inputVector = new Vector3(moveInput.X, 0, -moveInput.Y);

        var direction = Vector3.Transform(inputVector, Editor.GetCameraRotation());

        Editor.SetCameraPosition(Editor.GetCameraPosition() + direction * moveSpeed);
    }

    protected override void OnRender(double deltaTime)
    {
        Editor.IsSceneOpened = true;
        if (Application.Window == null || Application.Window.OpenGl == null) return;
        var regionSize = ImGuiNet.GetContentRegionAvail();
        Editor.ViewportSize = new Vector2D<int>((int)regionSize.X, (int)regionSize.Y);
        var pipeline = Application.Window.OpenGl.RenderPipeline;
        if (pipeline.FinalTexture == 0) return;

        ImGuiNet.Image((IntPtr)Application.Window.OpenGl.RenderPipeline.FinalTexture, regionSize);
        if (ImGuiNet.IsItemHovered())
        {
            if (ImGuiNet.IsMouseDown(ImGuiMouseButton.Right))
            {
                _scenePressed = true;
                Input.CursorVisible = false;
            }
        }
        if (_scenePressed && ImGuiNet.IsMouseReleased(ImGuiMouseButton.Right))
        {
            _scenePressed = false;
            Input.CursorVisible = true;
        }
    }
}