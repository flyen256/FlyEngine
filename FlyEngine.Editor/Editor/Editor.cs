using System.Collections.Concurrent;
using System.Numerics;
using FlyEngine.Core;
using FlyEngine.Core.Assets;
using FlyEngine.Core.SceneManagement;
using FlyEngine.Editor.Systems;
using FlyEngine.Editor.Systems.Console;
using FlyEngine.Editor.Systems.Gui;
using FlyEngine.Editor.Tasks;
using FlyEngine.Editor.Window;
using FlyEngine.Network;
using LiteNetLib;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using Silk.NET.Assimp;
using Silk.NET.Maths;
using File = System.IO.File;
using Mesh = FlyEngine.Core.Assets.Mesh;

namespace FlyEngine.Editor;

internal abstract class EditorClass;

public static class Editor
{
    private static readonly ILogger Logger = new Logger<EditorClass>(LoggerFactory.Create(b => b.AddConsole()));

    private static string[] AssimpExtensions
    {
        get
        {
            var extensionsAssimpString = new AssimpString();
            ModelManager.Assimp.GetExtensionList(ref extensionsAssimpString);
            var extensionsList = extensionsAssimpString.AsString.Split(";");
            return extensionsList.Select(ex => ex.Remove(0, 2)).ToArray();
        }
    }
    
    private static string? _currentProjectPath;
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

    private static string? _assetsPath;
    public static string? AssetsPath => GetAssetsPath();
    
    private static string? _tempPath;
    public static string? TempPath => GetTempPath();
    
    public static EditorScriptLoader? ScriptLoader => _window?.EditorScriptLoader;
    private static FileSystemWatcher? _assetsWatcher;
    private static EditorWindow? _window;

    public static readonly EditorTaskQueue TaskQueue = new();
    private static readonly List<EditorSystem> Systems = [
        new EditorGui(),
        new EditorConsole(),
        new EditorCameraMovement()];
    
    public static bool IsRunningTask => TaskQueue.IsProcessing;
    public static bool CompileError { get; private set; }
    public static bool IsSceneOpened { get; set; }
    
    public static event Action<string?>? OnCurrentProjectPathChanged;
    public static event Action? OnCompileScripts;

    private static bool _scriptsDirty;
    
    private static readonly ConcurrentQueue<Action> MainThreadQueue = new();

    public static Vector2D<int>? ViewportSize
    {
        get => _window?.EditorViewport;
        set
        {
            if (_window != null && value != null)
                _window.EditorViewport = value.Value;
        }
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
            _assetsWatcher.Changed += OnAssetsChanged;
            _assetsWatcher.Created += OnAssetsChanged;
            _assetsWatcher.Deleted += OnAssetsChanged;
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
            new DirectoryInfo(Directory.GetCurrentDirectory()).Parent?.Parent?.Parent?.Parent;
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
        if (_window == null) return;
        _window.EditorCameraRotation = target;
    }
    
    public static void SetCameraPosition(Vector3 target)
    {
        if (_window == null) return;
        _window.EditorCameraPosition = target;
    }

    public static Quaternion GetCameraRotation() =>
        _window?.EditorCameraRotation ?? Quaternion.Identity;
    public static Vector3 GetCameraPosition() =>
        _window?.EditorCameraPosition ?? Vector3.Zero;
    
    public static void Start(EditorWindow window)
    {
        _window = window;
        Application.Window = _window;
        Application.Window.OnLoadEvent += OnLoad;
        Application.Window.OnUpdateEvent += OnUpdate;
        Application.Window.OnRenderEvent += OnRender;
        Application.Window.OnFocusChanged += OnFocusChanged;
        Application.OpenWindow();
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
        TaskQueue.Enqueue(CompileScriptsAsync, "Compiling scripts");
        TaskQueue.Enqueue(LoadModelsAsync, "Loading models");
        TaskQueue.Enqueue(LoadAssetsAsync, "Loading assets");
    }

    private static void OnAssetsChanged(object? sender, FileSystemEventArgs eventArgs)
    {
        var fileInfo = new FileInfo(eventArgs.FullPath);
        ValidateOnScript(fileInfo);
        ValidateOnModel(fileInfo);
        if (SceneManager.CurrentScene != null && SceneManager.CurrentScene.Path != null && !DirectoryExists(SceneManager.CurrentScene.Path))
            SceneManager.UnloadScene();
    }

    private static void ValidateOnScript(FileInfo fileInfo)
    {
        if (!fileInfo.Extension.EndsWith(".cs")) return;
        if (_window is { IsFocused: true })
            TaskQueue.Enqueue(CompileScriptsAsync, "Compiling scripts");
        else
            _scriptsDirty = true;
    }

    private static void ValidateOnModel(FileInfo fileInfo)
    {
        if (!AssimpExtensions.Contains(fileInfo.Extension)) return;
        
    }

    private static void OnFocusChanged(bool value)
    {
        if (value && _scriptsDirty)
            TaskQueue.Enqueue(CompileScriptsAsync, "Compiling scripts");
    }

    public static async Task CompileScriptsAsync()
    {
        if (AssetsPath == null || _window == null) return;
        if (EditorConsole.Instance != null)
            EditorConsole.Instance.Messages.Clear();
        try
        {
            _scriptsDirty = false;
            var compilationResult = await Task.Run(() => 
            {
                var filePaths = Directory.EnumerateFiles(AssetsPath, "*.cs", SearchOption.AllDirectories).ToList();
                if (filePaths.Count == 0) return null;

                var syntaxTrees = filePaths.Select(f => 
                {
                    var code = File.ReadAllText(f);
                    return CSharpSyntaxTree.ParseText(code, path: f);
                }).ToList();

                var compilation = CSharpCompilation.Create(Application.ScriptsAssemblyName)
                    .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                        .WithOptimizationLevel(OptimizationLevel.Debug))
                    .AddReferences(GetMetadataReferences())
                    .AddSyntaxTrees(syntaxTrees);

                var ms = new MemoryStream();
                var result = compilation.Emit(ms);
                
                return new { result.Success, Stream = ms, result.Diagnostics };
            });

            if (compilationResult == null)
            {
                Logger.LogWarning("No C# files found");
                return;
            }

            if (compilationResult.Success)
            {
                CompileError = false;
                compilationResult.Stream.Seek(0, SeekOrigin.Begin);
                
                _window.EditorScriptLoader.Unload();
                _window.EditorScriptLoader = new EditorScriptLoader();
                _window.EditorScriptLoader.LoadFromStream(compilationResult.Stream);
                
                await compilationResult.Stream.DisposeAsync();
                Logger.LogInformation("Scripts compiled successfully!");
                OnCompileScripts?.Invoke();
            }
            else
            {
                CompileError = true;
                var errors = compilationResult.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .GroupBy(d => d.Location.SourceTree?.FilePath ?? "Unknown")
                    .ToList();
    
                foreach (var fileErrors in errors)
                {
                    Logger.LogError($"Errors in {fileErrors.Key}:");
                    if (EditorConsole.Instance != null)
                        EditorConsole.Instance.Messages.Add(new EditorConsoleMessage
                        {
                            Level = LogLevel.Error,
                            Message = $"Errors in {fileErrors.Key}:"
                        });
                    foreach (var diagnostic in fileErrors)
                    {
                        var lineSpan = diagnostic.Location.GetLineSpan();
                        Logger.LogError($"  Line {lineSpan.StartLinePosition.Line + 1}: {diagnostic.GetMessage()}");
                        if (EditorConsole.Instance != null)
                            EditorConsole.Instance.Messages.Add(new EditorConsoleMessage
                            {
                                Level = LogLevel.Error,
                                Message = $"  Line {lineSpan.StartLinePosition.Line + 1}: {diagnostic.GetMessage()}"
                            });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Compilation failed: {ex}");
        }
    }

    public static async Task LoadAssetsAsync()
    {
        if (_window?.OpenGl == null) return;
        await Task.Run(() => Dispatch(() => AssetsManager.LoadAssets(_window.OpenGl.Gl)));
    }

    public static async Task LoadModelsAsync()
    {
        if (AssetsPath == null || _window?.OpenGl == null) return;
        try
        {
            var startDate = DateTime.UtcNow;
            var loadResult = await LoadModelsDataAsync();
            var loadTime = DateTime.UtcNow - startDate;
            Logger.LogInformation($"Loaded {loadResult.Item1} models," +
                                  $" {loadResult.Item2.Count} meshes in {loadTime.TotalSeconds} seconds");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Load models failed: {ex}");
        }
    }

    private static async Task<(int, List<Mesh>)> LoadModelsDataAsync()
    {
        if (AssetsPath == null || _window?.OpenGl == null) return (0, []);
        try
        {
            var loadResult = await Task.Run(() =>
            {
                if (AssimpExtensions.Length == 0) return null;
                var filePaths = new List<string>();
                for (var i = 0; i < AssimpExtensions.Length; i++)
                {
                    var extension = AssimpExtensions[i];
                    filePaths.AddRange(
                        Directory
                            .EnumerateFiles(AssetsPath, $"*.{extension}", SearchOption.AllDirectories));
                }
                if (filePaths.Count == 0) return null;
                var modelsCount = 0;
                var meshes = new List<Mesh>();
                for (var i = 0; i < filePaths.Count; i++)
                {
                    var filePath = filePaths[i];
                    var loadedMeshes = ModelManager.LoadModel(_window.OpenGl, filePath);
                    meshes.AddRange(loadedMeshes);
                    modelsCount++;
                }
                return new { Models = modelsCount, Meshes = meshes };
            });
            if (loadResult != null) return (loadResult.Models, loadResult.Meshes);
            Logger.LogInformation($"No models found to load");
            return (0, []);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Load models failed: {ex}");
            return (0, []);
        }
    }
    
    private static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        var references = new List<MetadataReference>();
        var runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        
        string[] coreLibraries = 
        {
            "System.Private.CoreLib.dll",
            "System.Runtime.dll",
            "System.Threading.Tasks.dll",
            "System.Console.dll",
            "System.Collections.dll",
            "System.Linq.dll",
            "System.Runtime.Extensions.dll",
            "System.Runtime.InteropServices.dll",
            "System.Text.Json.dll",
            "System.ComponentModel.dll",
            "System.ComponentModel.Primitives.dll",
            "System.Numerics.Vectors.dll"
        };

        foreach (var lib in coreLibraries)
        {
            var path = Path.Combine(runtimePath, lib);
            if (File.Exists(path))
            {
                references.Add(MetadataReference.CreateFromFile(path));
                Logger.LogDebug($"Added core library: {lib}");
            }
            else
                Logger.LogWarning($"Core library not found: {path}");
        }

        var netstandardPath = Path.Combine(runtimePath, "netstandard.dll");
        if (File.Exists(netstandardPath))
            references.Add(MetadataReference.CreateFromFile(netstandardPath));

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .DistinctBy(a => a.Location)
            .ToList();

        foreach (var assembly in assemblies)
        {
            try
            {
                if (assembly.Location.StartsWith(runtimePath)) 
                    continue;
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to add reference {assembly.Location}: {ex.Message}");
            }
        }

        try
        {
            references.Add(MetadataReference.CreateFromFile(typeof(NetworkManager).Assembly.Location));
            references.Add(MetadataReference.CreateFromFile(typeof(NetPeer).Assembly.Location));
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Failed to add network references: {ex.Message}");
        }

        return references.Distinct();
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
}