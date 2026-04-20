using System.Reflection;
using FlyEngine.Core.Engine;
using FlyEngine.Core.Engine.Components.Common;
using FlyEngine.Editor.Systems;
using FlyEngine.Editor.Systems.Console;
using FlyEngine.Editor.Systems.Gui;
using FlyEngine.Editor.Tasks;
using FlyEngine.Editor.Window;
using FlyEngine.Network;
using ImGuiNET;
using LiteNetLib;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;

namespace FlyEngine.Editor;

internal abstract class EditorClass;

public static class Editor
{
    private const string ScriptsAssemblyName = "ScriptsAssembly";
    
    private static readonly ILogger Logger = new Logger<EditorClass>(LoggerFactory.Create(b => b.AddConsole()));
    
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
    public static event Action<string?>? OnCurrentProjectPathChanged; 

    private static string? _assetsPath;
    public static string? AssetsPath => GetAssetsPath();
    
    private static string? _tempPath;
    public static string? TempPath => GetTempPath();
    
    public static EditorScriptLoader ScriptLoader { get; private set; } = new();
    
    private static FileSystemWatcher? _assetsWatcher;

    private static string? GetAssetsPath()
    {
        if (string.IsNullOrEmpty(CurrentProjectPath)) return null;
        if (!string.IsNullOrEmpty(_assetsPath) && _assetsPath.StartsWith(CurrentProjectPath) && DirectoryExists(_assetsPath)) return _assetsPath;
        _assetsPath = CurrentProjectPath + "\\Assets";
        if (!DirectoryExists(_assetsPath))
            Directory.CreateDirectory(_assetsPath);
        if (_assetsWatcher == null)
        {
            _assetsWatcher ??= new FileSystemWatcher(_assetsPath, "*.cs")
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };
            _assetsWatcher.Changed += OnAssetsChanged;
        }
        else
            _assetsWatcher.Path = _assetsPath;
        
        return _assetsPath;
    }

    private static string? GetTempPath()
    {
        if (string.IsNullOrEmpty(CurrentProjectPath)) return null;
        if (!string.IsNullOrEmpty(_tempPath) && _tempPath.StartsWith(CurrentProjectPath) && DirectoryExists(_tempPath)) return _tempPath;
        _tempPath = CurrentProjectPath + "\\Temp";
        if (!DirectoryExists(_tempPath))
            Directory.CreateDirectory(_tempPath);
        return _tempPath;
    }

    private static string? GetDevelopmentProjectPath()
    {
        if (_currentProjectPath != null && DirectoryExists(_currentProjectPath)) return _currentProjectPath;
        var currentDirectory = Directory.GetCurrentDirectory().Split("\\");
        var targetCount = currentDirectory.Length - 4;
        var newCurrentDirectorySplit = string.Join("\\", currentDirectory[..targetCount]).Split("\\").ToList();
        newCurrentDirectorySplit.Add("FlyEngine.Game");
        _currentProjectPath = string.Join("\\", newCurrentDirectorySplit);
        if (!DirectoryExists(_currentProjectPath)) return null;
        OnCurrentProjectPathChanged?.Invoke(_currentProjectPath);
        return _currentProjectPath;
    }

    public static bool DirectoryExists(string path) => new DirectoryInfo(path).Exists;
    public static bool FileExists(string path) => new FileInfo(path).Exists;

    private static EditorWindow? _window;

    private static readonly List<EditorSystem> Systems = [new EditorGui(), new EditorConsole()];

    public static readonly EditorTaskQueue TaskQueue = new();
    
    public static bool IsRunningTask => TaskQueue.IsProcessing;
    public static bool CompileError { get; private set; }

    private static bool _scriptsDirty;
    
    public static void Start(EditorWindow window)
    {
        _window = window;
        Application.Window = _window;
        Application.Window.OnLoadEvent += OnLoad;
        Application.Window.OnUpdateEvent += OnUpdate;
        Application.Window.OnRenderEvent += OnRender;
        Application.Window.Handle.FocusChanged += OnFocusChanged;
        Application.OpenWindow();
    }

    private static void OnLoad()
    {
        TaskQueue.Enqueue(CompileScriptsAsync, "Compiling scripts");
    }

    private static void OnAssetsChanged(object? sender, FileSystemEventArgs eventArgs)
    {
        if (_assetsPath == null || _assetsPath != _assetsWatcher?.Path) return;
        _scriptsDirty = true;
    }

    private static void OnFocusChanged(bool value)
    {
        if (value && _scriptsDirty)
            TaskQueue.Enqueue(CompileScriptsAsync, "Compiling scripts");
    }

    public static async Task CompileScriptsAsync()
    {
        if (AssetsPath == null) return;
        if (EditorConsole.Instance != null)
            EditorConsole.Instance.Messages.Clear();
        try
        {
            var compilationResult = await Task.Run(() => 
            {
                var filePaths = Directory.EnumerateFiles(AssetsPath, "*.cs", SearchOption.AllDirectories).ToList();
                if (filePaths.Count == 0) return null;

                var syntaxTrees = filePaths.Select(f => 
                {
                    var code = File.ReadAllText(f);
                    return CSharpSyntaxTree.ParseText(code, path: f);
                }).ToList();

                var compilation = CSharpCompilation.Create(ScriptsAssemblyName)
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
                
                ScriptLoader.Unload();
                ScriptLoader = new EditorScriptLoader(); 
                ScriptLoader.LoadFromStream(compilationResult.Stream);
                var assembly = ScriptLoader.LoadFromAssemblyName(new AssemblyName(ScriptsAssemblyName));
                foreach (var type in assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Behaviour))))
                {
                    Console.WriteLine($"Found behaviour: {type}");
                }
                
                await compilationResult.Stream.DisposeAsync();
                Logger.LogInformation("Scripts compiled successfully!");
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
        foreach (var system in Systems)
            system.OnUpdate(deltaTime);
    }

    private static void OnRender(double deltaTime)
    {
        foreach (var system in Systems)
            system.OnRender(deltaTime);
    }
}