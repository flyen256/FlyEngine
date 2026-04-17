using FlyEngine.Core.Engine.UI.Layout;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Core.Engine.UI.Elements;

public class Label(string text) : UiElement
{
    public override void Draw()
    {
        ImGuiNet.Text(text);
        Size = ImGuiNet.GetItemRectSize();
    }
}
