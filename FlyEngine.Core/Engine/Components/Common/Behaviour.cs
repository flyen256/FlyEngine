namespace FlyEngine.Core.Components;

public class Behaviour : Component
{
    public override bool AllowMultipleInstances => false;

    public virtual void OnLoad() { }
    public virtual void OnUpdate(float deltaTime) { }
    public virtual void OnRender(float deltaTime) { }
}