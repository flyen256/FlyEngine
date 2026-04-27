using FlyEngine.Core.SceneManagement;
using MemoryPack;
using Microsoft.Extensions.Logging;

namespace FlyEngine.Editor.SceneManagement;

internal abstract class SceneSnapshotClass;

public static class SceneSnapshot
{
    private static readonly ILogger Logger = new Logger<SceneSnapshotClass>(LoggerFactory.Create(b => b.AddConsole()));
    
    private static string? SnapshotPath => Editor.TempPath != null ? Editor.TempPath + "\\SceneSnapshot.tmp" : null;

    static SceneSnapshot()
    {
        DeleteSnapshot();
    }
    
    public static async Task<bool> CreateSnapshotAsync(Scene scene)
    {
        if (SnapshotPath == null) return false;
        try
        {
            var snapshotFile = File.Open(SnapshotPath, FileMode.Create);
            await MemoryPackSerializer.SerializeAsync(snapshotFile, scene);
            snapshotFile.Close();
            Logger.LogInformation($"Created snapshot: {snapshotFile}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to create snapshot for {scene.Name}: {ex.Message}");
            return false;
        }
    }
    
    public static async Task RestoreSnapshotAsync()
    {
        if (SnapshotPath == null) return;
        try
        {
            SceneManager.UnloadScene();
            Scene? scene;
            await using (var snapshotFile = File.Open(SnapshotPath, FileMode.Open))
                scene = await MemoryPackSerializer.DeserializeAsync<Scene>(snapshotFile);
            if (scene == null)
                throw new Exception("Failed to deserialize scene");
            Editor.Dispatch(() => 
            {
                SceneManager.LoadScene(scene);
                Logger.LogInformation("Restored scene from snapshot in Main Thread");
            });
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to restore snapshot: {ex.Message}");
        }
    }

    public static void DeleteSnapshot()
    {
        if (SnapshotPath == null) return;
        try
        {
            if (Editor.FileExists(SnapshotPath))
                File.Delete(SnapshotPath);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to delete snapshot: {ex.Message}");
        }
    }
}