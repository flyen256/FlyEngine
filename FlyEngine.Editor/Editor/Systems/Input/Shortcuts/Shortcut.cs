using Silk.NET.Input;

namespace FlyEngine.Editor.Systems;

public class Shortcut(Key key, KeyModifiers modifiers, Action action, string description = "")
{
    public Key Key { get; set; } = key;
    public KeyModifiers Modifiers { get; set; } = modifiers;
    public Action Action { get; set; } = action;
    public string Description { get; set; } = description;
}