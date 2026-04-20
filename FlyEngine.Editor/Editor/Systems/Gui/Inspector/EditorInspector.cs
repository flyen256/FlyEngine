using System.Numerics;
using FlyEngine.Core.Engine.Components.Common;
using FlyEngine.Core.Engine.Extensions;
using ImGuiNET;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Editor.Systems.Gui;

public class EditorInspector : EditorGuiWindow
{
    protected override string Title => "Inspector";

    private GameObject? SelectedGameObject => EditorHierarchy.Instance?.SelectedGameObject;
    private GameObject? _lastSelected;
    private Vector3 _eulerRotation;
    
    protected override void BeforeBegin()
    {
        ImGuiNet.SetNextWindowDockID(EditorGui.RightDockId);
    }

    protected override void OnRender(double deltaTime)
    {
        if (EditorHierarchy.Instance == null || SelectedGameObject == null) return;
        if (_lastSelected != SelectedGameObject)
        {
            _lastSelected = SelectedGameObject;
            _eulerRotation = SelectedGameObject.Transform.Rotation.ToEulerAngles();
        }
        RenderTransform();
    }

    private void RenderTransform()
    {
        if (EditorHierarchy.Instance == null || SelectedGameObject == null) return;
        if (ImGuiNet.CollapsingHeader($"Transform##{SelectedGameObject.Name}", ImGuiTreeNodeFlags.AllowOverlap | ImGuiTreeNodeFlags.DefaultOpen))
        {
            var transform = SelectedGameObject.Transform;
            var pos = transform.Position;
            if (ImGuiNet.DragFloat3("Position", ref pos, 0.1f))
            {
                transform.Position = pos;
                EditorAction.MarkDirty();
            }

            var scale = transform.Scale;
            if (ImGuiNet.DragFloat3("Scale", ref scale, 0.1f))
            {
                transform.Scale = scale;
                EditorAction.MarkDirty();
            }

            if (ImGuiNet.DragFloat3("Rotation", ref _eulerRotation, 0.5f))
            {
                transform.Rotation = QuaternionUtils.FromVector3(_eulerRotation);
                EditorAction.MarkDirty();
            }
            else
            {
                var actualEuler = transform.Rotation.ToEulerAngles();
                if (Vector3.Distance(actualEuler, _eulerRotation) > 0.01f)
                    _eulerRotation = actualEuler;
            }

            ImGui.Separator();
        }
    }
}