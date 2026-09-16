using System.Numerics;
using FlyEngine.Core.Assets;
using FlyEngine.Core.Components;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace FlyEngine.Core.Renderer;

public abstract class RenderPipeline(OpenGl openGl)
{
    protected readonly OpenGl OpenGl = openGl;
    protected GL Gl => OpenGl.Gl;
    
    public bool IsDeferredGeometryPass { get; protected set; }
    public bool IsShadowPass { get; protected set; }
    
    public uint DeferredLightVao { get; protected set; }
    public uint FinalFbo { get; protected set; }
    public uint FinalTexture { get; protected set; }
    public uint DepthTexture { get; protected set; }
    
    public abstract void Initialize(string vertexCode);
    public abstract void Render(float deltaTime, bool editor = false);
    public abstract void Submit(MeshRenderer renderer, SubMesh mesh, Matrix4x4 model);
    public abstract void CreateFinalFramebuffer(Vector2D<int> viewport);
    public abstract void ResizeGBuffer(Vector2D<int> viewport);
}