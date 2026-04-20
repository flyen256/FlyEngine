using FlyEngine.Core.Engine.Components.Common;

namespace FlyEngine.Core.Engine.Components.Renderer;

public class Camera : Component
{
    public static Camera? CurrentCamera { get; set; }
}