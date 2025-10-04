using Silk.NET.Input;

namespace Flyeng.Events;

public interface IMouseClickEvents
{
    public void OnMouseDown(IMouse mouse, MouseButton button) { }
    public void OnMouseUp(IMouse mouse, MouseButton button) { }
}
