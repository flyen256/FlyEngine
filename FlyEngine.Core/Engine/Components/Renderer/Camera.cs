using FlyEngine.Core.Components.Common;

namespace FlyEngine.Core.Components.Renderer;

public class Camera : Component
{
    protected override void OnInitialize()
    {
        Application.Instance.Cameras.Add(this);
    }
}