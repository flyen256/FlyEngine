using JoltPhysicsSharp;

namespace FlyEngine.Core.Engine.Components.Physics.Colliders;

public class SphereCollider : Collider
{
    public float Radius { get; set; } = 0.5f;

    public override BodyID CreateBody()
    {
        BodyId = Application.Instance.Physics.CreateBody(new SphereShape(Radius), Transform.Position, Transform.Rotation,
            Engine.Physics.Physics.Layers.Moving, MotionType);
        return BodyId;
    }
}