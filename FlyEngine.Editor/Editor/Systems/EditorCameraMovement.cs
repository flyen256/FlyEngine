namespace FlyEngine.Editor.Systems;

public class EditorCameraMovement : EditorSystem
{
    public static EditorCameraMovement? Instance { get; private set; }

    public EditorCameraMovement()
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