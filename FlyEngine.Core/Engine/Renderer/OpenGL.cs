using FlyEngine.Core.Assets;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ARB;
using Silk.NET.Windowing;

namespace FlyEngine.Core.Renderer;

public class OpenGl
{
    private static Guid CubeMeshGuid => Guid.Parse("ac8dab28-605c-4020-8e74-7c1a1a5f2a95");
    private static Guid SphereMeshGuid => Guid.Parse("d91df955-1522-4520-928b-c62abc363eb4");
    
    public readonly GL Gl;
    public ArbBindlessTexture BindlessTexture;

    private uint _defaultWhiteTexture;
    public static ulong DefaultWhiteTexture { get; private set; }

    public int MaxDeferredLights { get; set; } = 48;
    public uint ShadowMapResolution { get; set; } = 4096;
	public uint ShadowMapTileSize { get; set; } = 1024;

    public RenderPipeline RenderPipeline { get; set; }
    public PostProcessVolume PostProcessVolume { get; set; }
    public MeshBudgetManager MeshBudgetManager { get; }
    
    public readonly IWindow Window;

    public OpenGl(IWindow window)
    {
        RenderPipeline = new DefaultDeferredRenderPipeline(this);
        Window = window;
        Gl = window.CreateOpenGL();
        PostProcessVolume = new PostProcessVolume(Gl);
        MeshBudgetManager = new MeshBudgetManager(Gl);
        CreateCubeMesh();
    }

    public unsafe bool Initialize(out string message)
    {
        message = string.Empty;
        if (!Gl.TryGetExtension(out BindlessTexture))
        {
            message = $"GL_ARB_bindless_texture not supported on your GPU";
            return false;
        }
        Gl.Viewport(0, 0, (uint)Window.Size.X, (uint)Window.Size.Y);
        Gl.Enable(EnableCap.DepthTest);
        Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        Gl.ClearColor(0, 0, 0, 0);
        
        Span<byte> white = [255, 255, 255, 255];
        
        _defaultWhiteTexture = Gl.CreateTexture(TextureTarget.Texture2D);
        Gl.TextureStorage2D(_defaultWhiteTexture, 1, SizedInternalFormat.Rgba8, 1, 1);
        var linear = (int)TextureMinFilter.Linear;
        Gl.TextureParameter(_defaultWhiteTexture, TextureParameterName.TextureMinFilter, in linear);
        Gl.TextureParameter(_defaultWhiteTexture, TextureParameterName.TextureMagFilter, in linear);
        
        fixed (void* ptr = white)
            Gl.TextureSubImage2D(_defaultWhiteTexture, 0, 0, 0, 1, 1, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
        
        var handle = BindlessTexture.GetTextureHandle(_defaultWhiteTexture);
        BindlessTexture.MakeTextureHandleResident(handle);
        DefaultWhiteTexture = handle;
        
        var vertexCode = LoadEmbeddedResourceShaderCode("vertex.vert");
        
        if (vertexCode == null)
            throw new Exception("Shaders not found in resources!");

        const uint stride = 8 * sizeof(float);

        Gl.EnableVertexAttribArray(0);
        Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);

        Gl.EnableVertexAttribArray(1);
        Gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));

        Gl.EnableVertexAttribArray(2);
        Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, stride, (void*)(5 * sizeof(float)));

        Gl.BindVertexArray(0);

        RenderPipeline.Initialize(vertexCode);
        return true;
    }

    private void CreateCubeMesh()
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
        Mesh.Create("Cube", CubeMeshGuid, this, vertices, indices, (uint)indices.Length);
        var meshes = ModelManager.LoadModelMeshesFromAssembly(this, "sphere.fbx");
        if (meshes.Count == 1)
            meshes[0].Guid = SphereMeshGuid;
    }

    public static Shader? LoadEmbeddedResourceShader(GL gl, string shaderName)
    {
        var vs = LoadEmbeddedResourceShaderCode($"{shaderName}.vert");
        var fs = LoadEmbeddedResourceShaderCode($"{shaderName}.frag");
        if (vs == null || fs == null)
            throw new Exception($"{shaderName} shaders not found in resources!");
        return new Shader(gl, vs, fs);
    }

    public static string? LoadEmbeddedResourceShaderCode(string shader)
    {
        var assembly = typeof(OpenGl).Assembly;
    
        var names = assembly.GetManifestResourceNames();
        var findName = names.ToList().Find(s => s.Contains(shader));
        if (findName == null) return null;

        using var stream = assembly.GetManifestResourceStream(findName);
        if (stream == null) return null;
        using var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();
        return string.IsNullOrWhiteSpace(text) ? null : text.TrimStart('\uFEFF');
    }
}
