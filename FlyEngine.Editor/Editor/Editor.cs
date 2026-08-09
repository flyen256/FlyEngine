using System.Collections.Concurrent;
using System.Numerics;
using FlyEngine.Core;
using FlyEngine.Core.Assets;
using FlyEngine.Core.Windowing;
using FlyEngine.Editor.Assets;
using FlyEngine.Editor.Scripting;
using FlyEngine.Editor.Systems;
using FlyEngine.Editor.TaskQueue;
using Microsoft.Extensions.Logging;
using Silk.NET.Assimp;
using Silk.NET.Maths;

namespace FlyEngine.Editor;

internal abstract class EditorClass;

public static class Editor
{
    private static readonly ILogger Logger = new Logger<EditorClass>(LoggerFactory.Create(b => b.AddConsole()));

    public static string[] AssimpExtensions
    {
        get
        {
            var extensionsAssimpString = new AssimpString();
            ModelManager.Assimp.GetExtensionList(ref extensionsAssimpString);
            return extensionsAssimpString.AsString.Split(";").Select(ex => ex.Remove(0, 2)).ToArray();
        }
    }
    public static string? CurrentProjectPath
    {
        get => GetDevelopmentProjectPath();
        set
        {
            if (_currentProjectPath != null && _currentProjectPath == value) return;
            _currentProjectPath = value;
            OnCurrentProjectPathChanged?.Invoke(_currentProjectPath);
        }
    }
    public static string? AssetsPath => GetAssetsPath();
    public static string? TempPath => GetTempPath();
    
    public static Window? Window { get; private set; }
    public static EditorAssets Assets { get; } = new();
    public static EditorScripts Scripts { get; } = new();
    public static readonly EditorTaskQueue TaskQueue = new();
    
    public static bool IsRunningTask => TaskQueue.IsProcessing;

    public static bool IsSceneOpened
    {
        set
        {
            if (Window != null)
                Window.IsEditorSceneOpened = value;
        }
    }

    public static event Action<string?>? OnCurrentProjectPathChanged;
    
    private static string? _currentProjectPath;
    private static string? _assetsPath;
    private static string? _tempPath;
    
    private static FileSystemWatcher? _assetsWatcher;
    private static readonly List<EditorSystem> Systems = [
        new EditorGui(),
        new EditorInput(),
        new EditorCameraMovement()];
    
    private static readonly ConcurrentQueue<Action> MainThreadQueue = new();
    
    public static void Start(Window window)
    {
        Window = window;
        Window.OnLoadEvent += OnLoad;
        Window.OnUpdateEvent += OnUpdate;
        Window.OnRenderEvent += OnRender;
        Window.OnFocusChanged += OnFocusChanged;
        Window.OnClosingEvent += OnClosing;
        Application.Window = Window;
        Application.OpenWindow();
        EditorAction.OnWindowResize += OnWindowResize;
    }

    public static void OnClosing()
    {
        EditorAction.OnWindowResize -= OnWindowResize;
        Window = null;
    }

    private static void OnWindowResize(Vector2 newSize)
    {
        Window?.Resize(newSize.ToGeneric().As<int>());
    }
    
    public static void Dispatch(Action action)
    {
        MainThreadQueue.Enqueue(action);
    }

    private static void ExecuteDispatchedActions()
    {
        while (MainThreadQueue.TryDequeue(out var action))
        {
            try
            {
                action.Invoke();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error executing dispatched action: {ex.Message}");
            }
        }
    }

    private static void OnLoad()
    {
        TaskQueue.Enqueue(Scripts.CompileScriptsAsync, "Compiling scripts");
        TaskQueue.Enqueue(Assets.LoadModelsAsync, "Loading models");
        TaskQueue.Enqueue(EditorAssets.LoadAssetsAsync, "Loading assets");
    }

    private static void OnFocusChanged(bool value)
    {
        if (value && Scripts.IsDirty)
            TaskQueue.Enqueue(Scripts.CompileScriptsAsync, "Compiling scripts");
    }

    private static void OnUpdate(double deltaTime)
    {
        ExecuteDispatchedActions();
        foreach (var system in Systems)
            system.OnUpdate(deltaTime);
    }

    private static void OnRender(double deltaTime)
    {
        foreach (var system in Systems)
            system.OnRender(deltaTime);
    }
    
    private static string? GetAssetsPath()
    {
        if (string.IsNullOrEmpty(CurrentProjectPath)) return null;
        if (!string.IsNullOrEmpty(_assetsPath) && _assetsPath.StartsWith(CurrentProjectPath) && DirectoryExists(_assetsPath)) return _assetsPath;
        _assetsPath = Path.Combine(CurrentProjectPath, "Assets");
        if (!DirectoryExists(_assetsPath))
            Directory.CreateDirectory(_assetsPath);
        if (_assetsWatcher == null)
        {
            _assetsWatcher = new FileSystemWatcher(_assetsPath)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
            };
            _assetsWatcher.Filters.Add("*.cs");
            foreach (var assimpExtension in AssimpExtensions)
                _assetsWatcher.Filters.Add($"*.{assimpExtension}");
            _assetsWatcher.Changed += Assets.OnAssetsChanged;
            _assetsWatcher.Created += Assets.OnAssetsChanged;
            _assetsWatcher.Deleted += Assets.OnAssetsChanged;
            // _assetsWatcher.Renamed += OnAssetsRenamed;
        }
        else
            _assetsWatcher.Path = _assetsPath;
        
        return _assetsPath;
    }

    private static string? GetTempPath()
    {
        if (string.IsNullOrEmpty(CurrentProjectPath)) return null;
        if (!string.IsNullOrEmpty(_tempPath) && _tempPath.StartsWith(CurrentProjectPath) && DirectoryExists(_tempPath)) return _tempPath;
        _tempPath = Path.Combine(CurrentProjectPath, "Temp");
        if (!DirectoryExists(_tempPath))
            Directory.CreateDirectory(_tempPath);
        return _tempPath;
    }

    private static string? GetDevelopmentProjectPath()
    {
        if (_currentProjectPath != null && DirectoryExists(_currentProjectPath)) return _currentProjectPath;
        var currentDirectory =
            new DirectoryInfo(AppContext.BaseDirectory).Parent?.Parent?.Parent?.Parent;
        if (currentDirectory == null) return null;
        var targetDirectory = new DirectoryInfo(Path.Combine(currentDirectory.FullName, "FlyEngine.Game"));
        _currentProjectPath = targetDirectory.FullName;
        if (!DirectoryExists(_currentProjectPath)) return null;
        OnCurrentProjectPathChanged?.Invoke(_currentProjectPath);
        return _currentProjectPath;
    }
    
    public static bool DirectoryExists(string path) => new DirectoryInfo(path).Exists;
    public static bool FileExists(string path) => new FileInfo(path).Exists;

    public static void SetCameraRotation(Quaternion target)
    {
        if (Window == null) return;
        Window.EditorCameraRotation = target;
    }
    
    public static void SetCameraPosition(Vector3 target)
    {
        if (Window == null) return;
        Window.EditorCameraPosition = target;
    }

    public static Quaternion GetCameraRotation() =>
        Window?.EditorCameraRotation ?? Quaternion.Identity;
    public static Vector3 GetCameraPosition() =>
        Window?.EditorCameraPosition ?? Vector3.Zero;
}
