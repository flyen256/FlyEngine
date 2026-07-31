using System.Diagnostics.CodeAnalysis;
using FlyEngine.Core.SceneManagement;
using MemoryPack;

namespace FlyEngine.Core.Components;

[MemoryPackable]
public partial class GameObject : Object
{
    [MemoryPackInclude]
    public bool Enabled { get; set; } = true;
    [MemoryPackIgnore]
    public bool IsDestroyed { get; private set; }

    [MemoryPackIgnore]
    private string _name;
    [MemoryPackInclude]
    public string Name
    {
        get => _name;
        set
        {
            if (value.Length == 0)
                return;
            if (Application.Scene != null && Application.Scene.ObjectExistsWithName(value))
            {
                var count = Application.Scene.GameObjects.Count(g => g.Name == value);
                _name = value + $"_{count}";
            }
            else
                _name = value;
        }
    }

    [MemoryPackIgnore]
    private Transform? _transform;

    [MemoryPackInclude]
    public Transform Transform
    {
        get => _transform!;
        set
        {
            _transform = value;
            _transform.GameObject = this;
        }
    }

    [MemoryPackIgnore]
    private ComponentStore _componentStore = null!;

    [MemoryPackInclude]
    public ComponentStore ComponentStore
    {
        get => _componentStore;
        set
        {
            if (value == null) return;
            _componentStore = value;
        }
    }

    [MemoryPackIgnore]
    public string LazyGameObjectName;

    public static GameObject CreateWithLazyReference(string name) =>
        new GameObject() { LazyGameObjectName = name };

    [MemoryPackConstructor]
    private GameObject()
    {
        ComponentStore = new ComponentStore
        {
            GameObject = this
        };
    }
    
    private GameObject(Transform transform, string name = "New game object")
    {
        Transform = transform;
        if (name.Length == 0)
            name = "New game object";
        _name = name;
        Name = name;
        ComponentStore = new ComponentStore
        {
            GameObject = this
        };
    }

    public static GameObject Create(string name, Component[]? components = null)
    {
        if (SceneManager.CurrentScene == null)
            throw new InvalidOperationException("No scene loaded");
        var gameObject = new GameObject(new Transform(Guid.NewGuid()), name);
        foreach (var component in components ?? [])
            gameObject.AddComponent(component);
        SceneManager.CurrentScene.AddGameObject(gameObject);
        return gameObject;
    }

    public override void Destroy()
    {
        
        ComponentStore.Dispose();
        IsDestroyed = true;
    }

    public T? GetComponent<T>() where T : class
    {
        return ComponentStore.GetComponent<T>();
    }

    public List<T> GetComponents<T>() where T : class
    {
        return ComponentStore.GetComponents<T>();
    }

    public Component? GetComponent(Type type)
    {
        return ComponentStore.GetComponent(type);
    }

    public T AddComponent<T>() where T : Component
    {
        return ComponentStore.AddComponent<T>();
    }
    
    public Component? AddComponent(Type component)
    {
        return ComponentStore.AddComponent(component);
    }

    public T AddComponent<T>(T component) where T : Component
    {
        return ComponentStore.AddComponent(component);
    }

    public bool TryGetComponent<T>([NotNullWhen(true)] out T? component) where T : Component
    {
        return ComponentStore.TryGetComponent(out component);
    }
}