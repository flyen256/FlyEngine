using FlyEngine.Core.Debugging;
using FlyEngine.Core.Input;
using FlyEngine.Core.SceneManagement;
using Silk.NET.Input;

namespace FlyEngine.Editor.Systems;

public class EditorInput : EditorSystem
{
    private readonly ShortcutManager _shortcutManager = new();

    public EditorInput()
    {
        _shortcutManager.Register(Key.S, KeyModifiers.Ctrl, Save, "Save");
        
        Input.OnKeyDownEvent += OnKeyDown;
    }
    
    private void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        _shortcutManager.ProcessKeyDown(keyboard, key);
    }

    private static async Task Save()
    {
        var scene = SceneManager.CurrentScene;
        if (scene == null) return;
        try
        {
            await Editor.TaskQueue.Enqueue(scene.SaveAsync, "Saving scene");
            EditorAction.IsDirty = false;
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to save scene: " + e);
        }
    }
}