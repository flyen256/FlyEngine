using System.Numerics;

namespace FlyEngine.Core.Gui.Layout;

public class GuiElement
{
    public readonly List<GuiElement> Children = [];
    public Vector2 Size { get; protected set; } = Vector2.Zero;
    
    public virtual void Draw() {}
}