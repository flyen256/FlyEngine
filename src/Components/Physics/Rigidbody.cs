using System.Numerics;
using FlyEngine.Components.Common;
using FlyEngine.Components.Physics.Colliders;
using JoltPhysicsSharp;

namespace FlyEngine.Components.Physics;

public class Rigidbody : Behaviour
{
    public BodyID BodyId { get; private set; }
    public bool IsKinematic { get; private set; }
    
    private Collider? _collider;
    
    public override void OnLoad()
    {
        if (!TryGetComponent<Collider>(out var collider)) return;
        BodyId = collider.CreateBody();
        _collider = collider;
    }

    public override void OnUpdate(double deltaTime)
    {
        if (_collider == null) return;
        if (_collider.MotionType != MotionType.Dynamic || IsKinematic)
        {
            Application.Instance.Physics.SetPosition(BodyId, Transform.Position);
            return;
        }
        Transform.Position = GetPosition();
        Transform.Rotation = GetRotation();
    }

    private Vector3 GetPosition()
    {
        return Application.Instance.Physics.GetPosition(BodyId);
    }
    
    private Quaternion GetRotation()
    {
        return Application.Instance.Physics.GetRotation(BodyId);
    }
}