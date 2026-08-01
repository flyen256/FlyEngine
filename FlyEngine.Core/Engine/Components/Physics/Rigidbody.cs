using System.Numerics;
using System.Text.Json.Serialization;
using JoltPhysicsSharp;

namespace FlyEngine.Core.Components;

public class Rigidbody : Behaviour
{
    public bool IsKinematic { get; set; }
    public MotionType MotionType { get; set; } = MotionType.Dynamic;
    
    [JsonIgnore]
    private Collider? _collider;
    
    public override void OnLoad()
    {
        if (!TryGetComponent<Collider>(out var collider)) return;
        _collider = collider;
    }

    public override void OnUpdate(float deltaTime)
    {
        if (_collider == null)
        {
            if (!TryGetComponent<Collider>(out var collider)) return;
            _collider = collider;
            return;
        }
        if (MotionType != MotionType.Dynamic || IsKinematic)
        {
            Core.Physics.Physics.SetPosition(_collider.BodyId, Transform.Position);
            return;
        }

        var transform = Transform;
        transform.Position = GetPosition();
        transform.Rotation = GetRotation();
        Transform = transform;
    }

    public void AddForce(Vector3 force)
    {
        if (CanApplyPhysics())
            Core.Physics.Physics.BodyInterface.AddForce(_collider!.BodyId, force);
    }
    
    public void AddImpulse(Vector3 impulse)
    {
        if (CanApplyPhysics())
            Core.Physics.Physics.BodyInterface.AddImpulse(_collider!.BodyId, impulse);
    }
    
    public void AddForce(Vector3 force, Vector3 worldPosition)
    {
        if (CanApplyPhysics())
            Core.Physics.Physics.BodyInterface.AddForce(_collider!.BodyId, force, worldPosition);
    }

    public void AddImpulse(Vector3 impulse, Vector3 worldPosition)
    {
        if (CanApplyPhysics())
            Core.Physics.Physics.BodyInterface.AddImpulse(_collider!.BodyId, impulse, worldPosition);
    }

    private bool CanApplyPhysics()
    {
        return _collider != null &&
               _collider.IsValid() &&
               !IsKinematic &&
               MotionType == MotionType.Dynamic;
    }

    private Vector3 GetPosition()
    {
        return _collider != null && _collider.IsValid() ?
            Core.Physics.Physics.GetPosition(_collider.BodyId) :
            Vector3.Zero;
    }
    
    private Quaternion GetRotation()
    {
        return _collider != null && _collider.IsValid() ?
            Core.Physics.Physics.GetRotation(_collider.BodyId) :
            Quaternion.Identity;
    }
}