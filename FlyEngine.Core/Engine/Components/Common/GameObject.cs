namespace FlyEngine.Core.Engine.Components.Common;

public class GameObject : Object
{
    public bool Enabled { get; set; } = true;

    private string _name;
    public string Name
    {
        get => _name;
        set
        {
            if (Application.Instance.GameObjects.Exists(g => g.Name == value))
            {
                var count = Application.Instance.GameObjects.Count(g => g.Name == value);
                _name = value + $"_{count}";
            }
            else
                _name = value;
        }
    }
    public readonly Transform Transform;

    public readonly ComponentStore ComponentStore;

    private GameObject(string name = "New game object")
    {
        Name = name;
        Transform = new Transform();
        ComponentStore = new ComponentStore(this);
    }

    public static GameObject Create(string name)
    {
        var gameObject = new GameObject(name);
        Application.Instance.GameObjects.Add(gameObject);
        return gameObject;
    }

    public override void Destroy()
    {
        for (var i = ComponentStore.List.Count - 1; i >= 0; i--)
            ComponentStore.RemoveComponent(ComponentStore.List[i]);
        Application.Instance.GameObjects.Remove(this);
    }

    public T? GetComponent<T>() where T : Component
    {
        return ComponentStore.GetComponent<T>();
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
