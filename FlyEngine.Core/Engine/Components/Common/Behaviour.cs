namespace FlyEngine.Core.Components.Common;

public class Behaviour : Component
{
    public override bool AllowMultipleInstances => false;

    protected override void OnInitialize()
    {
        Application.Instance.Behaviours.Add(this);
    }

    public virtual void OnLoad() { }
    public virtual void OnUpdate(double deltaTime) { }
    public virtual unsafe void OnRender(double deltaTime) { }
}
