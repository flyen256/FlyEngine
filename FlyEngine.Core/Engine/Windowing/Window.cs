using System.Numerics;
using System.Runtime.InteropServices;
using FlyEngine.Core.Components;
using FlyEngine.Core.Debugging;
using FlyEngine.Core.Gui;
using FlyEngine.Core.Math;
using FlyEngine.Core.Renderer;
using FlyEngine.Core.SceneManagement;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace FlyEngine.Core.Windowing;

public class Window
{
    public bool IsRunning { get; private set; }
    public bool IsLoaded { get; private set; }
    public bool IsFocused { get; private set; } = true;
    
    public float AspectRatio { get; private set; }
    
    public readonly IWindow Handle;

    protected readonly ApplicationWindowOptions WindowOptions;

    public event Action? OnLoadEvent;
    public event Action<double>? OnUpdateEvent;
    public event Action<double>? OnRenderEvent;
    public event Action<bool>? OnFocusChanged;
    public event Action? OnClosingEvent;
    
    public OpenGl? OpenGl { get; protected set; }
    
    public Matrix4x4 EditorCameraViewMatrix { get; protected set; }
    
    protected Matrix4x4 _editorCameraProjectionMatrix;
    public Matrix4x4 EditorCameraProjectionMatrix
    {
        get => _editorCameraProjectionMatrix;
        protected set => _editorCameraProjectionMatrix = value;
    }

    public Vector3 EditorCameraPosition { get; set; } = Vector3.Zero;
    public Quaternion EditorCameraRotation { get; set; } = Quaternion.Identity;
    
    public Vector2D<int> EditorViewport { get; set; }
    public bool IsEditorSceneOpened { get; set; }

    private static Scene? Scene => SceneManager.CurrentScene;
    private static Scene? _lastLoadedScene;
    private bool _graphicsReady;

    protected void UpdateMatrices()
    {
        var fov = MathHelper.DegreesToRadians(70f);
        
        var aspect = (float)EditorViewport.X / EditorViewport.Y;
        EditorCameraProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
            fov, 
            Application.IsEditor ? aspect : AspectRatio, 
            0.01f, 
            5000f);
        _editorCameraProjectionMatrix.M22 *= -1;
        var cameraWorldMatrix = Matrix4x4.CreateFromQuaternion(EditorCameraRotation)
                                * Matrix4x4.CreateTranslation(EditorCameraPosition);

        Matrix4x4.Invert(cameraWorldMatrix, out var view);
        EditorCameraViewMatrix = view;
    }

    public Window(ApplicationWindowOptions windowOptions)
    {
        WindowOptions = windowOptions;
        Handle = Silk.NET.Windowing.Window.Create(WindowOptions.AsWindowOptions());
        Handle.Load += OnLoad;
        Handle.Update += OnUpdate;
        Handle.Render += OnRender;
        Handle.Resize += OnResize;
        Handle.FramebufferResize += OnFramebufferResize;
        Handle.Closing += OnClosing;
        Handle.FocusChanged += FocusChanged;
    }

    public void Run()
    {
        IsRunning = true;
        Handle.Run();
    }

    public void Close()
    {
        _lastLoadedScene = null;
        Handle.Close();
        Handle.Dispose();
        IsRunning = false;
    }

    private void OnClosing()
    {
        IsLoaded = false;
        OnClosingEvent?.Invoke();
    }

    private void OnLoad()
    {
        OpenGl = new OpenGl(Handle);
        OpenGl.Initialize();
        OpenGl.ProcessShaders();

        Input.Input.Initialize(Handle);

        if (Input.Input.InputContext != null)
            ImGui.Initialize(
                OpenGl.Gl,
                Handle,
                Input.Input.InputContext,
                WindowOptions.MinSize
            );

        _graphicsReady = true;
        IsLoaded = true;
        OnLoadEvent?.Invoke();
        AspectRatio = (float)Handle.Size.X / Handle.Size.Y;
    }

    private void OnUpdate(double deltaTime)
    {
        TimeManager.DeltaTime = (float)deltaTime * TimeManager.TimeScale;
        Input.Input.Update(deltaTime);
        OnUpdateEvent?.Invoke(deltaTime);
        if (!Application.IsRunning) return;
        TimeManager.Timer += (float)deltaTime;
        if (_lastLoadedScene != Scene && Scene != null && !SceneManager.IsLoading)
        {
            _lastLoadedScene = Scene;
            _lastLoadedScene.OnLoad();
        }
        Physics.Physics.System.Update((float)deltaTime * TimeManager.TimeScale, 1, Physics.Physics.JobSystem);
        if (Scene == null) return;
        foreach (var behaviour in Scene.Behaviours.Where(behaviour => behaviour.IsActive()))
            behaviour.OnUpdate((float)deltaTime * TimeManager.TimeScale);
        Scene.EcsWorld.Update((float)deltaTime);
    }

    private void OnRender(double deltaTime)
    {
        if (Profiler.Enabled)
            Profiler.Stopwatch.Restart();
        var activeCameras = Scene?.Cameras.Where(camera => camera.IsActive()).ToList();
        Camera? camera = null;
        if (activeCameras != null)
        {
            camera = activeCameras.Count > 0 ?
                activeCameras.First(c => c.IsActive()) :
                null;
            Camera.CurrentCamera = camera;
        }
        camera?.UpdateMatrices(AspectRatio);
        if (Application.IsEditor)
            UpdateMatrices();
        
        if (OpenGl == null) return;

        OpenGl.RenderPipeline.Render((float)deltaTime * TimeManager.TimeScale, IsEditorSceneOpened);
        if (Profiler.Enabled || Profiler.Stopwatch.IsRunning)
        {
            Profiler.Stopwatch.Stop();
            Profiler.UpdateMetrics((float)deltaTime);
        }

        if (!ImGui.Initialized || ImGui.Controller == null) return;
        ImGui.Controller.Update((float)deltaTime);
        if (Scene != null)
        {
            var renderers = CollectionsMarshal.AsSpan(Scene.GuiWindows.ToList());
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (!renderer.IsActive()) continue;
                renderer.Render();
            }
        }
        Scene?.Update(deltaTime * TimeManager.TimeScale);
        OnRenderEvent?.Invoke(deltaTime * TimeManager.TimeScale);
        ImGui.Controller.Render();
    }

    private void OnResize(Vector2D<int> newSize)
    {
        var targetSize = newSize;
        if (newSize.X < WindowOptions.MinSize.X)
            targetSize.X = WindowOptions.MinSize.X;
        if (newSize.Y < WindowOptions.MinSize.Y)
            targetSize.Y = WindowOptions.MinSize.Y;
        Handle.Size = targetSize;
        if (Application.IsEditor)
            AspectRatio = (float)EditorViewport.X /  EditorViewport.Y;
        else
            AspectRatio = (float)targetSize.X / targetSize.Y;
    }

    public void Resize(Vector2D<int> newSize)
    {
        OnResize(newSize);
    }

    private void OnFramebufferResize(Vector2D<int> newSize)
    {
        if (!_graphicsReady || OpenGl == null) return;
        OpenGl.Gl.Viewport(0, 0, (uint)newSize.X, (uint)newSize.Y);
        OpenGl.RenderPipeline.ResizeGBuffer(newSize);
        if (Application.IsEditor)
            OpenGl.RenderPipeline.CreateFinalFramebuffer(newSize);
    }

    private void FocusChanged(bool value)
    {
        IsFocused = value;
        OnFocusChanged?.Invoke(value);
    }
}
