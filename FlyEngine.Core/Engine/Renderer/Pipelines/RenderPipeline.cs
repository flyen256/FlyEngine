using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace FlyEngine.Core.Engine.Renderer.Pipelines;

public abstract class RenderPipeline(OpenGl openGl)
{
    protected readonly OpenGl OpenGl = openGl;
    protected GL Gl => OpenGl.Gl;
    
    public bool IsDeferredGeometryPass { get; protected set; }
    public bool IsShadowPass { get; protected set; }
    
    public abstract void Render(double deltaTime);
    public abstract Shader GetRenderShader();
    public abstract void ProcessShaders(string vertexCode);
    public abstract void ResizeGBuffer(Vector2D<int> viewport);
}