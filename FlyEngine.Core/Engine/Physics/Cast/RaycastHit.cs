using System.Numerics;
using FlyEngine.Core.Components;
using FlyEngine.Core.Components.Colliders;

namespace FlyEngine.Core.Cast;

public struct RaycastHit
{
    public Vector3 Point;
    public Collider? Collider;
    public Rigidbody? Rigidbody;
}