using System.Numerics;
using FlyEngine.Core.Assets;
using FlyEngine.Core.CustomAttributes;
using FlyEngine.Core.Renderer;
using Silk.NET.OpenGL;

namespace FlyEngine.Core.Components;

public class MeshRenderer : Behaviour
{
    public Color AlbedoTint { get; set; } = Color.White;
    public float Metallic { get; set; }
    public float Smoothness { get; set; }
    [Serialize, ShowInInspector]
    private Mesh? _mesh;

    public override void OnRender(float deltaTime)
    {
        if (_mesh == null || Application.Window == null || Application.Window.OpenGl == null) return;
        var gl = Application.Window.OpenGl;
        var model = Transform.WorldMatrix;
        Render(gl, model);
    }

    private void Render(OpenGl gl, Matrix4x4 model)
    {
        if (_mesh == null) return;
        var shader = gl.RenderPipeline.GetRenderShader();

        shader.Use();
        _mesh.Bind();

        if (!gl.RenderPipeline.IsShadowPass)
        {
            gl.Gl.ActiveTexture(TextureUnit.Texture0);
            gl.Gl.BindTexture(TextureTarget.Texture2D, gl.DefaultWhiteTexture);

            shader.SetUniform(ShaderConstants.AlbedoTint, AlbedoTint.ToVector3());
            shader.SetUniform(ShaderConstants.Metallic, Metallic);
            shader.SetUniform(ShaderConstants.Smoothness, Smoothness);
        }

        shader.SetUniform(ShaderConstants.Model, model);
        
        Draw(gl);
    }

	private unsafe void Draw(OpenGl gl)
	{
		if (_mesh == null) return;
		gl.Gl.DrawElements(PrimitiveType.Triangles, _mesh.IndexCount, DrawElementsType.UnsignedInt, (void*)0);
	}
}
