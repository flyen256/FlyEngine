using Silk.NET.Input;

namespace FlyEngine.Core.Input;

public interface IKeyEvents
{
    public void OnKeyDown(Key key, int keyCode) {}
    public void OnKeyUp(Key key, int keyCode) {}
}