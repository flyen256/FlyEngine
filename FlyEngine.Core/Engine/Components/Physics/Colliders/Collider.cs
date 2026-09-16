using JoltPhysicsSharp;

namespace FlyEngine.Core.Components;

public class Collider : Component
{
    public BodyID BodyId { get; protected set; } = BodyID.Invalid;

    public override void OnEnable()
    {
        CreateBody(TryGetComponent(out Rigidbody? rigidBody) ? rigidBody.MotionType : MotionType.Static);
        Physics.Physics.SetPosition(BodyId, Transform.Position);
    }
    
    public override void OnDisable()
    {
        Destroy();
    }

    public override void Destroy()
    {
        if (BodyId.IsInvalid || Physics.Physics.System.IsDisposed) return;
        Physics.Physics.DestroyBody(BodyId);
        BodyId = BodyID.Invalid;
    }

    protected virtual void CreateBody(MotionType motionType) { }

    public virtual bool IsValid()
    {
        return BodyId.IsValid && BodyId != BodyID.Invalid;
    }
}