namespace FlyEngine.Components.Common;

public class ComponentStore
{
    private readonly GameObject _gameObject;
    public GameObject GameObject => _gameObject;
    protected readonly List<Component> Components = new();
    public IReadOnlyList<Component> List => Components;

    public ComponentStore(GameObject gameObject)
    {
        _gameObject = gameObject;
    }

    public T? GetComponent<T>() where T : Component
    {
        var component = Components.Find((c) => c.GetType() == typeof(T)) as T;
        var componentAsT = Components.Find((c) => c as T != null) as T ?? null;
        if (component != null)
            return component;
        if(componentAsT != null)
            return componentAsT;
        return null;
    }

    public T? AddComponent<T>() where T : Component
    {
        var instance = Activator.CreateInstance(typeof(T)) as Component;
        if (instance != null)
        {
            Components.Add(instance);
            instance.GameObject = GameObject;
        }
        return GetComponent<T>();
    }

    public T? AddComponent<T>(T component) where T : Component
    {
        if(component != null)
        {
            Components.Add(component);
            component.GameObject = GameObject;
        }
        return GetComponent<T>();
    }
}
