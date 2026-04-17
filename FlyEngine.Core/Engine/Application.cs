using FlyEngine.Core.Engine.Components.Common;
using FlyEngine.Core.Engine.Components.Renderer;
using FlyEngine.Core.Engine.Components.Renderer._3D;
using FlyEngine.Core.Engine.Components.Renderer.Lighting;
using FlyEngine.Core.Engine.Renderer;
using FlyEngine.Core.Engine.Renderer.Lighting;
using FlyEngine.Core.Engine.Renderer.Meshes;
using FlyEngine.Core.Engine.UI;
using FlyEngine.Core.Engine.UI.ImGui;
using FlyEngine.Core.Engine.UI.Layout.Interfaces;
using FlyEngine.Core.Engine.Window;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace FlyEngine.Core.Engine;

public class Application : IDisposable
{
    private static Application? _instance;
    public static Application Instance => _instance ?? throw new NullReferenceException("Application.Instance");
    
    public static bool IsRunning { get; private set; }
    
    public readonly IWindow Window;
    public readonly ModelManager ModelManager;
    public readonly Physics.Physics Physics;
    
    public OpenGl Gl { get; private set; }
    
    public readonly List<Behaviour> Behaviours = [];
    public readonly List<GameObject> GameObjects = [];
    public readonly List<Camera> Cameras = [];
    public readonly List<LightSource> Lights = [];
    public readonly List<IUiRenderer> UiRenderers = [];
    public float AspectRatio;

    public DeferredEnvironment Environment { get; set; } = DeferredEnvironment.Default;

    public Camera? CurrentCamera { get; private set; }

    private bool _graphicsReady;

    private readonly Action<Application> _scene;
    private Vector2D<int> _minWindowSize;

    public Application(Action<Application> scene, ApplicationWindowOptions windowOptions)
    {
        _instance = this;
        _scene = scene;
        _minWindowSize = windowOptions.MinSize;
        Window = Silk.NET.Windowing.Window.Create(windowOptions.AsWindowOptions());
        Physics = new Physics.Physics();
        ModelManager = new ModelManager();

        Window.Load += OnLoad;
        Window.Update += OnUpdate;
        Window.Render += OnRender;
        Window.Resize += OnResize;
        Window.FramebufferResize += OnFramebufferResize;
    }

    public void Run()
    {
        IsRunning = true;
        Window.Run();
    }

    public void Dispose()
    {
        IsRunning = false;
        Window.Close();
        Window.Dispose();
    }

    private void OnResize(Vector2D<int> newSize)
    {
        var targetSize = newSize;
        if (newSize.X < _minWindowSize.X)
            targetSize.X = _minWindowSize.X;
        if (newSize.Y < _minWindowSize.Y)
            targetSize.Y = _minWindowSize.Y;
        Window.Size = targetSize;
    }

    private void OnFramebufferResize(Vector2D<int> newSize)
    {
        var targetSize = newSize;
        AspectRatio = (float)targetSize.X / targetSize.Y;
        if (!_graphicsReady) return;
        Gl.Gl.Viewport(0, 0, (uint)targetSize.X, (uint)targetSize.Y);
        Gl.RenderPipeline.ResizeGBuffer(targetSize);
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
                Input.InputContext,
                _minWindowSize
            );
        if (ImGui.Initialized)
            foreach (var uiRenderer in UiRenderers)
                uiRenderer.OnLoadUi();

        _graphicsReady = true;
    }

    private void OnUpdate(double deltaTime)
    {
        Input.Update(deltaTime);
        Physics.System.Update((float)deltaTime, 1, Physics.JobSystem);
        foreach (var behaviour in Behaviours.Where(behaviour => behaviour.IsActive()))
            behaviour.OnUpdate(deltaTime);
    }

    private void OnRender(double deltaTime)
    {
        var activeCameras = Cameras.Where(camera => camera.IsActive()).ToList();
        var camera3D = activeCameras.Count > 0 && activeCameras.OfType<Camera3D>().Any() ?
            activeCameras.OfType<Camera3D>().First(c => c.IsActive()) :
            null;
        CurrentCamera = camera3D;

        camera3D?.UpdateMatrices(AspectRatio);
        
        Gl.RenderPipeline.Render(deltaTime);

        if (!ImGui.Initialized || ImGui.Controller == null) return;
        ImGui.Controller.Update((float)deltaTime);
        ImGui.Render((float)deltaTime);
        foreach (var renderer in UiRenderers.Where(renderer => renderer.IsActive()))
            renderer.Render();
        ImGui.Controller.Render();
    }
}
