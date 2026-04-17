using FlyEngine.Core.Engine.Components.Common;
using JoltPhysicsSharp;

namespace FlyEngine.Core.Engine.Components.Physics.Colliders;

public class Collider : Component
{
    public BodyID BodyId { get; protected set; }
    public MotionType MotionType { get; set; } = MotionType.Dynamic;

    protected override void OnInitialize()
    {
        CreateBody();
    }

    public virtual BodyID CreateBody() { return default; }
}