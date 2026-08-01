using FlyEngine.Core.ECS.Systems;
using MemoryPack;

namespace FlyEngine.Core.ECS;

[MemoryPackable]
public partial class World
{
    [MemoryPackInclude]
    private int _capacity = 1024;
    [MemoryPackInclude]
    private int _entityCount;
    [MemoryPackInclude]
    private Queue<int> _freeIds = [];
    
    [MemoryPackIgnore]
    private IComponentPool?[] _pools = new IComponentPool[32];
    
    [MemoryPackInclude]
    private Dictionary<string, byte[]> _serializedPools = new();
    [MemoryPackIgnore]
    private readonly List<ISystem> _systems = [];
    
    [MemoryPackOnSerializing]
    private void OnSerializing()
    {
        _serializedPools.Clear();
        for (var i = 0; i < _pools.Length; i++)
        {
            var pool = _pools[i];
            if (pool == null) continue;

            var typeName = pool.GetType().GetGenericArguments()[0].AssemblyQualifiedName!;
            _serializedPools[typeName] = pool.SerializePool();
        }
    }

    [MemoryPackOnDeserialized]
    private void OnDeserialized()
    {
        _pools = new IComponentPool[System.Math.Max(32, _serializedPools.Count * 2)];

        foreach (var kvp in _serializedPools)
        {
            var componentType = Type.GetType(kvp.Key);
            if (componentType == null) continue;

            var poolType = typeof(ComponentPool<>).MakeGenericType(componentType);
            var pool = (IComponentPool)Activator.CreateInstance(poolType)!;

            pool.DeserializePool(kvp.Value);

            var typeId = GetRuntimeComponentTypeId(componentType); 
            
            if (typeId >= _pools.Length)
                Array.Resize(ref _pools, typeId + 1);

            _pools[typeId] = pool;
        }

        _serializedPools.Clear();
    }

    private static int GetRuntimeComponentTypeId(Type componentType)
    {
        var openType = typeof(ComponentTypeId<>).MakeGenericType(componentType);
        var field = openType.GetField("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        return (int)field!.GetValue(null)!;
    }

    public void Update(float deltaTime)
    {
        for (var i = 0; i < _systems.Count; i++)
            _systems[i].Update(this, deltaTime);
    }
    
    public int CreateEntity()
    {
        if (_freeIds.Count > 0) return _freeIds.Dequeue();
        
        var id = _entityCount++;
        if (id >= _capacity) ResizeWorld(_capacity * 2);
        return id;
    }

    public void DestroyEntity(int id)
    {
        _entityCount--;
        _freeIds.Enqueue(id);
        
        for (var i = 0; i < _pools.Length; i++)
        {
            var pool = _pools[i];
            pool?.Remove(id);
        }
    }

    public ComponentPool<T> GetPool<T>() where T : struct
    {
        var typeId = ComponentTypeId<T>.Id;

        if (typeId >= _pools.Length)
            Array.Resize(ref _pools, System.Math.Max(typeId + 1, _pools.Length * 2));

        _pools[typeId] ??= new ComponentPool<T>(_capacity);

        return (ComponentPool<T>)_pools[typeId]!;
    }
    
    public List<IComponentPool> GetEntityComponents(int entityId)
    {
        var activePools = new List<IComponentPool>();

        for (var i = 0; i < _pools.Length; i++)
        {
            var pool = _pools[i];
            
            if (pool != null && pool.HasEntity(entityId))
                activePools.Add(pool);
        }

        return activePools;
    }
    
    public T AddSystem<T>() where T : ISystem, new()
    {
        var system = new T();
        _systems.Add(system);
        return system;
    }

    public void SetComponent<T>(int entityId, T component) where T : struct
    {
        var pool = GetPool<T>();
        pool.Instances[entityId] = component;
    }

    public ref T GetComponent<T>(int entityId) where T : struct
    {
        return ref GetPool<T>().Instances[entityId];
    }

    private void ResizeWorld(int newCapacity)
    {
        _capacity = newCapacity;
        foreach (var pool in _pools)
            pool?.Resize(_capacity);
    }
}