using System.Numerics;
using FlyEngine.Core.Engine.UI.Layout;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Core.Engine.UI.Elements;

public class Button(string label, Button.OnClickDelegate onClickDelegate) : UiElement
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