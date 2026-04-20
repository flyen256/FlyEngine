namespace FlyEngine.Editor.Systems;

public static class EditorAction
{
    public static event Action? OnSceneModified;
    public static void MarkDirty() => OnSceneModified?.Invoke();
}
