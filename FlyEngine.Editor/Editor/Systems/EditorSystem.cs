namespace FlyEngine.Editor.Systems;

public abstract class EditorSystem
{
    public virtual void OnUpdate(float deltaTime) { }
    public virtual void OnRender(float deltaTime) { }
}