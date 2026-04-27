using FlyEngine.Core.SceneManagement;
using MemoryPack;

namespace FlyEngine.Core.Components.Common;

[MemoryPackable]
public partial class GameObject : Object
{
    [MemoryPackInclude]
    public bool Enabled { get; set; } = true;
    [MemoryPackIgnore]
    public bool IsDestroyed { get; private set; }

    [MemoryPackInclude]
    private string _name;
    [MemoryPackIgnore]
    public string Name
    {
        get => _name;
        set
        {
            if (Application.Scene != null && Application.Scene.ObjectExistsWithName(value))
            {
                var count = Application.Scene.GameObjects.Count(g => g.Name == value);
                _name = value + $"_{count}";
            }
            else
                _name = value;
        }
    }
    [MemoryPackInclude]
    public required Transform Transform;

    [MemoryPackIgnore]
    private ComponentStore _componentStore;

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

    private GameObject(string name = "New game object")
    {
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
        var gameObject = new GameObject(name)
        {
            Transform = new Transform()
        };
        foreach (var component in components ?? [])
            gameObject.AddComponent(component);
        SceneManager.CurrentScene.AddGameObject(gameObject);
        return gameObject;
    }

    public override void Destroy()
    {
        foreach (var component in ComponentStore.List)
            ComponentStore.RemoveComponent(component);
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

    public bool TryGetComponent<T>(out T? component) where T : Component
    {
        return ComponentStore.TryGetComponent(out component);
    }
}