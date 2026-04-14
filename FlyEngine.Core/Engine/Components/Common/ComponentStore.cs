namespace FlyEngine.Core.Components.Common;

public class ComponentStore(GameObject gameObject)
{
    public GameObject GameObject { get; } = gameObject;

    public IReadOnlyList<Component> List => _components;
    
    private readonly List<Component> _components = [];

    private bool _initialized;

    public T? GetComponent<T>() where T : Component
    {
        var component = _components.Find((c) => c.GetType() == typeof(T)) as T;
        var componentAsT = _components.Find((c) => c is T) as T ?? null;
        return component ?? componentAsT;
    }

    public T AddComponent<T>() where T : Component
    {
        var instance = Activator.CreateInstance<T>();
        _components.Add(instance);
        instance.GameObject = GameObject;
        return instance;
    }

    public T AddComponent<T>(T component) where T : Component
    {
        _components.Add(component);
        component.GameObject = GameObject;
        return component;
    }

    public bool TryGetComponent<T>(out T component) where T : Component
    {
#pragma warning disable CS8601 // Possible null reference assignment.
        component = GetComponent<T>();
#pragma warning restore CS8601 // Possible null reference assignment.
        return component != null;
    }

    public void RemoveComponent(Component component)
    {
        component.OnRemovingFromStore();
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
