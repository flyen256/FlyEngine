using FlyEngine.Core;
using FlyEngine.Core.Debugging;
using FlyEngine.Core.Input;
using FlyEngine.Core.SceneManagement;
using Silk.NET.Input;

namespace FlyEngine.Editor.Systems;

public class EditorInput : EditorSystem
{
    private readonly ShortcutManager _shortcutManager = new();

    private bool _releaseInputCursorLocked;
    private bool _releaseInputCursorVisible;
    private bool _inputReleased;

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

    private void OnApplicationState(bool isRunning)
    {
        if (isRunning) return;
        _inputReleased = false;
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

    private void ReleaseInput()
    {
        _releaseInputCursorLocked = Input.CursorLocked;
        _releaseInputCursorVisible = Input.CursorVisible;
        Input.CursorLocked = false;
        Input.CursorVisible = true;
        _inputReleased = true;
        Input.BlockInput = _inputReleased;
    }

    public void BlockInput()
    {
        Input.CursorLocked = _releaseInputCursorLocked;
        Input.CursorVisible = _releaseInputCursorVisible;
        _inputReleased = false;
        Input.BlockInput = _inputReleased;
    }
}