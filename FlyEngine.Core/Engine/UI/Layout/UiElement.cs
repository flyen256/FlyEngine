using System.Numerics;

namespace FlyEngine.Core.Engine.UI.Layout;

public class UiElement
{
    public readonly List<UiElement> Children = [];
    public Vector2 Size { get; protected set; } = Vector2.Zero;
    
    public virtual void Draw() {}
}