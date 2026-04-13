using System.Drawing;
using System.Numerics;
using FlyEngine.Reactive;
using FlyEngine.Renderer.Common;
using FlyEngine.Renderer.Lighting;
using FlyEngine.Renderer.Meshes;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace FlyEngine.Renderer;

public class OpenGl
{
    public readonly GL Gl;
    public readonly ReactiveList<Texture> Textures = new();
    
    public uint DefaultWhiteTexture { get; private set; }
    public Mesh CubeMesh { get; }

    public bool IsDeferredGeometryPass { get; private set; }
    public bool IsShadowPass { get; private set; }
    
    public Shader ForwardGeometryShader { get; private set; }
    public Shader DeferredGeometryShader { get; private set; }
    public Shader DeferredLightShader { get; private set; }
    public Shader ShadowDepthShader { get; private set; }

    public int MaxDeferredLights { get; set; } = 24;
    public uint ShadowMapResolution { get; set; } = 4096;

    private BufferObject<float> _vbo;
    private BufferObject<uint> _ebo;
    private readonly Application _main;
    private readonly IWindow _window;

    private uint _gbufferFbo;
    private uint _gAlbedoMetallic;
    private uint _gNormalSmoothness;
    private uint _gDepth;
    private int _gbufferW;
    private int _gbufferH;

    private uint _deferredLightVao;
    private uint _deferredLightVbo;

    private uint _shadowFbo;
    private uint _shadowDepthTex;

    private uint _skyCubemap;

    public OpenGl(IWindow window, Application main)
    {
        _window = window;
        Gl = window.CreateOpenGL();
        CubeMesh = new Mesh(Gl);
        
        _main = main;

        Textures.OnAdd += texture => texture.Load();
        Textures.OnRemove += texture => texture.UnLoad();
    }

    public unsafe void Initialize()
    {
        Gl.Viewport(0, 0, (uint)_window.Size.X, (uint)_window.Size.Y);
        _main.AspectRatio = (float)_window.Size.X / _window.Size.Y;
        Gl.Enable(EnableCap.DepthTest);
        Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        Gl.ClearColor(Color.Gray);

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

    public void BindBuffers()
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

        _vbo = new BufferObject<float>(Gl, vertices, BufferTargetARB.ArrayBuffer);
        _ebo = new BufferObject<uint>(Gl, indices, BufferTargetARB.ElementArrayBuffer);
        CubeMesh.Vao = new VertexArrayObject<float, uint>(Gl, _vbo, _ebo);
        CubeMesh.IndexCount = (uint)indices.Length;
    }

    private string? LoadShaderCode(string shader)
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

        BuildDeferredPrograms(vertexCode);
        BuildShadowProgram();
        CreateShadowFramebuffer();
        CreateDeferredLightQuad();
        ResizeGBuffer(_window.Size.X, _window.Size.Y);
    }

    private void BuildDeferredPrograms(string vertexWorldCode)
    {
        var geomFs = LoadShaderCode("deferred_geom.frag");
        if (geomFs == null)
            throw new Exception("deferred_geom.frag not found in resources!");
        DeferredGeometryShader = new Shader(Gl, vertexWorldCode, geomFs);

        var lightVsCode = LoadShaderCode("deferred_light.vert");
        var lightFsCode = LoadShaderCode("deferred_light.frag");
        if (lightVsCode == null || lightFsCode == null)
            throw new Exception("Deferred light shaders not found in resources!");
        DeferredLightShader = new Shader(Gl,lightVsCode, lightFsCode);
    }

    private void BuildShadowProgram()
    {
        var vs = LoadShaderCode("shadow_depth.vert");
        var fs = LoadShaderCode("shadow_depth.frag");
        if (vs == null || fs == null)
            throw new Exception("shadow_depth shaders not found in resources!");
        ShadowDepthShader = new Shader(Gl, vs, fs);
    }

    private unsafe void CreateShadowFramebuffer()
    {
        _shadowDepthTex = Gl.GenTexture();
        Gl.BindTexture(TextureTarget.Texture2D, _shadowDepthTex);
        Gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.DepthComponent24, ShadowMapResolution, ShadowMapResolution, 0,
            PixelFormat.DepthComponent, PixelType.UnsignedInt, null);
        var linear = (int)TextureMinFilter.Linear;
        Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, in linear);
        Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, in linear);
        var nearest = (int)TextureMinFilter.Nearest;
        Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, in nearest);
        Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, in nearest);
        var wrapEdge = (int)TextureWrapMode.ClampToBorder;
        Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, in wrapEdge);
        Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, in wrapEdge);
        var border = stackalloc float[] { 1f, 1f, 1f, 1f };
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, border);
        Gl.BindTexture(TextureTarget.Texture2D, 0);

        _shadowFbo = Gl.GenFramebuffer();
        Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _shadowFbo);
        Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D,
            _shadowDepthTex, 0);
        Gl.DrawBuffer(DrawBufferMode.None);
        Gl.ReadBuffer(ReadBufferMode.None);
        var status = Gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != GLEnum.FramebufferComplete)
            throw new Exception($"Shadow framebuffer incomplete: {status}");
        Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }
    
    public unsafe void CreateProceduralSkyCubemap()
    {
        const int size = 128;
        _skyCubemap = Gl.GenTexture();
        Gl.BindTexture(TextureTarget.TextureCubeMap, _skyCubemap);
        var wrap = (int)TextureWrapMode.ClampToEdge;
        var linear = (int)TextureMinFilter.Linear;
        Gl.TexParameterI(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, in wrap);
        Gl.TexParameterI(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, in wrap);
        Gl.TexParameterI(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, in wrap);
        Gl.TexParameterI(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, in linear);
        Gl.TexParameterI(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, in linear);

        ReadOnlySpan<TextureTarget> faces =
        [
            TextureTarget.TextureCubeMapPositiveX,
            TextureTarget.TextureCubeMapNegativeX,
            TextureTarget.TextureCubeMapPositiveY,
            TextureTarget.TextureCubeMapNegativeY,
            TextureTarget.TextureCubeMapPositiveZ,
            TextureTarget.TextureCubeMapNegativeZ
        ];

        var faceBytes = new byte[size * size * 3];
        for (var f = 0; f < 6; f++)
        {
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var u = (x + 0.5f) / size * 2f - 1f;
                    var v = (y + 0.5f) / size * 2f - 1f;
                    var dir = CubeFaceDirection(f, u, v);
                    var c = SampleProceduralSky(dir);
                    var i = (y * size + x) * 3;
                    faceBytes[i] = (byte)(System.Math.Clamp(c.X, 0f, 1f) * 255f);
                    faceBytes[i + 1] = (byte)(System.Math.Clamp(c.Y, 0f, 1f) * 255f);
                    faceBytes[i + 2] = (byte)(System.Math.Clamp(c.Z, 0f, 1f) * 255f);
                }
            }

            fixed (byte* p = faceBytes)
                Gl.TexImage2D(faces[f], 0, InternalFormat.Rgb, size, size, 0, PixelFormat.Rgb, PixelType.UnsignedByte, p);
        }

        Gl.BindTexture(TextureTarget.TextureCubeMap, 0);
    }

    private static Vector3 CubeFaceDirection(int face, float u, float v)
    {
        return face switch
        {
            0 => Vector3.Normalize(new Vector3(1f, -v, -u)),
            1 => Vector3.Normalize(new Vector3(-1f, -v, u)),
            2 => Vector3.Normalize(new Vector3(u, 1f, v)),
            3 => Vector3.Normalize(new Vector3(u, -1f, -v)),
            4 => Vector3.Normalize(new Vector3(u, -v, 1f)),
            _ => Vector3.Normalize(new Vector3(-u, -v, -1f))
        };
    }

    private static float Smooth(float e0, float e1, float x)
    {
        var t = System.Math.Clamp((x - e0) / (e1 - e0 + 1e-6f), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    private static Vector3 SampleProceduralSky(Vector3 d)
    {
        d = Vector3.Normalize(d);

        var y = d.Y;

        var topSky = new Vector3(0.40f, 0.65f, 1.00f);
        var midSky = new Vector3(0.65f, 0.85f, 1.00f);
        var horizon = new Vector3(1.00f, 0.75f, 0.55f);
        var ground = new Vector3(0.25f, 0.35f, 0.20f);

        var t1 = Smooth(-0.2f, 0.6f, y);
        var t2 = Smooth(0.2f, 0.9f, y);

        var sky = Vector3.Lerp(horizon, midSky, t1);
        sky = Vector3.Lerp(sky, topSky, t2);

        var groundFade = Smooth(-1.0f, 0.0f, y);
        sky = Vector3.Lerp(ground, sky, groundFade);

        sky = Vector3.Lerp(sky, Vector3.One * sky.Length(), 0.08f);

        return sky;
    }

    public void RenderShadowPass(in Matrix4x4 lightSpaceMatrix, Action<double> drawMeshes, double deltaTime)
    {
        if (_shadowFbo == 0 || ShadowDepthShader.Handle == 0)
            return;
        
        Gl.Viewport(0, 0, ShadowMapResolution, ShadowMapResolution);
        Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _shadowFbo);
        Gl.Clear(ClearBufferMask.DepthBufferBit);
        Gl.Enable(EnableCap.DepthTest);
        
        Gl.Disable(EnableCap.CullFace);

        Gl.Enable(EnableCap.PolygonOffsetFill);
        Gl.PolygonOffset(1.1f, 2f);
        Gl.DepthFunc(DepthFunction.Less);

        IsShadowPass = true;
        ShadowDepthShader.Use();
        ShadowDepthShader.SetUniform(ShaderConstants.LightMatrix, lightSpaceMatrix);

        drawMeshes(deltaTime);

        IsShadowPass = false;
        Gl.Disable(EnableCap.PolygonOffsetFill);
        Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    private unsafe void CreateDeferredLightQuad()
    {
        ReadOnlySpan<float> tri =
        [
            -1f, -1f,
            3f, -1f,
            -1f, 3f
        ];
        _deferredLightVao = Gl.GenVertexArray();
        _deferredLightVbo = Gl.GenBuffer();
        Gl.BindVertexArray(_deferredLightVao);
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _deferredLightVbo);
        fixed (float* p = tri)
            Gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(tri.Length * sizeof(float)), p, BufferUsageARB.StaticDraw);
        Gl.EnableVertexAttribArray(0);
        Gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), (void*)0);
        Gl.BindVertexArray(0);
    }

    public void ResizeGBuffer(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return;

        _gbufferW = width;
        _gbufferH = height;

        if (_gbufferFbo != 0)
        {
            Gl.DeleteFramebuffer(_gbufferFbo);
            Gl.DeleteTexture(_gAlbedoMetallic);
            Gl.DeleteTexture(_gNormalSmoothness);
            Gl.DeleteTexture(_gDepth);
            _gbufferFbo = 0;
        }

        unsafe
        {
            _gAlbedoMetallic = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2D, _gAlbedoMetallic);
            Gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba16f, (uint)width, (uint)height, 0, PixelFormat.Rgba,
                PixelType.Float, null);
            var linear = (int)TextureMinFilter.Linear;
            Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, in linear);
            Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, in linear);
            var wrapEdge = (int)TextureWrapMode.ClampToEdge;
            Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, in wrapEdge);
            Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, in wrapEdge);

            _gNormalSmoothness = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2D, _gNormalSmoothness);
            Gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba16f, (uint)width, (uint)height, 0, PixelFormat.Rgba,
                PixelType.Float, null);
            Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, in linear);
            Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, in linear);
            Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, in wrapEdge);
            Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, in wrapEdge);

            _gDepth = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2D, _gDepth);
            Gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.DepthComponent24, (uint)width, (uint)height, 0,
                PixelFormat.DepthComponent, PixelType.UnsignedInt, null);
            var nearest = (int)TextureMinFilter.Nearest;
            Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, in nearest);
            Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, in nearest);
            Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, in wrapEdge);
            Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, in wrapEdge);

            Gl.BindTexture(TextureTarget.Texture2D, 0);

            _gbufferFbo = Gl.GenFramebuffer();
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _gbufferFbo);
            Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, _gAlbedoMetallic, 0);
            Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1,
                TextureTarget.Texture2D, _gNormalSmoothness, 0);
            Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
                TextureTarget.Texture2D, _gDepth, 0);

            Span<GLEnum> drawBufs =
            [
                GLEnum.ColorAttachment0,
                GLEnum.ColorAttachment1
            ];
            Gl.DrawBuffers(drawBufs);

            var status = Gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != GLEnum.FramebufferComplete)
                throw new Exception($"G-buffer framebuffer incomplete: {status}");

            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }
        CreateProceduralSkyCubemap();
    }

    public void BeginDeferredGeometryPass(in Matrix4x4 projection, in Matrix4x4 view)
    {
        Gl.Disable(EnableCap.Blend);
        Gl.Enable(EnableCap.DepthTest);
        Gl.DepthFunc(DepthFunction.Less);

        Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _gbufferFbo);
        Gl.Viewport(0, 0, (uint)_gbufferW, (uint)_gbufferH);
        Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        IsDeferredGeometryPass = true;
        DeferredGeometryShader.Use();
        DeferredGeometryShader.SetUniform(ShaderConstants.Projection, projection);
        DeferredGeometryShader.SetUniform(ShaderConstants.View, view);
    }

    public void FinishDeferredLightingPass(
        in Matrix4x4 projection,
        in Matrix4x4 view,
        Vector2 viewport,
        in Vector3 cameraPos,
        ReadOnlySpan<DeferredLightPacked> lights,
        in Vector3 ambient,
        in Vector3 clearColor,
        float ditherStrength,
        in DeferredEnvironment environment)
    {
        Matrix4x4.Invert(projection, out var invProj);
        Matrix4x4.Invert(view, out var invView);

        Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        Gl.Viewport(0, 0, (uint)viewport.X, (uint)viewport.Y);
        Gl.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, 1f);
        Gl.Clear(ClearBufferMask.ColorBufferBit);

        Gl.Disable(EnableCap.DepthTest);
        Gl.Disable(EnableCap.Blend);

        DeferredLightShader.Use();

        Gl.ActiveTexture(TextureUnit.Texture0);
        Gl.BindTexture(TextureTarget.Texture2D, _gAlbedoMetallic);
        DeferredLightShader.SetUniform(ShaderConstants.AlbedoMetallic, 0);

        Gl.ActiveTexture(TextureUnit.Texture1);
        Gl.BindTexture(TextureTarget.Texture2D, _gNormalSmoothness);
        DeferredLightShader.SetUniform(ShaderConstants.NormalSmoothness, 1);

        Gl.ActiveTexture(TextureUnit.Texture2);
        Gl.BindTexture(TextureTarget.Texture2D, _gDepth);
        DeferredLightShader.SetUniform(ShaderConstants.Depth, 2);

        Gl.ActiveTexture(TextureUnit.Texture3);
        Gl.BindTexture(TextureTarget.Texture2D, _shadowDepthTex);
        DeferredLightShader.SetUniform(ShaderConstants.ShadowMap, 3);

        Gl.ActiveTexture(TextureUnit.Texture4);
        Gl.BindTexture(TextureTarget.TextureCubeMap, _skyCubemap);
        DeferredLightShader.SetUniform(ShaderConstants.Skybox, 4);

        DeferredLightShader.SetUniform(ShaderConstants.ShadowEnabled, environment.ShadowEnabled ? 1 : 0);
        DeferredLightShader.SetUniform(ShaderConstants.ShadowDirIndex, environment.ShadowDirectionalLightIndex);
        DeferredLightShader.SetUniform(ShaderConstants.LightSpaceMatrix, environment.LightSpaceMatrix);
        
        DeferredLightShader.SetUniform(ShaderConstants.SunDirWorld, environment.SunDirectionWorld);

        DeferredLightShader.SetUniform(ShaderConstants.FogEnabled, environment.FogEnabled ? 1 : 0);
        DeferredLightShader.SetUniform(ShaderConstants.FogDensity, environment.FogDensity);
        DeferredLightShader.SetUniform(ShaderConstants.FogHeight, environment.FogHeight);
        DeferredLightShader.SetUniform(ShaderConstants.FogFalloff, environment.FogHeightFalloff);
        DeferredLightShader.SetUniform(ShaderConstants.FogScatter, environment.FogScattering);
        DeferredLightShader.SetUniform(ShaderConstants.FogColor, environment.FogColor);

        DeferredLightShader.SetUniform(ShaderConstants.ViewportSize, viewport);

        DeferredLightShader.SetUniform(ShaderConstants.InverseProjection, invProj);
        DeferredLightShader.SetUniform(ShaderConstants.InverseView, invView);

        DeferredLightShader.SetUniform(ShaderConstants.CameraPosition, cameraPos);
        DeferredLightShader.SetUniform(ShaderConstants.AmbientColor, ambient);
        DeferredLightShader.SetUniform(ShaderConstants.DitherStrength, ditherStrength);

        var n = System.Math.Min(lights.Length, MaxDeferredLights);
        DeferredLightShader.SetUniform(ShaderConstants.NumLights, n);
        var zero = Vector4.Zero;
        for (var i = 0; i < MaxDeferredLights; i++)
        {
            var pk = i < n ? lights[i] : default;
            var p0 = i < n ? pk.Pack0 : zero;
            var p1 = i < n ? pk.Pack1 : zero;
            var p2 = i < n ? pk.Pack2 : zero;
            var p3 = i < n ? pk.Pack3 : zero;
            var p4 = i < n ? pk.Pack4 : zero;
            DeferredLightShader.SetUniform(ShaderConstants.Pack(0, i), p0);
            DeferredLightShader.SetUniform(ShaderConstants.Pack(1, i), p1);
            DeferredLightShader.SetUniform(ShaderConstants.Pack(2, i), p2);
            DeferredLightShader.SetUniform(ShaderConstants.Pack(3, i), p3);
            DeferredLightShader.SetUniform(ShaderConstants.Pack(4, i), p4);
        }

        Gl.BindVertexArray(_deferredLightVao);
        Gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
        Gl.BindVertexArray(0);

        Gl.Enable(EnableCap.DepthTest);
        Gl.Enable(EnableCap.Blend);
        Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        Gl.ClearColor(128f / 255f, 128f / 255f, 128f / 255f, 1f);
        Gl.ActiveTexture(TextureUnit.Texture4);
        Gl.BindTexture(TextureTarget.TextureCubeMap, 0);
        Gl.ActiveTexture(TextureUnit.Texture3);
        Gl.BindTexture(TextureTarget.Texture2D, 0);
        Gl.ActiveTexture(TextureUnit.Texture2);
        Gl.BindTexture(TextureTarget.Texture2D, 0);
        Gl.ActiveTexture(TextureUnit.Texture1);
        Gl.BindTexture(TextureTarget.Texture2D, 0);
        Gl.ActiveTexture(TextureUnit.Texture0);
        Gl.BindTexture(TextureTarget.Texture2D, 0);
        IsDeferredGeometryPass = false;
    }
}
