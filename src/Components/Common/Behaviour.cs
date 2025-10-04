namespace Flyeng;

public class Behaviour : Component
{
    protected override void OnInitialize()
    {
        base.OnInitialize();
        Application.Behaviours.Add(this);
    }

    public virtual void OnLoad() { }
    public virtual void OnUpdate(double deltaTime) { }
    public virtual void OnRender(double deltaTime) { }
    public virtual void OnCollision(CollisionResult result) { }
}
