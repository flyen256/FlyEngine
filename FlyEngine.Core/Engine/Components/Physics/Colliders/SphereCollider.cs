using JoltPhysicsSharp;

namespace FlyEngine.Core.Components.Colliders;

public class SphereCollider : Collider
{
    public float Radius { get; set; } = 0.5f;

    protected override void CreateBody(MotionType motionType)
    {
        BodyId = Physics.CreateBody(new SphereShape(Radius), Transform.Position, Transform.Rotation,
            Physics.Layers.Moving, motionType);
    }
}