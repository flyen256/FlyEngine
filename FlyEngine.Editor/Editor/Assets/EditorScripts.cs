using FlyEngine.Core;
using FlyEngine.Editor.Systems.Console;
using FlyEngine.Network;
using LiteNetLib;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;

namespace FlyEngine.Editor.Assets;

public class EditorScripts
{
    private readonly ILogger _logger = new Logger<EditorScripts>(LoggerFactory.Create(b => b.AddConsole()));
    
    public event Action? OnCompileScripts;
    
    public bool IsDirty { get; set; }
    public bool CompileError { get; private set; }
    
    public async Task CompileScriptsAsync()
    {
        if (Editor.AssetsPath == null || Editor.Window == null) return;
        EditorConsole.Messages.Clear();
        try
        {
            IsDirty = false;
            var compilationResult = await Task.Run(() => 
            {
                var filePaths =
                    Directory.EnumerateFiles(
                        Editor.AssetsPath, 
                        "*.cs",
                        SearchOption.AllDirectories).ToList();
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
                _logger.LogWarning("No C# files found");
                return;
            }

            if (compilationResult.Success)
            {
                CompileError = false;
                compilationResult.Stream.Seek(0, SeekOrigin.Begin);
                
                Editor.Window.EditorScriptLoader.Unload();
                Editor.Window.EditorScriptLoader = new EditorScriptLoader();
                Editor.Window.EditorScriptLoader.LoadFromStream(compilationResult.Stream);
                
                await compilationResult.Stream.DisposeAsync();
                _logger.LogInformation("Scripts compiled successfully!");
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
                    _logger.LogError($"Errors in {fileErrors.Key}:");
                    EditorConsole.Messages.Add(new EditorConsoleMessage
                    {
                        Level = LogLevel.Error,
                        Message = $"Errors in {fileErrors.Key}:"
                    });
                    foreach (var diagnostic in fileErrors)
                    {
                        var lineSpan = diagnostic.Location.GetLineSpan();
                        _logger.LogError($"  Line {lineSpan.StartLinePosition.Line + 1}: {diagnostic.GetMessage()}");
                        EditorConsole.Messages.Add(new EditorConsoleMessage
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
            _logger.LogError($"Compilation failed: {ex}");
        }
    }
    
    private IEnumerable<MetadataReference> GetMetadataReferences()
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
                _logger.LogDebug($"Added core library: {lib}");
            }
            else
                _logger.LogWarning($"Core library not found: {path}");
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
                _logger.LogWarning($"Failed to add reference {assembly.Location}: {ex.Message}");
            }
        }

        try
        {
            references.Add(MetadataReference.CreateFromFile(typeof(NetworkManager).Assembly.Location));
            references.Add(MetadataReference.CreateFromFile(typeof(NetPeer).Assembly.Location));
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to add network references: {ex.Message}");
        }

        return references.Distinct();
    }
}