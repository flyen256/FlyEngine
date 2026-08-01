using Silk.NET.Input;

namespace FlyEngine.Editor.Systems;

public class ShortcutManager
{
    private readonly List<Shortcut> _shortcuts = [];
    private readonly List<AsyncShortcut> _asyncShortcuts = [];

    public void Register(Key key, KeyModifiers modifiers, Action action, string description = "")
    {
        _shortcuts.Add(new Shortcut(key, modifiers, action, description));
    }
    
    public void Register(Key key, KeyModifiers modifiers, Func<Task> action, string description = "")
    {
        _asyncShortcuts.Add(new AsyncShortcut(key, modifiers, action, description));
    }

    public void Clear()
    {
        _shortcuts.Clear();
        _asyncShortcuts.Clear();
    }

    public void ProcessKeyDown(IKeyboard keyboard, Key key)
    {
        var currentModifiers = KeyModifiers.None;
        
        if (keyboard.IsKeyPressed(Key.ControlLeft) || keyboard.IsKeyPressed(Key.ControlRight))
            currentModifiers |= KeyModifiers.Ctrl;
            
        if (keyboard.IsKeyPressed(Key.ShiftLeft) || keyboard.IsKeyPressed(Key.ShiftRight))
            currentModifiers |= KeyModifiers.Shift;
            
        if (keyboard.IsKeyPressed(Key.AltLeft) || keyboard.IsKeyPressed(Key.AltRight))
            currentModifiers |= KeyModifiers.Alt;

        foreach (var shortcut in _shortcuts.Where(shortcut =>
                     shortcut.Key == key && shortcut.Modifiers == currentModifiers))
        {
            shortcut.Action.Invoke();
            break;
        }
        foreach (var shortcut in _asyncShortcuts.Where(shortcut =>
                     shortcut.Key == key && shortcut.Modifiers == currentModifiers))
        {
            shortcut.Action.Invoke();
            break;
        }
    }
}