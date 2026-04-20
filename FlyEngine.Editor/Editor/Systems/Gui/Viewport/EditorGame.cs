using ImGuiNET;

namespace FlyEngine.Editor.Systems.Gui;

public class EditorGame : EditorGuiWindow
{
    protected override string Title => "Game";
    
    protected override void BeforeBegin()
    {
        ImGui.SetNextWindowDockID(EditorGui.CenterDockId);
    }
}