using MemoryPack;

namespace FlyEngine.Core.ECS;

[MemoryPackable]
public partial class ComponentPool<T> : IComponentPool
    where T : struct
{
    [MemoryPackInclude]
    public T[] Instances;
    
    [MemoryPackInclude]
    private bool[] _hasComponent;

    [MemoryPackConstructor]
    public ComponentPool()
    {
        Instances = [];
        _hasComponent = [];
    }

    public ComponentPool(int initialCapacity)
    {
        Instances = new T[initialCapacity];
        _hasComponent = new bool[initialCapacity];
    }

    public void Resize(int newCapacity)
    {
        Array.Resize(ref Instances, newCapacity);
        Array.Resize(ref _hasComponent, newCapacity);
    }

    public void Remove(int entityId)
    {
        if (entityId >= _hasComponent.Length) return;
        _hasComponent[entityId] = false;
        Instances[entityId] = default;
    }

    public bool HasEntity(int entityId) => entityId < _hasComponent.Length && _hasComponent[entityId];

    public byte[] SerializePool() => MemoryPackSerializer.Serialize(this);

    public void DeserializePool(byte[] bytes)
    {
        ComponentPool<T>? value = null;
        MemoryPackSerializer.Deserialize(bytes, ref value);
        if (value == null) return;
        Instances = value.Instances;
        _hasComponent = value._hasComponent;
    }
}