using Silk.NET.OpenGL;

namespace FlyEngine.Core.Renderer;

public abstract class PostProcessEffect<T>(Shader shader, T data) : IPostProcessEffect where T : struct
{
    protected T Data = data;
    protected readonly Shader Shader = shader;
    
    public abstract void Render(OpenGl gl);
}