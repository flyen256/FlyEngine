using System.Numerics;
using JoltPhysicsSharp;

namespace FlyEngine.Core.Engine.Components.Physics.Colliders;

public class BoxCollider : Collider
{
    public Vector3 HalfExtent { get; set; }

    public override BodyID CreateBody()
    {
        BodyId = Application.Instance.Physics.CreateBody(new BoxShape(HalfExtent), Transform.Position, Transform.Rotation,
            Engine.Physics.Physics.Layers.Moving, MotionType);
        return BodyId;
    }
}