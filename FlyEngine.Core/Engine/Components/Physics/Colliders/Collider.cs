using FlyEngine.Core.Components.Common;
using JoltPhysicsSharp;

namespace FlyEngine.Core.Components.Colliders;

public class Collider : Component
{
    public BodyID BodyId { get; protected set; } = BodyID.Invalid;

    protected override void OnInitialize()
    {
        CreateBody(TryGetComponent(out Rigidbody? rigidBody) ? rigidBody.MotionType : MotionType.Static);
        Physics.SetPosition(BodyId, Transform.Position);
    }

    protected virtual void CreateBody(MotionType motionType) { }

    public virtual bool IsValid()
    {
        return BodyId.IsValid && BodyId != BodyID.Invalid;
    }
}