using Silk.NET.OpenGL;

namespace FlyEngine.Core.Renderer;

public class PostProcessVolume
{
    public readonly List<IPostProcessEffect> Effects = [];

    public PostProcessVolume(GL gl)
    {
        var fogShader = OpenGl.LoadEmbeddedResourceShader(gl, "fog");
        if (fogShader != null) Effects.Add(new FogPostEffect(fogShader, FogPostEffectData.Default));
    }
}