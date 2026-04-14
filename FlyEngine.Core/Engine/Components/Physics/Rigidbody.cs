using System.Numerics;
using FlyEngine.Core.Components.Common;
using FlyEngine.Core.Components.Physics.Colliders;
using JoltPhysicsSharp;

namespace FlyEngine.Core.Components.Physics;

public class Rigidbody : Behaviour
{
    public bool IsKinematic { get; private set; }
    
    private Collider? _collider;
    
    public override void OnLoad()
    {
        if (!TryGetComponent<Collider>(out var collider)) return;
        _collider = collider;
    }

    public override void OnUpdate(double deltaTime)
    {
        if (_collider == null) return;
        if (_collider.MotionType != MotionType.Dynamic || IsKinematic)
        {
            Application.Instance.Physics.SetPosition(_collider.BodyId, Transform.Position);
            return;
        }
        Transform.Position = GetPosition();
        Transform.Rotation = GetRotation();
    }

    public void AddForce(Vector3 force)
    {
        if (_collider == null || IsKinematic || _collider.MotionType != MotionType.Dynamic) return;
        Application.Instance.Physics.BodyInterface.AddForce(_collider.BodyId, force);
    }
    
    public void AddImpulse(Vector3 impulse)
    {
        if (_collider == null || IsKinematic || _collider.MotionType != MotionType.Dynamic) return;
        Application.Instance.Physics.BodyInterface.AddImpulse(_collider.BodyId, impulse);
    }

    private Vector3 GetPosition()
    {
        return _collider != null ? Application.Instance.Physics.GetPosition(_collider.BodyId) : Vector3.Zero;
    }
    
    private Quaternion GetRotation()
    {
        return _collider != null ? Application.Instance.Physics.GetRotation(_collider.BodyId) : Quaternion.Identity;
    }
}