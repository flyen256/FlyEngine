using System.Numerics;
using FlyEngine.Core.Components;
using Silk.NET.OpenGL;

namespace FlyEngine.Core.Renderer;

public class FogPostEffect(Shader shader, FogPostEffectData data) : PostProcessEffect<FogPostEffectData>(shader, data)
{
    public override void Render(OpenGl openGl)
    {
        return;
        var camera = Camera.CurrentCamera;
        if (camera == null && !Application.IsEditor) return;
        var window = Application.Window;
        if (window == null) return;
        var pipeline = openGl.RenderPipeline;
        var gl = openGl.Gl;
    
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, pipeline.FinalTexture);
        Shader.SetUniform(ShaderConstants.LightedTexture, 0);

        gl.ActiveTexture(TextureUnit.Texture1);
        gl.BindTexture(TextureTarget.Texture2D, pipeline.DepthTexture);
        Shader.SetUniform(ShaderConstants.DepthTexture, 1);
    
        var view = camera?.ViewMatrix ?? window.EditorCameraViewMatrix;
        var proj = camera?.ProjectionMatrix ?? window.EditorCameraProjectionMatrix;

        Matrix4x4.Invert(proj, out var invProj);
        Matrix4x4.Invert(view, out var invView);
        var invProjView = invProj * invView; 
        Shader.SetUniform(ShaderConstants.InvProjView, invProjView);
        Shader.SetUniform(ShaderConstants.CameraPosition, camera?.Transform.Position ?? 
                                                          window.EditorCameraPosition);
        Shader.SetUniform(ShaderConstants.LightDir, LightSource.SunLightSource?.Transform.Forward ?? Vector3.Zero);
        Shader.SetUniform(ShaderConstants.LightColor, LightSource.SunLightSource?.Color.ToVector3() ?? Vector3.One);
        Shader.SetUniform(ShaderConstants.FogSteps, System.Math.Max(Data.FogSteps, 1));
        Shader.SetUniform(ShaderConstants.FogDensity, Data.FogDensity);
        Shader.SetUniform(ShaderConstants.FogColor, Data.FogColor);
        
        gl.BindVertexArray(pipeline.DeferredLightVao);
        gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
        gl.BindVertexArray(0);
    }
}