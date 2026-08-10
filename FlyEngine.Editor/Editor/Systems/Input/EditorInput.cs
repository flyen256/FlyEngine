using FlyEngine.Core;
using FlyEngine.Core.Debugging;
using FlyEngine.Core.Input;
using FlyEngine.Core.SceneManagement;
using Silk.NET.Input;

namespace FlyEngine.Editor.Systems;

public class EditorInput : EditorSystem
{
    private readonly ShortcutManager _shortcutManager = new();

    private static bool _releaseInputCursorLocked;
    private static bool _releaseInputCursorVisible;

    public static bool InputReleased { get; private set; }

    public EditorInput()
    {
        _shortcutManager.Register(Key.S, KeyModifiers.Ctrl, Save, "Save");
        _shortcutManager.Register(Key.F1, KeyModifiers.Shift, ReleaseInput, "Release input");
        
        Input.OnKeyDownEvent += OnKeyDown;
        Application.OnApplicationState += OnApplicationState;
    }
    
    private void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        _shortcutManager.ProcessKeyDown(keyboard, key);
    }

    private static void OnApplicationState(bool isRunning)
    {
        if (isRunning) return;
        InputReleased = false;
        Input.BlockInput = false;
        _releaseInputCursorLocked = false;
        _releaseInputCursorVisible = true;
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

    private static void ReleaseInput()
    {
        _releaseInputCursorLocked = Input.CursorLocked;
        _releaseInputCursorVisible = Input.CursorVisible;
        Input.CursorLocked = false;
        Input.CursorVisible = true;
        InputReleased = true;
        Input.BlockInput = InputReleased;
    }

    public static void UnReleaseInput()
    {
        Input.CursorLocked = _releaseInputCursorLocked;
        Input.CursorVisible = _releaseInputCursorVisible;
        InputReleased = false;
        Input.BlockInput = InputReleased;
    }
}