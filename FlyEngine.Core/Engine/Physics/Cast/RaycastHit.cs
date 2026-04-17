using System.Numerics;
using FlyEngine.Core.Engine.Components.Physics;
using FlyEngine.Core.Engine.Components.Physics.Colliders;

namespace FlyEngine.Core.Engine.Physics.Cast;

public struct RaycastHit
{
    public Vector3 Point;
    public Collider? Collider;
    public Rigidbody? Rigidbody;
}