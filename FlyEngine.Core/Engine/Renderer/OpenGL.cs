using System.Drawing;
using FlyEngine.Core.Engine.Reactive;
using FlyEngine.Core.Engine.Renderer.Meshes;
using FlyEngine.Core.Engine.Renderer.Pipelines;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace FlyEngine.Core.Engine.Renderer;

public class OpenGl
{
    public readonly GL Gl;
    public readonly ReactiveList<Texture> Textures = new();
    
    public uint DefaultWhiteTexture { get; private set; }
    
    public Mesh CubeMesh { get; }
    
    public Shader ForwardGeometryShader { get; private set; }

    public int MaxDeferredLights { get; set; } = 24;
    public uint ShadowMapResolution { get; set; } = 4096;

    public RenderPipeline RenderPipeline { get; set; }
    
    public readonly Application Application;
    public readonly IWindow Window;

    public OpenGl(IWindow window, Application application)
    {
        RenderPipeline = new DefaultPipeline(this);
        Window = window;
        Gl = window.CreateOpenGL();
        CubeMesh = CreateCubeMesh();
        
        Application = application;

        Textures.OnAdd += texture => texture.Load();
        Textures.OnRemove += texture => texture.UnLoad();
    }

    public unsafe void Initialize()
    {
        Gl.Viewport(0, 0, (uint)Window.Size.X, (uint)Window.Size.Y);
        Application.AspectRatio = (float)Window.Size.X / Window.Size.Y;
        Gl.Enable(EnableCap.DepthTest);
        Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        Gl.ClearColor(Color.Black);

        DefaultWhiteTexture = Gl.GenTexture();
        Gl.BindTexture(TextureTarget.Texture2D, DefaultWhiteTexture);
        Span<byte> white = [255, 255, 255, 255];
        fixed (byte* p = white)
            Gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, 1, 1, 0, PixelFormat.Rgba, PixelType.UnsignedByte, p);
        var linear = (int)TextureMinFilter.Linear;
        Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, in linear);
        Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, in linear);
        Gl.BindTexture(TextureTarget.Texture2D, 0);
    }

    public Mesh CreateCubeMesh()
    {
        float[] vertices =
        [
            -0.5f, -0.5f,  0.5f,  0.0f, 1.0f,   0.0f,  0.0f,  1.0f,
            0.5f, -0.5f,  0.5f,  1.0f, 1.0f,   0.0f,  0.0f,  1.0f,
            0.5f,  0.5f,  0.5f,  1.0f, 0.0f,   0.0f,  0.0f,  1.0f,
            -0.5f,  0.5f,  0.5f,  0.0f, 0.0f,   0.0f,  0.0f,  1.0f,

            -0.5f, -0.5f, -0.5f,  1.0f, 1.0f,   0.0f,  0.0f, -1.0f,
            0.5f, -0.5f, -0.5f,  0.0f, 1.0f,   0.0f,  0.0f, -1.0f,
            0.5f,  0.5f, -0.5f,  0.0f, 0.0f,   0.0f,  0.0f, -1.0f,
            -0.5f,  0.5f, -0.5f,  1.0f, 0.0f,   0.0f,  0.0f, -1.0f,

            -0.5f,  0.5f, -0.5f,  0.0f, 1.0f,   0.0f,  1.0f,  0.0f,
            0.5f,  0.5f, -0.5f,  1.0f, 1.0f,   0.0f,  1.0f,  0.0f,
            0.5f,  0.5f,  0.5f,  1.0f, 0.0f,   0.0f,  1.0f,  0.0f,
            -0.5f,  0.5f,  0.5f,  0.0f, 0.0f,   0.0f,  1.0f,  0.0f,

            -0.5f, -0.5f, -0.5f,  1.0f, 1.0f,   0.0f, -1.0f,  0.0f,
            0.5f, -0.5f, -0.5f,  0.0f, 1.0f,   0.0f, -1.0f,  0.0f,
            0.5f, -0.5f,  0.5f,  0.0f, 0.0f,   0.0f, -1.0f,  0.0f,
            -0.5f, -0.5f,  0.5f,  1.0f, 0.0f,   0.0f, -1.0f,  0.0f,

            0.5f, -0.5f, -0.5f,  1.0f, 1.0f,   1.0f,  0.0f,  0.0f,
            0.5f,  0.5f, -0.5f,  1.0f, 0.0f,   1.0f,  0.0f,  0.0f,
            0.5f,  0.5f,  0.5f,  0.0f, 0.0f,   1.0f,  0.0f,  0.0f,
            0.5f, -0.5f,  0.5f,  0.0f, 1.0f,   1.0f,  0.0f,  0.0f,

            -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,  -1.0f,  0.0f,  0.0f,
            -0.5f,  0.5f, -0.5f,  0.0f, 0.0f,  -1.0f,  0.0f,  0.0f,
            -0.5f,  0.5f,  0.5f,  1.0f, 0.0f,  -1.0f,  0.0f,  0.0f,
            -0.5f, -0.5f,  0.5f,  1.0f, 1.0f,  -1.0f,  0.0f,  0.0f
        ];

        uint[] indices =
        [
            0,  1,  2,  2,  3,  0,
            4,  5,  6,  6,  7,  4,
            8,  9,  10, 10, 11, 8,
            12, 13, 14, 14, 15, 12,
            16, 17, 18, 18, 19, 16,
            20, 21, 22, 22, 23, 20
        ];

        return new Mesh(Gl, vertices, indices, (uint)indices.Length);
    }

    public string? LoadShaderCode(string shader)
    {
        var assembly = typeof(OpenGl).Assembly;
    
        var names = assembly.GetManifestResourceNames();
        var findName = names.ToList().Find(s => s.Contains(shader));
        if (findName == null) return null;

        using var stream = assembly.GetManifestResourceStream(findName);
        if (stream == null) return null;
        using var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();
        if (string.IsNullOrWhiteSpace(text))
            return null;
        return text.TrimStart('\uFEFF');
    }

    public unsafe void ProcessShaders()
    {
        var vertexCode = LoadShaderCode("vertex.vert");
        var fragmentCode = LoadShaderCode("fragment.frag");
        
        if (vertexCode == null || fragmentCode == null)
            throw new Exception("Shaders not found in resources!");
        ForwardGeometryShader = new Shader(Gl, vertexCode, fragmentCode);

        const uint stride = 8 * sizeof(float);

        Gl.EnableVertexAttribArray(0);
        Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);

        Gl.EnableVertexAttribArray(1);
        Gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));

        Gl.EnableVertexAttribArray(2);
        Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, stride, (void*)(5 * sizeof(float)));

        Gl.BindVertexArray(0);

        RenderPipeline.ProcessShaders(vertexCode);
    }
}
