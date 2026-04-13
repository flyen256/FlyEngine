using FlyEngine.Components.Common;
using JoltPhysicsSharp;

namespace FlyEngine.Components.Physics.Colliders;

public class Collider : Component
{
    public MotionType MotionType { get; set; } = MotionType.Dynamic;
    
    public virtual BodyID CreateBody() { return default; }
}