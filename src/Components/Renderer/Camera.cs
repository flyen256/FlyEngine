using FlyEngine.Components.Common;

namespace FlyEngine.Components.Renderer;

public class Camera : Component
{
    protected override void OnInitialize()
    {
        Application.Instance.Cameras.Add(this);
    }
}