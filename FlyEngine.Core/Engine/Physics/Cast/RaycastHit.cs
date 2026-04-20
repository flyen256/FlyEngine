using System.Numerics;
using FlyEngine.Core.Engine.Components;
using FlyEngine.Core.Engine.Components.Colliders;

namespace FlyEngine.Core.Engine.Cast;

public struct RaycastHit
{
    public Vector3 Point;
    public Collider? Collider;
    public Rigidbody? Rigidbody;
}