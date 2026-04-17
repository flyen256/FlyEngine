using System.Numerics;
using FlyEngine.Core.Engine.Components.Renderer._3D;
using FlyEngine.Core.Engine.Components.Renderer.Lighting;
using FlyEngine.Core.Engine.Renderer.Common;
using FlyEngine.Core.Engine.Renderer.Lighting;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace FlyEngine.Core.Engine.Renderer.Pipelines;

public class DefaultPipeline(OpenGl openGl) : RenderPipeline(openGl)
{
    public Shader DeferredGeometryShader { get; private set; }
    public Shader DeferredLightShader { get; private set; }
    public Shader ShadowDepthShader { get; private set; }
    
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
    
    public override void Render(double deltaTime)
    {
        var application = OpenGl.Application;
        if (application.CurrentCamera is not Camera3D camera3D) return;
        var projection = camera3D.ProjectionMatrix;
        var view = camera3D.ViewMatrix;
        BeginDeferredGeometryPass(projection, view);
        foreach (var behaviour in application.Behaviours.Where(behaviour => behaviour.IsActive()))
            behaviour.OnRender(deltaTime);

        var camPos = camera3D.Transform.Position;
        var camPosSys = new Vector3(camPos.X, camPos.Y, camPos.Z);
        Span<DeferredLightPacked> lightBuf = stackalloc DeferredLightPacked[OpenGl.MaxDeferredLights];
        var lightCount = 0;
        var sunLightIndex = -1;
        LightSource? sunLight = null;
        foreach (var light in application.Lights)
        {
            if (!light.IsActive()) continue;
            if (lightCount >= OpenGl.MaxDeferredLights) return;

            if (sunLightIndex < 0 && light.CastShadows && light.Type == LightType.Directional)
            {
                sunLightIndex = lightCount;
                sunLight = light;
            }
            lightBuf[lightCount++] = light.BuildPacked();
        }

        var lightSpace = Matrix4x4.Identity;
        var sunDirection = new Vector3(0.2f, 0.85f, 0.35f);
        if (sunLightIndex >= 0 && sunLight != null)
        {
            var rot = Matrix4x4.CreateFromQuaternion(sunLight.Transform.Rotation);
            var packForward = Vector3.Normalize(Vector3.Transform(-Vector3.UnitZ, rot));
            var lightDir = Vector3.Normalize(packForward);
            lightSpace = ShadowMapping.CreateLightSpaceMatrix(
                lightDir,
                camPosSys,
                30f,
                0.01f,
                300f
            );
            sunDirection = -packForward;
        }

        RenderShadowPass(lightSpace, dt =>
        {
            foreach (var behaviour in application.Behaviours.Where(behaviour => behaviour.IsActive()))
                behaviour.OnRender(dt);
        }, deltaTime);

        FinishDeferredLightingPass(
            projection,
            view,
            application.Window.Size,
            camPosSys,
            lightBuf[..lightCount],
            1f,
            application.Environment,
            sunLightIndex,
            sunDirection,
            lightSpace);
        Gl.Clear((uint)ClearBufferMask.DepthBufferBit);
    }
    
    public override void ProcessShaders(string vertexCode)
    {
        BuildDeferredPrograms(vertexCode);
        BuildShadowProgram();
        CreateShadowFramebuffer();
        CreateDeferredLightQuad();
        ResizeGBuffer(OpenGl.Window.Size);
    }

    public override Shader GetRenderShader()
    {
        return IsShadowPass
            ? ShadowDepthShader
            : IsDeferredGeometryPass
                ? DeferredGeometryShader
                : OpenGl.ForwardGeometryShader;
    }

    private void BuildDeferredPrograms(string vertexWorldCode)
    {
        var geomFs = OpenGl.LoadShaderCode("deferred_geom.frag");
        if (geomFs == null)
            throw new Exception("deferred_geom.frag not found in resources!");
        DeferredGeometryShader = new Shader(Gl, vertexWorldCode, geomFs);

        var lightVsCode = OpenGl.LoadShaderCode("deferred_light.vert");
        var lightFsCode = OpenGl.LoadShaderCode("deferred_light.frag");
        if (lightVsCode == null || lightFsCode == null)
            throw new Exception("Deferred light shaders not found in resources!");
        DeferredLightShader = new Shader(Gl,lightVsCode, lightFsCode);
    }

    private void BuildShadowProgram()
    {
        var vs = OpenGl.LoadShaderCode("shadow_depth.vert");
        var fs = OpenGl.LoadShaderCode("shadow_depth.frag");
        if (vs == null || fs == null)
            throw new Exception("shadow_depth shaders not found in resources!");
        ShadowDepthShader = new Shader(Gl, vs, fs);
    }

    private unsafe void CreateShadowFramebuffer()
    {
        _shadowDepthTex = Gl.GenTexture();
        Gl.BindTexture(TextureTarget.Texture2D, _shadowDepthTex);
        Gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.DepthComponent24, OpenGl.ShadowMapResolution, OpenGl.ShadowMapResolution, 0,
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
        
        Gl.Viewport(0, 0, OpenGl.ShadowMapResolution, OpenGl.ShadowMapResolution);
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

    public override void ResizeGBuffer(Vector2D<int> viewport)
    {
        var width = viewport.X;
        var height = viewport.Y;
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
        Vector2D<int> viewport,
        in Vector3 cameraPos,
        ReadOnlySpan<DeferredLightPacked> lights,
        float ditherStrength,
        in DeferredEnvironment environment,
        int sunLightIndex,
        in Vector3 sunDirection,
        in Matrix4x4 lightSpaceMatrix)
    {
        Matrix4x4.Invert(projection, out var invProj);
        Matrix4x4.Invert(view, out var invView);

        Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        Gl.Viewport(0, 0, (uint)viewport.X, (uint)viewport.Y);
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
        DeferredLightShader.SetUniform(ShaderConstants.ShadowDirIndex, sunLightIndex);
        DeferredLightShader.SetUniform(ShaderConstants.LightSpaceMatrix, lightSpaceMatrix);
        
        DeferredLightShader.SetUniform(ShaderConstants.SunDirWorld, sunDirection);

        DeferredLightShader.SetUniform(ShaderConstants.FogEnabled, environment.FogEnabled ? 1 : 0);
        DeferredLightShader.SetUniform(ShaderConstants.FogDensity, environment.FogDensity);
        DeferredLightShader.SetUniform(ShaderConstants.FogHeight, environment.FogHeight);
        DeferredLightShader.SetUniform(ShaderConstants.FogFalloff, environment.FogHeightFalloff);
        DeferredLightShader.SetUniform(ShaderConstants.FogScatter, environment.FogScattering);
        DeferredLightShader.SetUniform(ShaderConstants.FogColor, environment.FogColor);

        DeferredLightShader.SetUniform(ShaderConstants.ViewportSize, new Vector2(viewport.X, viewport.Y));

        DeferredLightShader.SetUniform(ShaderConstants.InverseProjection, invProj);
        DeferredLightShader.SetUniform(ShaderConstants.InverseView, invView);

        DeferredLightShader.SetUniform(ShaderConstants.CameraPosition, cameraPos);
        DeferredLightShader.SetUniform(ShaderConstants.AmbientColor, environment.AmbientColor);
        DeferredLightShader.SetUniform(ShaderConstants.DitherStrength, ditherStrength);

        var n = System.Math.Min(lights.Length, OpenGl.MaxDeferredLights);
        DeferredLightShader.SetUniform(ShaderConstants.NumLights, n);
        var zero = Vector4.Zero;
        for (var i = 0; i < OpenGl.MaxDeferredLights; i++)
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