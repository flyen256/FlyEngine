using System.Runtime.Loader;
using FlyEngine.Core.SceneManagement;
using FlyEngine.Core.Windowing;
using Scene = FlyEngine.Core.SceneManagement.Scene;

namespace FlyEngine.Core;

public static class Application
{
    public static bool IsEditor { get; private set; }
    public static AssemblyLoadContext ScriptsLoader { get; set; } = new(Scripting.Scripting.ScriptsAssemblyName + "Context", isCollectible: true);
    
    public static Scene? Scene => SceneManager.CurrentScene;
    public static bool IsRunning => _isRunning && Window is { IsRunning: true, IsLoaded: true };

    public static Window? Window
    {
        get => _window;
        set
        {
            if (_window == value) return;
            if (_window != null && IsRunning)
                throw new InvalidOperationException("Window is already running");
            _window = value;
        }
    }

    public static event Action<bool>? OnApplicationState;
    
    public static double UpdatesPerSecond
    {
        get => Window?.Handle.UpdatesPerSecond ?? 0.0f;
        set
        {
            if (Window == null) return;
            Window.Handle.UpdatesPerSecond = value;
        }
    }
    
    public static double FramesPerSecond
    {
        get => Window?.Handle.FramesPerSecond ?? 0.0f;
        set
        {
            if (Window == null) return;
            Window.Handle.FramesPerSecond = value;
        }
    }
    
    public static bool VSync
    {
        get => Window?.Handle.VSync ?? false;
        set
        {
            if (Window == null) return;
            Window.Handle.VSync = value;
        }
    }

    private static bool _initialized;
    private static bool _isRunning;
    private static Window? _window;

    public static void Initialize(bool isEditor = false)
    {
        IsEditor = isEditor;
        _initialized = true;
    }
    
    public static void OpenWindow()
    {
        if (!_initialized)
            throw new InvalidOperationException("Application is not initialized");
        Window?.Run();
    }

    public static void Run()
    {
        if (!_initialized)
            throw new InvalidOperationException("Application is not initialized");
        if (Window == null) return;
        Physics.Physics.Init();
        _isRunning = true;
        OnApplicationState?.Invoke(_isRunning);
        TimeManager.Timer = 0f;
        if (!Window.IsRunning)
            OpenWindow();
    }

    public static void Stop()
    {
        if (!_initialized)
            throw new InvalidOperationException("Application is not initialized");
        _isRunning = false;
        OnApplicationState?.Invoke(_isRunning);
        TimeManager.Timer = 0f;
        Physics.Physics.Shutdown();
        if (!IsEditor)
            CloseWindow();
        Input.Input.CursorVisible = true;
    }

    public static void CloseWindow()
    {
        if (!_initialized)
            throw new InvalidOperationException("Application is not initialized");
        Window?.Close();
    }

    public static void Quit()
    {
        if (!_initialized)
            throw new InvalidOperationException("Application is not initialized");
        Stop();
        CloseWindow();
        _initialized = false;
    }
}
