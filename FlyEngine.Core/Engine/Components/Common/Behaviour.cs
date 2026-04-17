namespace FlyEngine.Core.Engine.Components.Common;

public class Behaviour : Component
{
    public override bool AllowMultipleInstances => false;

    protected override void OnInitialize()
    {
        Application.Instance.Behaviours.Add(this);
    }

    protected internal override void OnRemoved()
    {
        Application.Instance.Behaviours.Remove(this);
    }

    public virtual void OnLoad() { }
    public virtual void OnUpdate(double deltaTime) { }
    public virtual void OnRender(double deltaTime) { }
}
