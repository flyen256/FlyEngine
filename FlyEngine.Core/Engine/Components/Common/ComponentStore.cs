using System.Runtime.InteropServices;

namespace FlyEngine.Core.Engine.Components.Common;

public class ComponentStore(GameObject gameObject)
{
    public GameObject GameObject { get; } = gameObject;

    public IReadOnlyList<Component> List => _components;

    private readonly List<Component> _components = [];

    private bool _initialized;

    public T? GetComponent<T>() where T : class
    {
        var span = CollectionsMarshal.AsSpan(_components);
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i] is T t) return t;
        }
        return null;
    }

    public List<T> GetComponents<T>() where T : class
    {
        var result = new List<T>();
        var span = CollectionsMarshal.AsSpan(_components);
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i] is T t) result.Add(t);
        }
        return result;
    }

    public Component? GetComponent(Type type)
    {
        var span = CollectionsMarshal.AsSpan(_components);
        for (int i = 0; i < span.Length; i++)
        {
            var comp = span[i];
            if (type.IsInstanceOfType(comp)) return comp;
        }
        return null;
    }

    public T AddComponent<T>() where T : Component
    {
        var instance = Activator.CreateInstance<T>();
        _components.Add(instance);
        instance.GameObject = GameObject;
        if (!Application.IsRunning) return instance;
        instance.Initialize();
        if (instance is Behaviour behaviour)
            behaviour.OnLoad();
        return instance;
    }

    public T AddComponent<T>(T component) where T : Component
    {
        _components.Add(component);
        component.GameObject = GameObject;
        if (!Application.IsRunning) return component;
        component.Initialize();
        if (component is Behaviour behaviour)
            behaviour.OnLoad();
        return component;
    }

    public bool TryGetComponent<T>(out T? component) where T : Component
    {
        component = GetComponent<T>();
        return component != null;
    }

    public void RemoveComponent(Component component)
    {
        component.OnRemoved();
        component.OnDisable();
        _components.Remove(component);
    }

    public void InitializeComponents()
    {
        if (_initialized) return;
        foreach (var component in _components)
            component.Initialize();
        _initialized = true;
    }
}