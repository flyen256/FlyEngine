using System.Numerics;
using FlyEngine.Core.Components.Physics;
using FlyEngine.Core.Components.Physics.Colliders;

namespace FlyEngine.Core.Physics.Cast;

public struct RaycastHit
{
    public Vector3 Point;
    public Collider? Collider;
    public Rigidbody? Rigidbody;
}