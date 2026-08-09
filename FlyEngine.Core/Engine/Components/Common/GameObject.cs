using System.Diagnostics.CodeAnalysis;
using FlyEngine.Core.SceneManagement;
using MemoryPack;
using Object = FlyEngine.Core.Assets.Object;

namespace FlyEngine.Core.Components;

[MemoryPackable]
public partial class GameObject : Object
{
    [MemoryPackInclude]
    private bool _enabled = true;
    [MemoryPackInclude]
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled.Equals(value)) return;
            _enabled = value;
            if (_enabled)
                for (var i = 0; i < ComponentStore.List.Count; i++)
                    ComponentStore.List[i].OnEnable();
            else
                for (var i = 0; i < ComponentStore.List.Count; i++)
                    ComponentStore.List[i].OnDisable();
        }
    }
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
            if (value.Length == 0)
                return;
            if (Application.Scene != null && Application.Scene.ObjectExistsWithName(value) && !value.Equals(_name))
            {
                var count = Application.Scene.GameObjects.Count(g => g.Name == value);
                _name = value + $"_{count}";
            }
            else
                _name = value;
        }
    }

    [MemoryPackInclude]
    private TransformComponent _transform;

    [MemoryPackIgnore]
    public ref TransformComponent Transform =>
        ref _transform;

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

    [MemoryPackIgnore]
    public Scene Scene;

    [MemoryPackIgnore] public GameObject? ParentGameObject { get; private set; }

    [MemoryPackIgnore] private readonly List<GameObject> _childrenGameObjects = [];
    [MemoryPackIgnore] public IReadOnlyList<GameObject> ChildrenGameObjects => _childrenGameObjects;

    public static GameObject CreateWithLazyReference(string name) => new() { LazyGameObjectName = name };

    [MemoryPackConstructor]
    private GameObject()
    {
        ComponentStore = new ComponentStore
        {
            GameObject = this
        };
    }
    
    private GameObject(Scene scene, string name = "New game object")
    {
        Scene = scene;
        _transform = new TransformComponent(Guid.NewGuid())
        {
            GameObject = this
        };
        if (name.Length == 0)
            name = "New game object";
        _name = name;
        Name = name;
        ComponentStore = new ComponentStore
        {
            GameObject = this
        };
    }

    internal void AddChild(GameObject child)
    {
        _childrenGameObjects.Add(child);
    }

    internal void RemoveChild(GameObject child)
    {
        _childrenGameObjects.Remove(child);
    }

    internal void SetParent(GameObject? parent)
    {
        ParentGameObject = parent;
    }

    public static GameObject Create(string name, Component[]? components = null)
    {
        if (SceneManager.CurrentScene == null)
            throw new InvalidOperationException("No scene loaded");
        var gameObject = new GameObject(SceneManager.CurrentScene, name);
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

    public override void CopyTo(ref Object? copy)
    {
        if (SceneManager.CurrentScene == null)
            throw new InvalidOperationException("No scene loaded");
        var gameObject = new GameObject(SceneManager.CurrentScene, Name);
        var copyTransform = _transform;
        copyTransform.Guid = Guid.NewGuid();
        gameObject._transform = _transform;
        copy = gameObject;
    }

    public override void PasteCopy()
    {
        if (SceneManager.CurrentScene == null)
            throw new InvalidOperationException("No scene loaded");
        
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