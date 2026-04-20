using FlyEngine.Core.Engine.SceneManagement;
using MemoryPack;

namespace FlyEngine.Core.Engine.Components.Common;

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
    public ComponentStore ComponentStore;

    private GameObject(string name = "New game object")
    {
        _name = name;
        Name = name;
        ComponentStore = new ComponentStore(this);
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

    public T AddComponent<T>(T component) where T : Component
    {
        return ComponentStore.AddComponent(component);
    }

    public bool TryGetComponent<T>(out T? component) where T : Component
    {
        return ComponentStore.TryGetComponent(out component);
    }
}