using System.Numerics;
using FlyEngine.Core.Extensions;
using FlyEngine.Core.SceneManagement;
using MemoryPack;

namespace FlyEngine.Core.Components;

[MemoryPackable]
public partial struct TransformComponent : IEquatable<TransformComponent>
{
    [MemoryPackInclude] public Guid Guid { get; set; }
    [MemoryPackIgnore] public Guid LazyGuid { get; set; } = Guid.Empty;

    [MemoryPackInclude] private int _parentEntityId = -1;
    [MemoryPackInclude] private int _firstChildEntityId = -1;
    [MemoryPackInclude] private int _nextSiblingEntityId = -1;
    [MemoryPackInclude] private int _prevSiblingEntityId = -1;

    [MemoryPackInclude] public Guid ParentGuid { get; set; } = Guid.Empty;

    [MemoryPackIgnore] public GameObject? GameObject { get; set; }
    [MemoryPackIgnore] public bool IsEcsMode => GameObject == null;
    [MemoryPackInclude] public int EntityId { get; set; }

    [MemoryPackInclude] public Vector3 Euler { get; set; }
    [MemoryPackInclude] private Vector3 _localPosition = Vector3.Zero;
    [MemoryPackInclude] private Quaternion _localRotation = Quaternion.Identity;
    [MemoryPackInclude] private Vector3 _localScale = Vector3.One;
    [MemoryPackInclude] private Matrix4x4 _worldMatrix = Matrix4x4.Identity;
    
    [MemoryPackIgnore] private bool _isDirty = true;

    [MemoryPackIgnore]
    public TransformComponent? Parent
    {
        get
        {
            if (!IsEcsMode) return GameObject?.ParentGameObject?.Transform;
            if (SceneManager.CurrentScene == null || _parentEntityId == -1) return null;
            var pool = SceneManager.CurrentScene.EcsWorld.GetPool<TransformComponent>();
            return _parentEntityId >= 0 && _parentEntityId < pool.Instances.Length
                ? pool.Instances[_parentEntityId]
                : null;
        }
    }

    public void SetParent(GameObject? newParentGo, int newParentEntityId = -1)
    {
        if (IsEcsMode)
        {
            if (SceneManager.CurrentScene == null) return;
            var pool = SceneManager.CurrentScene.EcsWorld.GetPool<TransformComponent>();
            
            ref var current = ref pool.Instances[EntityId];

            if (_parentEntityId != -1 && _parentEntityId < pool.Instances.Length)
            {
                ref var oldParent = ref pool.Instances[_parentEntityId];

                if (oldParent._firstChildEntityId == EntityId)
                    oldParent._firstChildEntityId = current._nextSiblingEntityId;

                if (current._prevSiblingEntityId != -1)
                    pool.Instances[current._prevSiblingEntityId]._nextSiblingEntityId = current._nextSiblingEntityId;
                    
                if (current._nextSiblingEntityId != -1)
                    pool.Instances[current._nextSiblingEntityId]._prevSiblingEntityId = current._prevSiblingEntityId;

                current._parentEntityId = -1;
                current._nextSiblingEntityId = -1;
                current._prevSiblingEntityId = -1;
                
                oldParent.SetDirty();
            }

            _parentEntityId = newParentEntityId;

            if (_parentEntityId != -1 && _parentEntityId < pool.Instances.Length)
            {
                ref var newParent = ref pool.Instances[_parentEntityId];
                current._parentEntityId = _parentEntityId;
                
                var formerFirstChildId = newParent._firstChildEntityId;
                
                newParent._firstChildEntityId = EntityId;
                current._nextSiblingEntityId = formerFirstChildId;
                current._prevSiblingEntityId = -1;

                if (formerFirstChildId != -1)
                    pool.Instances[formerFirstChildId]._prevSiblingEntityId = EntityId;
                
                newParent.SetDirty();
            }
        
            current.SetDirty();
        }
        else if (GameObject != null)
        {
            var parentGameObject = GameObject.ParentGameObject;
            if (parentGameObject != null)
            {
                parentGameObject.RemoveChild(GameObject);
                parentGameObject.Transform.SetDirty();
            }

            parentGameObject = newParentGo;
            GameObject.SetParent(parentGameObject);

            ParentGuid = newParentGo != null ? newParentGo.Transform.Guid : Guid.Empty;

            if (parentGameObject != null)
            {
                parentGameObject.AddChild(GameObject);
                parentGameObject.Transform.SetDirty();
            }
        }

        SetDirty();
    }

    [MemoryPackIgnore]
    public Vector3 LocalPosition
    {
        get => _localPosition;
        set
        {
            if (_localPosition.Equals(value)) return;
            _localPosition = value;
            SetDirty();
        }
    }

    [MemoryPackIgnore]
    public Quaternion LocalRotation
    {
        get => _localRotation;
        set
        {
            if (_localRotation.Equals(value)) return;
            _localRotation = value;
            SetDirty();
            Euler = value.ToEulerAngles();
        }
    }

    [MemoryPackIgnore]
    public Vector3 LocalScale
    {
        get => _localScale;
        set
        {
            if (_localScale.Equals(value)) return;
            _localScale = value;
            SetDirty();
        }
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
            var parent = Parent;
            if (!parent.HasValue)
                LocalPosition = value;
            else if (Matrix4x4.Invert(parent.Value.WorldMatrix, out var invertedParentMatrix))
                LocalPosition = Vector3.Transform(value, invertedParentMatrix);
        }
    }

    [MemoryPackIgnore]
    public Quaternion Rotation
    {
        get => Quaternion.CreateFromRotationMatrix(WorldMatrix);
        set
        {
            var parent = Parent;
            if (!parent.HasValue)
                LocalRotation = value;
            else
                LocalRotation = Quaternion.Inverse(parent.Value.Rotation) * value;
        }
    }

    [MemoryPackIgnore]
    public Vector3 Scale
    {
        get => GetWorldScaleFromMatrix();
        set
        {
            var parent = Parent;
            if (!parent.HasValue)
                LocalScale = value;
            else
            {
                var parentScale = parent.Value.Scale;
                LocalScale = new Vector3(
                    parentScale.X != 0 ? value.X / parentScale.X : 0,
                    parentScale.Y != 0 ? value.Y / parentScale.Y : 0,
                    parentScale.Z != 0 ? value.Z / parentScale.Z : 0
                );
            }
        }
    }

    [MemoryPackIgnore] public Vector3 Forward => Vector3.Transform(new Vector3(0, 0, -1), Rotation);
    [MemoryPackIgnore] public Vector3 Right => Vector3.Transform(new Vector3(1, 0, 0), Rotation);
    [MemoryPackIgnore] public Vector3 Up => Vector3.Transform(new Vector3(0, 1, 0), Rotation);

    [MemoryPackConstructor]
    public TransformComponent()
    {
    }

    public TransformComponent(Guid guid)
    {
        Guid = guid;
    }

    private void UpdateWorldMatrix()
    {
        var localMatrix = Matrix4x4.CreateScale(_localScale) *
                          Matrix4x4.CreateFromQuaternion(_localRotation) *
                          Matrix4x4.CreateTranslation(_localPosition);

        var parent = Parent;
        if (!parent.HasValue)
            _worldMatrix = localMatrix;
        else
            _worldMatrix = localMatrix * parent.Value.WorldMatrix;

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

    public void SetDirty()
    {
        if (_isDirty) return;
        _isDirty = true;

        if (IsEcsMode)
        {
            var pool = SceneManager.CurrentScene?.EcsWorld.GetPool<TransformComponent>();
            if (pool == null) return;

            var currentChildId = _firstChildEntityId;

            while (currentChildId != -1)
            {
                ref var childTransform = ref pool.Instances[currentChildId];

                currentChildId = childTransform._nextSiblingEntityId; 
                pool.Instances[currentChildId].SetDirty();
            }
        }
        else if (GameObject != null)
        {
            foreach (var childGo in GameObject.ChildrenGameObjects)
            {
                ref var transform = ref childGo.Transform;
                transform.SetDirty();
            }
        }
    }

    public void ResolveReferences(ReadOnlySpan<GameObject> gameObjects)
    {
        for (var i = 0; i < gameObjects.Length; i++)
        {
            var gameObject = gameObjects[i];
            if (!IsEcsMode && ParentGuid != Guid.Empty && gameObject.Transform.Guid == ParentGuid)
                SetParent(gameObject);
        }
    }

    public bool Equals(TransformComponent other) => Guid == other.Guid && EntityId == other.EntityId;
    public override bool Equals(object? obj) => obj is TransformComponent other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Guid, EntityId);
}