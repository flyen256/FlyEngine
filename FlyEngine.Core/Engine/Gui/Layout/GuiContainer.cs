using System.Numerics;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Core.Engine.Gui.Layout;

public class GuiContainer : GuiElement
{
    public Orientation Orientation { get; set; } = Orientation.Vertical;
    
    public GuiContainer() {}

    public GuiContainer(Orientation orientation)
    {
        Orientation = orientation;
    }
    
    public override void Draw()
    {
        var size = Vector2.Zero;
        var i = 0;
        foreach (var child in Children)
        {
            ImGuiNet.PushID(i++);
            child.Draw();
        
            if (Orientation == Orientation.Horizontal)
            {
                size.X += child.Size.X;
                size.Y = System.Math.Max(size.Y, child.Size.Y);
                if (i < Children.Count) ImGuiNet.SameLine();
            }
            else
            {
                size.Y += child.Size.Y;
                size.X = System.Math.Max(size.X, child.Size.X);
            }
        
            ImGuiNet.PopID();
        }
        Size = size;
    }
}