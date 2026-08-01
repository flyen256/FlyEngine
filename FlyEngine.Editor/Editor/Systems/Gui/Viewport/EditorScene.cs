using FlyEngine.Core;
using FlyEngine.Core.Components;
using FlyEngine.Core.Input;
using ImGuiNET;
using Silk.NET.Maths;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Editor.Systems;

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

        if (ImGuiNet.IsItemHovered() && ImGuiNet.IsMouseDown(ImGuiMouseButton.Right))
        {
            ScenePressed = true;
            Input.LockAndHideCursor();
        }

        if (ScenePressed && ImGuiNet.IsMouseReleased(ImGuiMouseButton.Right))
        {
            ScenePressed = false;
            Input.UnlockAndShowCursor();
        }
        
        if (ImGuiNet.Begin("Operation", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse))
        {
            if (ImGuiNet.Selectable("Translate", _operation == EditorGizmo.Operation.Translate))
                _operation = EditorGizmo.Operation.Translate;
            if (ImGuiNet.Selectable("Rotate", _operation == EditorGizmo.Operation.Rotate))
                _operation = EditorGizmo.Operation.Rotate;
        }
        ImGuiNet.End();

        if (Selection.SelectedObject == null) return;

        var selectedObject = Selection.SelectedObject;

        if (selectedObject is not GameObject gameObject) return;
        
        var screenPosLocal = EditorGizmo.WorldToScreen(gameObject.Transform.Position);
        var screenPosAbs = screenPosLocal + windowPos;
        
        var drawList = ImGuiNet.GetForegroundDrawList();
        drawList.PushClipRect(windowPos, windowPos + regionSize, true);
        EditorGizmo.DrawGizmo(_operation, drawList, gameObject, screenPosAbs, windowPos);
        drawList.PopClipRect();
    }
}
