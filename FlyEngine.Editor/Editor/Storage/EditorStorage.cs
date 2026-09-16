namespace FlyEngine.Editor.Storage;

public static class EditorStorage
{
    public static EditorPreferences Preferences { get; private set; }

    static EditorStorage()
    {
        Preferences = EditorPreferences.Load();
    }
}