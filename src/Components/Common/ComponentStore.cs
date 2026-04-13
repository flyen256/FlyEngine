namespace FlyEngine.Components.Common;

public class ComponentStore(GameObject gameObject)
{
    public GameObject GameObject { get; } = gameObject;

    public IReadOnlyList<Component> List => _components;
    
    private readonly List<Component> _components = [];

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
        instance.Initialize();
        return instance;
    }

    public T AddComponent<T>(T component) where T : Component
    {
        _components.Add(component);
        component.GameObject = GameObject;
        component.Initialize();
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
}
