using System.Numerics;
using JoltPhysicsSharp;

namespace FlyEngine.Core.Components.Colliders;

public class BoxCollider : Collider
{
    public Vector3 HalfExtent { get; set; }

    protected override void CreateBody(MotionType motionType)
    {
        BodyId = Physics.CreateBody(new BoxShape(HalfExtent), Transform.Position, Transform.Rotation,
            Physics.Layers.Moving, motionType);
    }
}