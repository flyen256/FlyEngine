using Silk.NET.OpenGL;

namespace FlyEngine.Core.Renderer;

public interface IPostProcessEffect
{
    public void Render(OpenGl gl);
}