using System.Numerics;
using FlyEngine.Behaviours;
using FlyEngine.Components.Common;
using FlyEngine.Components.Physics;
using FlyEngine.Components.Physics.Colliders;
using FlyEngine.Components.Renderer;
using FlyEngine.Components.Renderer._3D;
using FlyEngine.Components.Renderer._3D.Meshes;
using FlyEngine.Components.Renderer.Lighting;
using FlyEngine.Extensions;
using FlyEngine.Renderer;
using FlyEngine.Renderer.Lighting;
using FlyEngine.Renderer.Meshes;
using FlyEngine.UI.ImGui;
using JoltPhysicsSharp;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace FlyEngine;

public class Application
{
    private static Application? _instance;
    public static Application Instance => _instance ?? throw new NullReferenceException("Application.Instance");
    
    public readonly IWindow Window;
    public readonly ModelManager ModelManager;
    public readonly Physics.Physics Physics;
    
    public OpenGl Gl { get; private set; }
    
    public readonly List<Behaviour> Behaviours = [];
    public readonly List<GameObject> GameObjects = [];
    public readonly List<Camera> Cameras = [];
    public readonly List<LightSource> Lights = [];
    public float AspectRatio;

    public Camera CurrentCamera { get; private set; }

    private bool _graphicsReady;

    public Application()
    {
        _instance = this;
        var windowOptions = WindowOptions.Default with
        {
            Size = new Vector2D<int>(1024, 768),
            Title = "My first silk.net window",
            VSync = false,
            FramesPerSecond = 144,
            WindowState = WindowState.Fullscreen
        };
        Window = Silk.NET.Windowing.Window.Create(windowOptions);
        Physics = new Physics.Physics();
        ModelManager = new ModelManager();

        Window.Load += OnLoad;
        Window.Update += OnUpdate;
        Window.Render += OnRender;
        Window.FramebufferResize += OnResize;

        Window.Run();
    }

    private void OnResize(Vector2D<int> newSize)
    {
        AspectRatio = (float)newSize.X / newSize.Y;
        if (!_graphicsReady) return;
        Gl.Gl.Viewport(0, 0, (uint)newSize.X, (uint)newSize.Y);
        Gl.ResizeGBuffer(newSize.X, newSize.Y);
    }

    private void OnLoad()
    {
        Gl = new OpenGl(Window, Instance);
        Gl.Initialize();
        Gl.BindBuffers();
        Gl.ProcessShaders();
        
        Input.Initialize(Window);
        
        foreach (var behaviour in Behaviours.Where(behaviour => behaviour.IsActive()))
        {
            behaviour.OnLoad();
        }
        if (Input.InputContext != null)
        {
            ImGui.Initialize(
                Gl.Gl,
                Window,
                Input.InputContext
            );
        }

        _graphicsReady = true;
    }

    private void OnUpdate(double deltaTime)
    {
        Input.Update(deltaTime);
        Physics.System.Update((float)deltaTime, 1, Physics.JobSystem);
        foreach (var behaviour in Behaviours.Where(behaviour => behaviour.IsActive()))
        {
            behaviour.OnUpdate(deltaTime);
        }
    }

    private unsafe void OnRender(double deltaTime)
    {
        var activeCameras = Cameras.Where(camera => camera.IsActive()).ToList();
        var camera3D = activeCameras.OfType<Camera3D>().First(c => c.IsActive());
        CurrentCamera = camera3D;

        camera3D.UpdateMatrices(AspectRatio);
        var projection = camera3D.ProjectionMatrix;
        var view = camera3D.ViewMatrix;

        Gl.BeginDeferredGeometryPass(projection, view);
        foreach (var behaviour in Behaviours.Where(behaviour => behaviour.IsActive()))
            behaviour.OnRender(deltaTime);

        var camPos = camera3D.Transform.Position;
        var camPosSys = new Vector3(camPos.X, camPos.Y, camPos.Z);
        Span<DeferredLightPacked> lightBuf = stackalloc DeferredLightPacked[Gl.MaxDeferredLights];
        var lightCount = 0;
        var shadowDirIndex = -1;
        LightSource? sunLight = null;
        foreach (var light in Lights)
        {
            if (!light.IsActive() || lightCount >= Gl.MaxDeferredLights) continue;

            if (shadowDirIndex < 0 && light.CastShadows && light.Type == LightType.Directional)
            {
                shadowDirIndex = lightCount;
                sunLight = light;
            }
            lightBuf[lightCount++] = light.BuildPacked();
        }

        var lightSpace = Matrix4x4.Identity;
        var sunDirWorld = new Vector3(0.2f, 0.85f, 0.35f);
        sunDirWorld = Vector3.Normalize(sunDirWorld);
        if (shadowDirIndex >= 0 && sunLight != null)
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
            sunDirWorld = Vector3.Normalize(-packForward);
        }

        Gl.RenderShadowPass(lightSpace, dt =>
        {
            foreach (var behaviour in Behaviours.Where(behaviour => behaviour.IsActive()))
                behaviour.OnRender(dt);
        }, deltaTime);

        var environment = new DeferredEnvironment
        {
            ShadowEnabled = shadowDirIndex >= 0,
            ShadowDirectionalLightIndex = shadowDirIndex < 0 ? 0 : shadowDirIndex,
            LightSpaceMatrix = lightSpace,
            SunDirectionWorld = sunDirWorld,
            FogEnabled = false,
            FogDensity = 0.2f,
            FogHeight = 1.5f,
            FogHeightFalloff = 0.7f,
            FogScattering = 0.5f,
            FogColor = new Vector3(1f, 1f, 1f)
        };

        Gl.FinishDeferredLightingPass(
            projection,
            view,
            new Vector2(Window.Size.X, Window.Size.Y),
            camPosSys,
            lightBuf[..(int)lightCount],
            new Vector3(0.04f, 0.045f, 0.06f),
            new Vector3(128f / 255f, 128f / 255f, 128f / 255f),
            1f,
            environment);
        Gl.Gl.Clear((uint)ClearBufferMask.DepthBufferBit);

        if (ImGui.Initialized)
            ImGui.Render((float)deltaTime);
    }
}
