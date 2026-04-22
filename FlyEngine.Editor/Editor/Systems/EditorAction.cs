namespace FlyEngine.Editor.Systems;

public static class EditorAction
{
    public static event Action? OnSceneChanged;
    public static void SceneChanged() => OnSceneChanged?.Invoke();
    public static event Action? OnSceneModified;
    public static void MarkDirty() => OnSceneModified?.Invoke();
}
