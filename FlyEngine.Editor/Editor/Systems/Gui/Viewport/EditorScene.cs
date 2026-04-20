using ImGuiNET;

namespace FlyEngine.Editor.Systems.Gui;

public class EditorScene : EditorGuiWindow
{
    protected override string Title => "Scene";

    protected override void BeforeBegin()
    {
        ImGui.SetNextWindowDockID(EditorGui.CenterDockId);
    }

    protected override void OnRender(double deltaTime)
    {
        
    }
}