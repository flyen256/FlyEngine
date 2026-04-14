namespace FlyEngine.Core.Components.Common;

public class Component : Object
{
    public bool Enabled { get; set; } = true;
    public virtual bool AllowMultipleInstances => true;
    public required GameObject GameObject;
    public Transform Transform => GameObject.Transform;

    protected virtual void OnInitialize() { }

    protected internal virtual void OnRemovingFromStore() { }

    public void Initialize() => OnInitialize();
    
    public override void Destroy()
    {
        GameObject.ComponentStore.RemoveComponent(this);
    }

    public static T CreateGameObject<T>(string? name = null) where T : Component
    {
        var gameObject = GameObject.Create(name ?? typeof(T).Name);
        var component = gameObject.AddComponent<T>();
        return component;
    }

    public bool IsActive()
    {
        return Enabled && GameObject.Enabled;
    }
    
    public T? GetComponent<T>() where T : Component
    {
        return GameObject.GetComponent<T>();
    }

    public T AddComponent<T>() where T : Component
    {
        return GameObject.AddComponent<T>();
    }

    public T AddComponent<T>(T component) where T : Component
    {
        return GameObject.AddComponent(component);
    }

    public bool TryGetComponent<T>(out T component) where T : Component
    {
        return GameObject.TryGetComponent(out component);
    }
}
