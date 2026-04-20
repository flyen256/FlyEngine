using JoltPhysicsSharp;

namespace FlyEngine.Core.Engine.Components.Colliders;

public class SphereCollider : Collider
{
    public float Radius { get; set; } = 0.5f;

    public override BodyID CreateBody()
    {
        BodyId = Physics.CreateBody(new SphereShape(Radius), Transform.Position, Transform.Rotation,
            Physics.Layers.Moving, MotionType);
        return BodyId;
    }
}