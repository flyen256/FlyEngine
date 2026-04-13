using JoltPhysicsSharp;

namespace FlyEngine.Components.Physics.Colliders;

public class SphereCollider : Collider
{
    public float Radius { get; set; } = 0.5f;

    public override BodyID CreateBody()
    {
        return Application.Instance.Physics.CreateBody(new SphereShape(Radius), Transform.Position, Transform.Rotation,
            FlyEngine.Physics.Physics.Layers.Moving, MotionType);
    }
}