using System.Numerics;
using JoltPhysicsSharp;

namespace FlyEngine.Core.Components;

public class BoxCollider : Collider
{
    public Vector3 HalfExtent { get; set; }

    protected override void CreateBody(MotionType motionType)
    {
        BodyId = Core.Physics.Physics.CreateBody(new BoxShape(HalfExtent), Transform.Position, Transform.Rotation,
            Core.Physics.Physics.Layers.Moving, motionType);
    }
}