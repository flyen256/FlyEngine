using System.Numerics;
using Silk.NET.Input;

namespace FlyEngine.UI.Interfaces;

public interface IMouseEvents
{
    public void OnMouseMove(IMouse mouse, Vector2 position) { }
    public void OnMouseEnter(IMouse mouse, Vector2 position) { }
    public void OnMouseExit(IMouse mouse, Vector2 position) { }
}
