using FlyEngine.Core.Gui.Layout;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Core.Gui.Elements;

public class Label(string text) : GuiElement
{
    public override void Draw()
    {
        ImGuiNet.Text(text);
        Size = ImGuiNet.GetItemRectSize();
    }
}