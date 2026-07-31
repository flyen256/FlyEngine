using System.Numerics;
using FlyEngine.Core.Components;

namespace FlyEngine.Core.Physics;

public struct RaycastHit
{
    public Vector3 Point;
    public Collider? Collider;
    public Rigidbody? Rigidbody;
}