using System.Numerics;
using JoltPhysicsSharp;

namespace FlyEngine.Components.Physics.Colliders;

public class BoxCollider : Collider
{
    public Vector3 HalfExtent { get; set; }

    public override BodyID CreateBody()
    {
        return Application.Instance.Physics.CreateBody(new BoxShape(HalfExtent), Transform.Position, Transform.Rotation,
            FlyEngine.Physics.Physics.Layers.Moving, MotionType);
    }
}