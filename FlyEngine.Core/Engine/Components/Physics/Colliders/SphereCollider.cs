using JoltPhysicsSharp;

namespace FlyEngine.Core.Components;

public class SphereCollider : Collider
{
    public float Radius { get; set; } = 0.5f;

    protected override void CreateBody(MotionType motionType)
    {
        BodyId = Core.Physics.Physics.CreateBody(new SphereShape(Radius), Transform.Position, Transform.Rotation,
            Core.Physics.Physics.Layers.Moving, motionType);
    }
}