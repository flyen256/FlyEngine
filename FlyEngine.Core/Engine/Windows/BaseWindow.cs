using System.Numerics;
using FlyEngine.Core.Math;
using FlyEngine.Core.Renderer;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace FlyEngine.Core;

public abstract class BaseWindow
{
    public bool IsRunning { get; private set; }
    public bool IsLoaded { get; private set; }
    public bool IsFocused { get; private set; } = true;
    public virtual bool IsEditor => false;
    
    public float AspectRatio { get; private set; }
    
    public readonly IWindow Handle;

    protected readonly ApplicationWindowOptions WindowOptions;

    public event Action? OnLoadEvent;
    public event Action<double>? OnUpdateEvent;
    public event Action<double>? OnRenderEvent;
    public event Action<bool>? OnFocusChanged;
    
    public OpenGl? OpenGl { get; protected set; }
    
    public Matrix4x4 EditorCameraViewMatrix { get; private set; }
    
    private Matrix4x4 _editorCameraProjectionMatrix;
    public Matrix4x4 EditorCameraProjectionMatrix
    {
        get => _editorCameraProjectionMatrix;
        private set => _editorCameraProjectionMatrix = value;
    }

    public Vector3 EditorCameraPosition { get; set; } = Vector3.Zero;
    public Quaternion EditorCameraRotation { get; set; } = Quaternion.Identity;
    
    public EditorScriptLoader EditorScriptLoader { get; set; } = new();
    
    public Vector2D<int> EditorViewport { get; set; }

    protected void UpdateMatrices()
    {
        var fov = MathHelper.DegreesToRadians(70f);
        
        EditorCameraProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
            fov, 
            AspectRatio, 
            0.01f, 
            5000f);
        _editorCameraProjectionMatrix.M22 *= -1;
        var cameraWorldMatrix = Matrix4x4.CreateFromQuaternion(EditorCameraRotation)
                                * Matrix4x4.CreateTranslation(EditorCameraPosition);

        Matrix4x4.Invert(cameraWorldMatrix, out var view);
        EditorCameraViewMatrix = view;
    }

    protected BaseWindow(ApplicationWindowOptions windowOptions)
    {
        WindowOptions = windowOptions;
        Handle = Window.Create(WindowOptions.AsWindowOptions());
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
        Handle.Close();
        Handle.Dispose();
        IsRunning = false;
    }
    
    protected virtual void OnClosing()
    {
        IsLoaded = false;
    }

    protected virtual void OnLoad()
    {
        IsLoaded = true;
        OnLoadEvent?.Invoke();
        AspectRatio = (float)Handle.Size.X / Handle.Size.Y;
    }

    protected virtual void OnUpdate(double deltaTime)
    {
        OnUpdateEvent?.Invoke(deltaTime);
    }

    protected virtual void OnRender(double deltaTime)
    {
        OnRenderEvent?.Invoke(deltaTime);
    }
    
    protected virtual void OnResize(Vector2D<int> newSize)
    {
        var targetSize = newSize;
        if (newSize.X < WindowOptions.MinSize.X)
            targetSize.X = WindowOptions.MinSize.X;
        if (newSize.Y < WindowOptions.MinSize.Y)
            targetSize.Y = WindowOptions.MinSize.Y;
        Handle.Size = targetSize;
        AspectRatio = (float)targetSize.X / targetSize.Y;
    }
    
    protected virtual void OnFramebufferResize(Vector2D<int> newSize) { }

    protected virtual void FocusChanged(bool value)
    {
        IsFocused = value;
        OnFocusChanged?.Invoke(value);
    }
}