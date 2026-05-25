using System.Numerics;
using FlyEngine.Core;
using ImGuiNET;
using Silk.NET.Maths;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Editor.Systems.Gui;

public class EditorScene : EditorGuiWindow
{
    protected override string Title => "Scene";

    private EditorGizmo.Operation _operation;

    public static bool ScenePressed { get; private set; }

    protected override void BeforeBegin()
    {
        ImGuiNet.SetNextWindowDockID(EditorGui.CenterDockId);
    }

    protected override void OnRender(double deltaTime)
    {
        Editor.IsSceneOpened = true;
        if (Application.Window == null || Editor.Window == null || Application.Window.OpenGl == null) return;
        var windowPos = ImGuiNet.GetCursorScreenPos();
        var regionSize = ImGuiNet.GetContentRegionAvail();
        var mousePos = ImGuiNet.GetMousePos();
        Editor.Window.EditorViewport = new Vector2D<int>((int)regionSize.X, (int)regionSize.Y);
        var pipeline = Application.Window.OpenGl.RenderPipeline;
        if (pipeline.FinalTexture == 0) return;

        ImGuiNet.Image((IntPtr)Application.Window.OpenGl.RenderPipeline.FinalTexture, regionSize);
        
        if (ImGuiNet.IsItemHovered())
        {
            if (ImGuiNet.IsMouseDown(ImGuiMouseButton.Right))
            {
                ScenePressed = true;
                Input.CursorVisible = false;
            }
        }
        
        if (ScenePressed && ImGuiNet.IsMouseReleased(ImGuiMouseButton.Right))
        {
            ScenePressed = false;
            Input.CursorVisible = true;
        }
        
        if (ImGuiNet.Begin("Operation", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse))
        {
            if (ImGuiNet.Selectable("Translate", _operation == EditorGizmo.Operation.Translate))
                _operation = EditorGizmo.Operation.Translate;
            if (ImGuiNet.Selectable("Rotate", _operation == EditorGizmo.Operation.Rotate))
                _operation = EditorGizmo.Operation.Rotate;
        }
        ImGuiNet.End();

        if (Editor.SelectionManager.SelectedGameObject == null) return;

        var selectedObject = Editor.SelectionManager.SelectedGameObject;
        
        var screenPosLocal = EditorGizmo.WorldToScreen(selectedObject.Transform.Position);
        var screenPosAbs = screenPosLocal + windowPos;
        
        var drawList = ImGuiNet.GetForegroundDrawList();
        drawList.PushClipRect(windowPos, windowPos + regionSize, true);
        EditorGizmo.DrawGizmo(_operation, drawList, selectedObject.Transform, screenPosAbs, windowPos);
        drawList.PopClipRect();
    }

    public static bool RayIntersectsAabb(Vector3 rayOrigin, Vector3 rayDir, Vector3 boxMin, Vector3 boxMax, out float distance)
    {
        distance = 0f;
        var t1 = (boxMin.X - rayOrigin.X) / rayDir.X;
        var t2 = (boxMax.X - rayOrigin.X) / rayDir.X;
        var t3 = (boxMin.Y - rayOrigin.Y) / rayDir.Y;
        var t4 = (boxMax.Y - rayOrigin.Y) / rayDir.Y;
        var t5 = (boxMin.Z - rayOrigin.Z) / rayDir.Z;
        var t6 = (boxMax.Z - rayOrigin.Z) / rayDir.Z;

        var min = Math.Max(Math.Max(Math.Min(t1, t2), Math.Min(t3, t4)), Math.Min(t5, t6));
        var max = Math.Min(Math.Min(Math.Max(t1, t2), Math.Max(t3, t4)), Math.Max(t5, t6));

        if (max < 0 || min > max) return false;

        distance = min;
        return true;
    }
    
    private (Vector3 Origin, Vector3 Direction) ScreenPointToRay(
        Vector2 screenPos,
        Vector2 viewportSize)
    {
        var viewMatrix = Editor.Window?.EditorCameraViewMatrix;
        var projectionMatrix = Editor.Window?.EditorCameraProjectionMatrix;
        if (!viewMatrix.HasValue || !projectionMatrix.HasValue)
            return (Vector3.Zero, Vector3.Zero);
        var x = (2.0f * screenPos.X) / viewportSize.X - 1.0f;
        var y = 1.0f - (2.0f * screenPos.Y) / viewportSize.Y;
        var rayStartNdc = new Vector4(x, y, 0.0f, 1.0f);
        var rayEndNdc = new Vector4(x, y, 1.0f, 1.0f);
        Matrix4x4.Invert(viewMatrix.Value * projectionMatrix.Value, out var invViewProj);
        var rayStartWorld = Vector4.Transform(rayStartNdc, invViewProj);
        var rayEndWorld = Vector4.Transform(rayEndNdc, invViewProj);
        var startPoint = new Vector3(rayStartWorld.X, rayStartWorld.Y, rayStartWorld.Z) / rayStartWorld.W;
        var endPoint = new Vector3(rayEndWorld.X, rayEndWorld.Y, rayEndWorld.Z) / rayEndWorld.W;
        var direction = Vector3.Normalize(endPoint - startPoint);
        return (startPoint, direction);
    }
}
