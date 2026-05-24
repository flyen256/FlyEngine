using System.Numerics;

namespace FlyEngine.Editor.Systems;

public static class EditorAction
{
    public static bool IsDirty { get; set; }
    public static event Action? OnSceneChanged;
    public static void SceneChanged() => OnSceneChanged?.Invoke();
    public static event Action? OnSceneModified;
    public static void WindowResize(Vector2 newSize) => OnWindowResize?.Invoke(newSize);
    public static event Action<Vector2>? OnWindowResize;

    public static void MarkDirty()
    {
        IsDirty = true;
        OnSceneModified?.Invoke();
    }
}
