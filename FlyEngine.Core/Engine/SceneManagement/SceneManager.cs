namespace FlyEngine.Core.Engine.SceneManagement;

public static class SceneManager
{
    public static Scene? CurrentScene { get; private set; }
    public static bool IsLoading { get; private set; }
    public static float LoadingProgress { get; private set; }

    public static void LoadScene(Scene scene)
    {
        IsLoading = true;
        LoadingProgress = 0f;
        CurrentScene = scene;
        CurrentScene.PreLoad();
        LoadingProgress = 1f;
        IsLoading = false;
    }

    public static async Task LoadSceneAsync(byte[] sceneData)
    {
        IsLoading = true;
        LoadingProgress = 0f;
        
    }
    
    public static async Task LoadSceneAsync(Scene scene)
    {
        IsLoading = true;
        LoadingProgress = 0f;
        CurrentScene = scene;
        await Task.Run(CurrentScene.PreLoad);
        LoadingProgress = 1f;
        IsLoading = false;
    }

    public static void UnloadScene()
    {
        CurrentScene?.OnUnload();
    }
}