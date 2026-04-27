using System.Numerics;
using System.Text.Json.Serialization;
using FlyEngine.Core.Components.Colliders;
using FlyEngine.Core.Components.Common;
using JoltPhysicsSharp;

namespace FlyEngine.Core.Components;

public class Rigidbody : Behaviour
{
    public bool IsKinematic { get; set; }
    
    [JsonIgnore]
    private Collider? _collider;
    
    public override void OnLoad()
    {
        if (!TryGetComponent<Collider>(out var collider)) return;
        _collider = collider;
    }

    public override void OnUpdate(double deltaTime)
    {
        if (_collider == null)
        {
            if (!TryGetComponent<Collider>(out var collider)) return;
            _collider = collider;
            return;
        }
        if (_collider.MotionType != MotionType.Dynamic || IsKinematic)
        {
            Physics.SetPosition(_collider.BodyId, Transform.Position);
            return;
        }
        Transform.Position = GetPosition();
        Transform.Rotation = GetRotation();
    }

    public void AddForce(Vector3 force)
    {
        if (CanApplyPhysics())
            Physics.BodyInterface.AddForce(_collider!.BodyId, force);
    }
    
    public void AddImpulse(Vector3 impulse)
    {
        if (CanApplyPhysics())
            Physics.BodyInterface.AddImpulse(_collider!.BodyId, impulse);
    }
    
    public void AddForce(Vector3 force, Vector3 worldPosition)
    {
        if (CanApplyPhysics())
            Physics.BodyInterface.AddForce(_collider!.BodyId, force, worldPosition);
    }

    public void AddImpulse(Vector3 impulse, Vector3 worldPosition)
    {
        if (CanApplyPhysics())
            Physics.BodyInterface.AddImpulse(_collider!.BodyId, impulse, worldPosition);
    }

    private bool CanApplyPhysics()
    {
        return _collider != null && !IsKinematic && _collider.MotionType == MotionType.Dynamic;
    }

    private Vector3 GetPosition()
    {
        return _collider != null ? Physics.GetPosition(_collider.BodyId) : Vector3.Zero;
    }
    
    private Quaternion GetRotation()
    {
        return _collider != null ? Physics.GetRotation(_collider.BodyId) : Quaternion.Identity;
    }
}