using System.Text.Json.Serialization;
using FlyEngine.Core.CustomAttributes;
using MemoryPack;

namespace FlyEngine.Core.Components.Common;

[MemoryPackable]
public partial class Component : Object
{
    [HideInInspector]
    [MemoryPackInclude]
    private bool _enabled = true;

    [HideInInspector]
    [MemoryPackIgnore]
    [JsonIgnore]
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (value == _enabled) return;
            _enabled = value;
            if (_enabled && Application.IsRunning)
                OnEnable();
            else if (Application.IsRunning)
                OnDisable();
        }
    }
    [HideInInspector]
    [MemoryPackIgnore]
    [JsonIgnore]
    public virtual bool AllowMultipleInstances => true;
    [MemoryPackIgnore]
    [JsonIgnore]
    [HideInInspector]
    public GameObject GameObject;
    [MemoryPackIgnore]
    [JsonIgnore]
    [HideInInspector]
    public Transform Transform => GameObject.Transform;
    [MemoryPackIgnore]
    [JsonIgnore]
    [HideInInspector]
    public bool Initialized { get; private set; }
    [MemoryPackIgnore]
    [JsonIgnore]
    [HideInInspector]
    public int SceneIndex { get; internal set; } = -1;

    protected virtual void OnInitialize() { }

    protected virtual void OnEnable() { }
    public virtual void OnDisable() { }
    protected internal virtual void OnRemoved() { }

    public void Initialize()
    {
        if (Initialized) return;
        Initialized = true;
        OnInitialize();
        if (Enabled)
            OnEnable();
    }

    public override void Destroy()
    {
        GameObject.ComponentStore.RemoveComponent(this);
    }

    public static T CreateGameObject<T>(string? name = null) where T : Component
    {
        var instance = Activator.CreateInstance<T>();
        GameObject.Create(name ?? typeof(T).Name, [instance]);
        return instance;
    }

    public bool IsActive()
    {
        return Enabled && GameObject is { Enabled: true, IsDestroyed: false };
    }

    public T? GetComponent<T>() where T : class
    {
        return GameObject.GetComponent<T>();
    }

    public List<T> GetComponents<T>() where T : class
    {
        return GameObject.GetComponents<T>();
    }

    public Component? GetComponent(Type type)
    {
        return GameObject.GetComponent(type);
    }

    public T AddComponent<T>() where T : Component
    {
        return GameObject.AddComponent<T>();
    }

    public T AddComponent<T>(T component) where T : Component
    {
        return GameObject.AddComponent(component);
    }

    public bool TryGetComponent<T>(out T? component) where T : Component
    {
        return GameObject.TryGetComponent(out component);
    }
}