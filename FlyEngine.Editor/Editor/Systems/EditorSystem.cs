namespace FlyEngine.Editor.Systems;

public abstract class EditorSystem
{
    public abstract void OnUpdate(double deltaTime);
    public abstract void OnRender(double deltaTime);
}