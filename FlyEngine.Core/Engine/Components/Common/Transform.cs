using System.Numerics;
using MemoryPack;

namespace FlyEngine.Core.Engine.Components.Common;

[MemoryPackable]
public partial class Transform
{
    [MemoryPackInclude]
    private Transform? _parent;
    [MemoryPackInclude]
    private List<Transform> _children = [];

    [MemoryPackInclude]
    private Vector3 _localPosition = Vector3.Zero;
    [MemoryPackInclude]
    private Quaternion _localRotation = Quaternion.Identity;
    [MemoryPackInclude]
    private Vector3 _localScale = Vector3.One;

    [MemoryPackInclude]
    private Matrix4x4 _worldMatrix = Matrix4x4.Identity;
    [MemoryPackIgnore]
    private bool _isDirty = true;

    [MemoryPackIgnore]
    public Transform? Parent
    {
        get => _parent;
        set
        {
            _parent?._children.Remove(this);
            _parent = value;
            _parent?._children.Add(this);
            SetDirty();
        }
    }

    [MemoryPackIgnore]
    public Vector3 LocalPosition
    {
        get => _localPosition;
        set { _localPosition = value; SetDirty(); }
    }

    [MemoryPackIgnore]
    public Quaternion LocalRotation
    {
        get => _localRotation;
        set { _localRotation = value; SetDirty(); }
    }

    [MemoryPackIgnore]
    public Vector3 LocalScale
    {
        get => _localScale;
        set { _localScale = value; SetDirty(); }
    }

    [MemoryPackIgnore]
    public Matrix4x4 WorldMatrix
    {
        get
        {
            if (_isDirty) UpdateWorldMatrix();
            return _worldMatrix;
        }
    }
    
    [MemoryPackIgnore]
    public Vector3 Position
    {
        get => WorldMatrix.Translation;
        set
        {
            if (Parent == null)
                LocalPosition = value;
            else if(Matrix4x4.Invert(Parent.WorldMatrix, out var invertedParentMatrix))
                LocalPosition = Vector3.Transform(value, invertedParentMatrix);
        }
    }

    [MemoryPackIgnore]
    public Quaternion Rotation
    {
        get => Quaternion.CreateFromRotationMatrix(WorldMatrix);
        set
        {
            if (Parent == null)
                LocalRotation = value;
            else
                LocalRotation = Quaternion.Inverse(Parent.Rotation) * value;
        }
    }
    
    [MemoryPackIgnore]
    public Vector3 Scale
    {
        get => GetWorldScaleFromMatrix();
        set
        {
            if (Parent == null)
                LocalScale = value;
            else
            {
                var parentScale = Parent.Scale;
                LocalScale = new Vector3(
                    parentScale.X != 0 ? value.X / parentScale.X : 0,
                    parentScale.Y != 0 ? value.Y / parentScale.Y : 0,
                    parentScale.Z != 0 ? value.Z / parentScale.Z : 0
                );
            }
        }
    }

    [MemoryPackIgnore]
    public Vector3 Forward => Vector3.Transform(new Vector3(0, 0, -1), Rotation);

    private void UpdateWorldMatrix()
    {
        var localMatrix = Matrix4x4.CreateScale(_localScale) *
                          Matrix4x4.CreateFromQuaternion(_localRotation) *
                          Matrix4x4.CreateTranslation(_localPosition);

        if (Parent == null)
            _worldMatrix = localMatrix;
        else
            _worldMatrix = localMatrix * Parent.WorldMatrix;

        _isDirty = false;
    }
    
    public Vector3 GetWorldScaleFromMatrix()
    {
        var matrix = WorldMatrix;
        return new Vector3(
            new Vector3(matrix.M11, matrix.M12, matrix.M13).Length(),
            new Vector3(matrix.M21, matrix.M22, matrix.M23).Length(),
            new Vector3(matrix.M31, matrix.M32, matrix.M33).Length()
        );
    }

    private void SetDirty()
    {
        if (_isDirty) return;

        _isDirty = true;
        foreach (var child in _children)
            child.SetDirty();
    }
}