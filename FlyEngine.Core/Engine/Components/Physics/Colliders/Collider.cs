using FlyEngine.Core.Components.Common;
using JoltPhysicsSharp;

namespace FlyEngine.Core.Components.Colliders;

public class Collider : Component
{
    public BodyID BodyId { get; protected set; }
    public MotionType MotionType { get; set; } = MotionType.Dynamic;

    protected override void OnInitialize()
    {
        CreateBody();
        Physics.SetPosition(BodyId, Transform.Position);
    }

    public virtual BodyID CreateBody() { return default; }
}