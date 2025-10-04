namespace Flyeng;

public class Components
{
    private GameObject _gameObject;
    public GameObject GameObject => _gameObject;
    protected List<Component> _components = new();
    public IReadOnlyList<Component> List => _components;

    public Components(GameObject gameObject)
    {
        _gameObject = gameObject;
    }

    public T? GetComponent<T>() where T : Component
    {
        T? component = _components.Find((c) => c.GetType() == typeof(T)) as T;
        T? componentAsT = _components.Find((c) => c as T != null) as T ?? null;
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
            _components.Add(instance);
            instance.GameObject = GameObject;
        }
        return GetComponent<T>();
    }

    public T? AddComponent<T>(T component) where T : Component
    {
        if(component != null)
        {
            _components.Add(component);
            component.GameObject = GameObject;
        }
        return GetComponent<T>();
    }
}
