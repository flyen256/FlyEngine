using FlyEngine.Core.Engine.Renderer;
using FlyEngine.Core.Engine.SceneManagement;
using Scene = FlyEngine.Core.Engine.SceneManagement.Scene;

namespace FlyEngine.Core.Engine;

public static class Application
{
    public static Scene? Scene => SceneManager.CurrentScene;

    private static bool _isRunning;
    public static bool IsRunning => _isRunning && Window is { IsRunning: true, IsLoaded: true };

    private static BaseWindow? _window;

    public static BaseWindow? Window
    {
        get => _window;
        set
        {
            if (_window == value) return;
            if (_window != null && IsRunning)
                throw new InvalidOperationException("Window is already running");
            if (_window != null)
            {
                _window.OnUpdateEvent -= OnUpdate;
            }
            _window = value;
            if (_window != null)
            {
                _window.OnUpdateEvent += OnUpdate;
            }
        }
    }
    public static OpenGl? OpenGl => Window?.OpenGl;

    private static Scene? _lastLoadedScene;

    private static void OnUpdate(double deltaTime)
    {
        if (!IsRunning) return;
        if (_lastLoadedScene != Scene && Scene != null && !SceneManager.IsLoading)
        {
            _lastLoadedScene = Scene;
            _lastLoadedScene.OnLoad();
        }
        Input.Update(deltaTime);
        Physics.System.Update((float)deltaTime, 1, Physics.JobSystem);
        if (Scene == null) return;
        foreach (var behaviour in Scene.Behaviours.Where(behaviour => behaviour.IsActive()))
            behaviour.OnUpdate(deltaTime);
    }

    private static void CleanUp()
    {
        _lastLoadedScene = null;
    }

    public static void OpenWindow()
    {
        Window?.Run();
    }

    public static void Run()
    {
        if (Window == null) return;
        _isRunning = true;
        if (!Window.IsRunning)
            OpenWindow();
    }

    public static void Stop()
    {
        _isRunning = false;
        CleanUp();
        if (Window is { IsEditor: false })
            CloseWindow();
    }

    public static void CloseWindow()
    {
        Window?.Close();
    }

    public static void Quit()
    {
        Stop();
        CloseWindow();
    }
}