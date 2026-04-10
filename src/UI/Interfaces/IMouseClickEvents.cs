using Silk.NET.Input;

namespace FlyEngine.UI.Interfaces;

public interface IMouseClickEvents
{
    public void OnMouseDown(IMouse mouse, MouseButton button) { }
    public void OnMouseUp(IMouse mouse, MouseButton button) { }
}
