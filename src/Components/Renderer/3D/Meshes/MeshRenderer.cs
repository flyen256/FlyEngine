using System.Numerics;
using FlyEngine.Components.Common;
using FlyEngine.Renderer.Common;
using FlyEngine.Renderer.Meshes;
using Silk.NET.OpenGL;

namespace FlyEngine.Components.Renderer._3D.Meshes;

public class MeshRenderer : Behaviour
{
    public Vector3 AlbedoTint { get; set; } = Vector3.One;
    public float Metallic { get; set; }
    public float Smoothness { get; set; }
    public Mesh? Mesh { get; set; }

    public override unsafe void OnRender(double deltaTime)
    {
        if (Mesh == null) return;
        var gl = Application.Instance.Gl;
        var shader = gl.IsShadowPass
            ? gl.ShadowDepthShader
            : gl.IsDeferredGeometryPass
                ? gl.DeferredGeometryShader
                : gl.ForwardGeometryShader;

        shader.Use();
        Mesh.Bind();

        if (!gl.IsShadowPass)
        {
            gl.Gl.ActiveTexture(TextureUnit.Texture0);
            gl.Gl.BindTexture(TextureTarget.Texture2D, gl.DefaultWhiteTexture);

            shader.SetUniform(ShaderConstants.AlbedoTint, AlbedoTint);
            shader.SetUniform(ShaderConstants.Metallic, Metallic);
            shader.SetUniform(ShaderConstants.Smoothness, Smoothness);
        }

        var model =
            Matrix4x4.CreateScale(Transform.Size) *
            Matrix4x4.CreateFromQuaternion(Transform.Rotation) *
            Matrix4x4.CreateTranslation(Transform.Position);

        shader.SetUniform(ShaderConstants.Model, model);

        gl.Gl.DrawElements(PrimitiveType.Triangles, Mesh.IndexCount, DrawElementsType.UnsignedInt, (void*)0);
    }
}