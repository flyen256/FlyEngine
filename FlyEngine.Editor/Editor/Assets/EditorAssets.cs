using FlyEngine.Core.Assets;
using FlyEngine.Core.SceneManagement;
using Microsoft.Extensions.Logging;

namespace FlyEngine.Editor.Assets;

public class EditorAssets
{
    private readonly ILogger _logger = new Logger<EditorAssets>(LoggerFactory.Create(b => b.AddConsole()));
    
    public void OnAssetsChanged(object? sender, FileSystemEventArgs eventArgs)
    {
        var fileInfo = new FileInfo(eventArgs.FullPath);
        ValidateOnScript(fileInfo);
        ValidateOnModel(fileInfo);
        if (SceneManager.CurrentScene != null &&
            SceneManager.CurrentScene.Path != null &&
            !Editor.DirectoryExists(SceneManager.CurrentScene.Path))
            SceneManager.UnloadScene();
    }
    
    private void ValidateOnScript(FileInfo fileInfo)
    {
        if (!fileInfo.Extension.EndsWith(".cs")) return;
        if (Editor.Window is { IsFocused: true })
            Editor.TaskQueue.Enqueue(Editor.Scripts.CompileScriptsAsync, "Compiling scripts");
        else
            Editor.Scripts.IsDirty = true;
    }

    private void ValidateOnModel(FileInfo fileInfo)
    {
        if (!Editor.AssimpExtensions.Contains(fileInfo.Extension)) return;
        
    }
    
    public static async Task LoadAssetsAsync()
    {
        if (Editor.Window?.OpenGl == null) return;
        await Task.Run(() => Editor.Dispatch(() => AssetsManager.LoadAssets(Editor.Window.OpenGl.Gl)));
    }

    public async Task LoadModelsAsync()
    {
        if (Editor.AssetsPath == null || Editor.Window?.OpenGl == null) return;
        try
        {
            var startDate = DateTime.UtcNow;
            var loadResult = await LoadModelsDataAsync();
            var loadTime = DateTime.UtcNow - startDate;
            _logger.LogInformation("Loaded {Item1} models," +
                                    " {Item2} meshes in {TotalSeconds} seconds",
                loadResult.Item1,
                loadResult.Item2,
                loadTime.TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError("Load models failed: {Exception}", ex);
        }
    }

    private async Task<(int, int)> LoadModelsDataAsync()
    {
        if (Editor.AssetsPath == null || Editor.Window?.OpenGl == null) return (0, 0);
        try
        {
            var loadResult = await Task.Run(() =>
            {
                if (Editor.AssimpExtensions.Length == 0) return null;
                var filePaths = new List<string>();
                foreach (var extension in Editor.AssimpExtensions)
                {
                    filePaths.AddRange(
                        Directory
                            .EnumerateFiles(Editor.AssetsPath, $"*.{extension}", SearchOption.AllDirectories));
                }
                if (filePaths.Count == 0) return null;
                var modelsCount = 0;
                var meshesCount = 0;
                for (var i = 0; i < filePaths.Count; i++)
                {
                    var filePath = filePaths[i];
                    var fileInfo = new FileInfo(filePath);
                    var name = fileInfo.Exists ? fileInfo.Name : string.Empty;
                    var model = ModelManager.LoadModel(Editor.Window.OpenGl, filePath, name);
                    meshesCount += model.Meshes.Count;
                    modelsCount++;
                }
                return new { Models = modelsCount, MeshesCount = meshesCount };
            });
            if (loadResult != null) return (loadResult.Models, loadResult.MeshesCount);
            _logger.LogInformation($"No models found to load");
            return (0, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError("Load models failed: {Exception}", ex);
            return (0, 0);
        }
    }
}