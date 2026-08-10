using FlyEngine.Core.Assets;
using FlyEngine.Core.Debugging;
using FlyEngine.Core.Serialization;
using MemoryPack;

namespace FlyEngine.Core.SceneManagement;

public static class SceneManager
{
    public static Scene? CurrentScene { get; private set; }
    public static bool IsLoading { get; private set; }
    public static float LoadingProgress { get; private set; }

    public static event Action<Scene?>? OnLoaded;
    public static event Action<float>? OnLoadProgress;

    public static void LoadScene(Scene scene)
    {
        CurrentScene?.UnloadScene();
        CurrentScene = scene;
        OnLoaded?.Invoke(CurrentScene);
    }

    public static async Task LoadScene(string path)
    {
        CurrentScene?.UnloadScene();
        IsLoading = true;
        var name = Path.GetFileName(path).Replace(".scene", "");
        Scene? scene;
        await using (var snapshotFile = File.Open(path, FileMode.Open))
        {
            var stream = new ProgressStream(snapshotFile, d =>
            {
                var progress = (float)d;
                LoadingProgress = progress;
                OnLoadProgress?.Invoke(progress);
            });
            scene = await MemoryPackSerializer.DeserializeAsync<Scene>(stream);
        }
        if (scene == null)
        {
            IsLoading = false;
            Debug.LogError($"Failed to load scene: {path}");
            return;
        }
        scene.Path = path;
        scene.Name = name;
        CurrentScene = scene;
        IsLoading = false;
        OnLoaded?.Invoke(CurrentScene);
    }

    public static void UnloadScene()
    {
        CurrentScene?.UnloadScene();
        CurrentScene = null;
    }
}