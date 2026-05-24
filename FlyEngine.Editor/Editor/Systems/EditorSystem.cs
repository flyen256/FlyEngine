namespace FlyEngine.Editor.Systems;

public abstract class EditorSystem
{
    public virtual void OnUpdate(double deltaTime) { }
    public virtual void OnRender(double deltaTime) { }
}