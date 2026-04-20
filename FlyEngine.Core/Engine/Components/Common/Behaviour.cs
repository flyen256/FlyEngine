namespace FlyEngine.Core.Engine.Components.Common;

public class Behaviour : Component
{
    public override bool AllowMultipleInstances => false;

    public virtual void OnLoad() { }
    public virtual void OnUpdate(double deltaTime) { }
    public virtual void OnRender(double deltaTime) { }
}