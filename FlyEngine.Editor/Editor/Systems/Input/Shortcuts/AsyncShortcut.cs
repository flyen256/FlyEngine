using Silk.NET.Input;

namespace FlyEngine.Editor.Systems;

public class AsyncShortcut(Key key, KeyModifiers modifiers, Func<Task> action, string description = "")
{
    public Key Key { get; set; } = key;
    public KeyModifiers Modifiers { get; set; } = modifiers;
    public Func<Task> Action { get; set; } = action;
    public string Description { get; set; } = description;
}