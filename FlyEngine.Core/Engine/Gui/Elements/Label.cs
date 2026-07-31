using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Core.Gui;

public class Label(string text) : GuiElement
{
    public override void Draw()
    {
        ImGuiNet.Text(text);
        Size = ImGuiNet.GetItemRectSize();
    }
}