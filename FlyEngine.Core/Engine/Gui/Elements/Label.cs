using FlyEngine.Core.Engine.Gui.Layout;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Core.Engine.Gui.Elements;

public class Label(string text) : GuiElement
{
    public override void Draw()
    {
        ImGuiNet.Text(text);
        Size = ImGuiNet.GetItemRectSize();
    }
}