using System.Numerics;
using FlyEngine.Core.Assets;
using FlyEngine.Core.Components.Common;
using FlyEngine.Core.Renderer;
using FlyEngine.Core.Renderer.Common;
using FlyEngine.Core.Renderer.Types;
using Silk.NET.OpenGL;

namespace FlyEngine.Core.Components.Renderer._3D.Meshes;

public class MeshRenderer : Behaviour
{
    public Color AlbedoTint { get; set; } = Color.White;
    public float Metallic { get; set; }
    public float Smoothness { get; set; }
    public Mesh? Mesh { get; set; }

    public override void OnRender(double deltaTime)
    {
        if (Mesh == null || Application.Window == null || Application.Window.OpenGl == null) return;
        var gl = Application.Window.OpenGl;
        var model = Transform.WorldMatrix;
        Render(gl, model);
    }

    private unsafe void Render(OpenGl gl, Matrix4x4 model)
    {
        if (Mesh == null) return;
        var shader = gl.RenderPipeline.GetRenderShader();

        shader.Use();
        Mesh.Bind();

        if (!gl.RenderPipeline.IsShadowPass)
        {
            gl.Gl.ActiveTexture(TextureUnit.Texture0);
            gl.Gl.BindTexture(TextureTarget.Texture2D, gl.DefaultWhiteTexture);

            shader.SetUniform(ShaderConstants.AlbedoTint, AlbedoTint.ToVector3());
            shader.SetUniform(ShaderConstants.Metallic, Metallic);
            shader.SetUniform(ShaderConstants.Smoothness, Smoothness);
        }

        shader.SetUniform(ShaderConstants.Model, model);

        gl.Gl.DrawElements(PrimitiveType.Triangles, Mesh.IndexCount, DrawElementsType.UnsignedInt, (void*)0);
    }
}