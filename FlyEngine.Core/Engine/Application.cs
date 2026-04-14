using FlyEngine.Core.Components.Common;
using FlyEngine.Core.Components.Renderer;
using FlyEngine.Core.Components.Renderer._3D;
using FlyEngine.Core.Components.Renderer.Lighting;
using FlyEngine.Core.Renderer;
using FlyEngine.Core.Renderer.Lighting;
using FlyEngine.Core.Renderer.Meshes;
using FlyEngine.Core.UI.ImGui;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace FlyEngine.Core;

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

    public DeferredEnvironment Environment { get; set; } = DeferredEnvironment.Default;

    public Camera? CurrentCamera { get; private set; }

    private bool _graphicsReady;

    private Action<Application> _scene;

    public Application(Action<Application> scene)
    {
        _scene = scene;
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
        Gl.RenderPipeline.ResizeGBuffer(newSize);
    }

    private void OnLoad()
    {
        Gl = new OpenGl(Window, Instance);
        Gl.Initialize();
        Gl.ProcessShaders();
        
        _scene.Invoke(this);

        foreach (var gameObject in GameObjects)
            gameObject.ComponentStore.InitializeComponents();
        
        Input.Initialize(Window);
        
        foreach (var behaviour in Behaviours.Where(behaviour => behaviour.IsActive()))
            behaviour.OnLoad();
        
        if (Input.InputContext != null)
            ImGui.Initialize(
                Gl.Gl,
                Window,
                Input.InputContext
            );

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
        
        Gl.RenderPipeline.Render(deltaTime);
        
        if (ImGui.Initialized)
            ImGui.Render((float)deltaTime);
    }
}
