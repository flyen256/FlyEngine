using FlyEngine.Core.Gui.Layout;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Core.Gui.Elements;

public class Button(string label, Button.OnClickDelegate onClickDelegate) : GuiElement
{
    public bool Enabled { get; set; } = true;

    public delegate void OnClickDelegate(Button button);

    public override void Draw()
    {
        ImGuiNet.BeginDisabled(!Enabled);
        if (ImGuiNet.Button(label))
            onClickDelegate.Invoke(this);
        Size = ImGuiNet.GetItemRectSize();
        ImGuiNet.EndDisabled();
    }
}