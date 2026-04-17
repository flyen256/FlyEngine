using FlyEngine.Core.Engine.Components.Common;

namespace FlyEngine.Core.Engine.Components.Renderer;

public class Camera : Component
{
    protected override void OnInitialize()
    {
        Application.Instance.Cameras.Add(this);
    }

    protected internal override void OnRemoved()
    {
        Application.Instance.Cameras.Remove(this);
    }
}