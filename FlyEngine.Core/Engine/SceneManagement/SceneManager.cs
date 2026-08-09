using FlyEngine.Core.Assets;
using FlyEngine.Core.Serialization;
using MemoryPack;

namespace FlyEngine.Core.SceneManagement;

public static class SceneManager
{
    public static Scene? CurrentScene { get; private set; }
    public static bool IsLoading { get; private set; }
    public static float LoadingProgress { get; private set; }
    public delegate void OnLoadProgressDelegate(float progress);
    public static event OnLoadProgressDelegate? OnLoadProgress;

    public static void LoadScene(Scene scene)
    {
        CurrentScene = scene;
    }

    public static async Task LoadScene(string path)
    {
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
            throw new Exception("Deserialize failed");
        scene.Path = path;
        scene.Name = name;
        CurrentScene = scene;
        IsLoading = false;
    }

    public static void UnloadScene()
    {
        if (CurrentScene != null)
            AssetsManager.UnloadAsset(CurrentScene);
        CurrentScene = null;
    }
}