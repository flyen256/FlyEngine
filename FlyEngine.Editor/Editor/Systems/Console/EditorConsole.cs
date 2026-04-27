namespace FlyEngine.Editor.Systems.Console;

public class EditorConsole : EditorSystem
{
    public static EditorConsole? Instance { get; private set; }

    public readonly List<EditorConsoleMessage> Messages = [];

    public EditorConsole()
    {
        Instance = this;
    }
    
    public override void OnUpdate(double deltaTime)
    {
        
    }

    public override void OnRender(double deltaTime)
    {
        
    }
}