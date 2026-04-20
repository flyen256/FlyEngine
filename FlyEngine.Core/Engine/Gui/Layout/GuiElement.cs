using System.Numerics;

namespace FlyEngine.Core.Engine.Gui.Layout;

public class GuiElement
{
    public readonly List<GuiElement> Children = [];
    public Vector2 Size { get; protected set; } = Vector2.Zero;
    
    public virtual void Draw() {}
}