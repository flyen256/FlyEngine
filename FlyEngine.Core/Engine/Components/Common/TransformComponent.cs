using System;
using System.Collections.Generic;
using System.Numerics;
using FlyEngine.Core.Extensions;
using FlyEngine.Core.SceneManagement;
using FlyEngine.Core.Serialization.MemoryPack;
using MemoryPack;

namespace FlyEngine.Core.Components;

[MemoryPackable]
public partial struct TransformComponent : IEquatable<TransformComponent>
{
    [MemoryPackInclude] public Guid Guid { get; set; }

    [MemoryPackIgnore] public Guid LazyGuid { get; set; } = Guid.Empty;

    [MemoryPackIgnore] private int _parentEntityId = -1;
    [MemoryPackIgnore] private readonly List<int> _childrenEntityIds = [];

    [MemoryPackIgnore] public IReadOnlyList<int> ChildrenEntityIds => _childrenEntityIds;

    [MemoryPackInclude]
    public Guid ParentGuid { get; set; } = Guid.Empty;
    
    [MemoryPackIgnore] private GameObject? _parentGameObject;
    [MemoryPackIgnore] private readonly List<GameObject> _childrenGameObjects = [];
    
    [MemoryPackIgnore] public IReadOnlyList<GameObject> ChildrenGameObjects => _childrenGameObjects;

    [MemoryPackInclude]
    [GameObjectFormatter]
    public GameObject? GameObject { get; set; }

    [MemoryPackInclude] public int EntityId { get; set; }

    [MemoryPackIgnore] public bool IsEcsMode => GameObject == null;

    [MemoryPackInclude] private Vector3 _localPosition = Vector3.Zero;
    [MemoryPackInclude] private Quaternion _localRotation = Quaternion.Identity;
    [MemoryPackInclude] private Vector3 _localScale = Vector3.One;
    [MemoryPackInclude] public Vector3 Euler { get; set; }

    [MemoryPackInclude] private Matrix4x4 _worldMatrix = Matrix4x4.Identity;
    [MemoryPackIgnore] private bool _isDirty = true;

    [MemoryPackIgnore] public string LazyGameObjectName = string.Empty;

    [MemoryPackIgnore]
    public TransformComponent? Parent
    {
        get
        {
            if (!IsEcsMode) return _parentGameObject?.Transform;
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
            
            if (_parentEntityId != -1 && _parentEntityId < pool.Instances.Length)
            {
                pool.Instances[_parentEntityId]._childrenEntityIds.Remove(EntityId);
                pool.Instances[_parentEntityId].SetDirty();
            }

            _parentEntityId = newParentEntityId;
            if (_parentEntityId != -1 && _parentEntityId < pool.Instances.Length)
            {
                pool.Instances[_parentEntityId]._childrenEntityIds.Add(EntityId);
                pool.Instances[_parentEntityId].SetDirty();
            }
        }
        else if (GameObject != null)
        {
            if (_parentGameObject != null)
            {
                var transform = _parentGameObject.Transform;
                transform._childrenGameObjects.Remove(GameObject);
                transform.SetDirty();
                _parentGameObject.Transform = transform;
            }

            _parentGameObject = newParentGo;

            ParentGuid = newParentGo != null ? newParentGo.Transform.Guid : Guid.Empty;

            if (_parentGameObject != null)
            {
                var transform = _parentGameObject.Transform;
                transform._childrenGameObjects.Add(GameObject);
                transform.SetDirty();
                _parentGameObject.Transform = transform;
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
        Console.WriteLine("Update world matrix");
        var localMatrix = Matrix4x4.CreateScale(_localScale) *
                          Matrix4x4.CreateFromQuaternion(_localRotation) *
                          Matrix4x4.CreateTranslation(_localPosition);

        var parent = Parent;
        if (!parent.HasValue)
            _worldMatrix = localMatrix;
        else
            _worldMatrix = localMatrix * parent.Value.WorldMatrix;

        _isDirty = false;
        
        if (IsEcsMode)
        {
            var pool = SceneManager.CurrentScene?.EcsWorld.GetPool<TransformComponent>();
            if (pool == null || !pool.HasEntity(EntityId)) return;

            pool.Instances[EntityId] = this;
        }
        else if (GameObject != null)
            GameObject.Transform = this;
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

            foreach (var childId in _childrenEntityIds)
            {
                if (childId < 0 || childId >= pool.Instances.Length) continue;
                var child = pool.Instances[childId];
                child.SetDirty();
                pool.Instances[childId] = child;
            }
        }
        else
        {
            foreach (var childGo in _childrenGameObjects)
            {
                var transform = childGo.Transform;
                transform.SetDirty();
                childGo.Transform = transform;
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

            var parent = Parent;
            if (IsEcsMode && parent.HasValue && gameObject.Name == parent.Value.LazyGameObjectName)
                SetParent(gameObject, gameObject.EntityId);
        }
    }

    public static TransformComponent CreateWithLazyReference(string parentName) =>
        new() { LazyGameObjectName = parentName };

    public static TransformComponent CreateWithLazyGuid(Guid guid) =>
        new() { LazyGuid = guid };

    public bool Equals(TransformComponent other) => Guid == other.Guid && EntityId == other.EntityId;
    public override bool Equals(object? obj) => obj is TransformComponent other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Guid, EntityId);
}