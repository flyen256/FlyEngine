using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using FlyEngine.Core.SceneManagement;
using MemoryPack;

namespace FlyEngine.Core.Components;

[MemoryPackable]
public partial class ComponentStore : IDisposable
{
    [MemoryPackIgnore]
    public GameObject GameObject { get; init; } = null!;

    [MemoryPackIgnore]
    public IReadOnlyList<Component> List => _components;
    
    [MemoryPackInclude]
    private List<Component> _components = [];

    [MemoryPackInclude]
    private List<ComponentDataHolder>? SerializedData
    {
        get
        {
            var holders = new List<ComponentDataHolder>();
            foreach (var comp in List)
            {
                holders.Add(new ComponentDataHolder
                {
                    TypeName = comp.GetType().AssemblyQualifiedName!,
                    JsonPayload = JsonSerializer.Serialize(comp, comp.GetType(), ComponentSerializer.JsonOptions)
                });
            }

            return holders;
        }
        set
        {
            _components.Clear();
            if (value == null) return;

            foreach (var holder in value)
            {
                var type = Type.GetType(holder.TypeName) ?? Application.ScriptsLoader.LoadFromAssemblyName(
                    new AssemblyName(Scripting.Scripting.ScriptsAssemblyName)).GetType(holder.TypeName.Split(",")[0]);
                if (type == null) continue;

                var compObject = JsonSerializer.Deserialize(holder.JsonPayload, type, ComponentSerializer.JsonOptions);
                if (compObject is not Component comp) continue;
                comp.GameObject = GameObject;
                _components.Add(comp);
            }
        }
    }

    [MemoryPackIgnore]
    private bool _initialized;

    public T? GetComponent<T>() where T : class
    {
        var span = CollectionsMarshal.AsSpan(_components);
        for (var i = 0; i < span.Length; i++)
        {
            if (span[i] is T t) return t;
        }
        return null;
    }

    public List<T> GetComponents<T>() where T : class
    {
        var result = new List<T>();
        var span = CollectionsMarshal.AsSpan(_components);
        for (var i = 0; i < span.Length; i++)
        {
            if (span[i] is T t) result.Add(t);
        }
        return result;
    }

    public Component? GetComponent(Type type)
    {
        var span = CollectionsMarshal.AsSpan(_components);
        for (var i = 0; i < span.Length; i++)
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
        SceneManager.CurrentScene?.RegisterComponent(instance, GameObject);
        if (!Application.IsRunning) return instance;
        instance.Initialize();
        instance.OnLoad();
        return instance;
    }
    
    public Component? AddComponent(Type component)
    {
        if (!component.IsSubclassOf(typeof(Component))) return null;
        if (Activator.CreateInstance(component) is not Component instance) return null;
        SceneManager.CurrentScene?.RegisterComponent(instance, GameObject);
        _components.Add(instance);
        instance.GameObject = GameObject;
        if (!Application.IsRunning) return instance;
        instance.Initialize();
        instance.OnLoad();
        return instance;
    }

    public T AddComponent<T>(T component) where T : Component
    {
        SceneManager.CurrentScene?.RegisterComponent(component, GameObject);
        _components.Add(component);
        component.GameObject = GameObject;
        if (!Application.IsRunning) return component;
        component.Initialize();
        component.OnLoad();
        return component;
    }

    public bool TryGetComponent<T>([NotNullWhen(true)] out T? component) where T : Component
    {
        component = GetComponent<T>();
        return component != null;
    }

    public void RemoveComponent(Component component)
    {
        component.OnRemoved();
        if (SceneManager.CurrentScene != null)
            SceneManager.CurrentScene.RemoveComponent(component);
        _components.Remove(component);
    }

    public void InitializeComponents()
    {
        if (_initialized) return;
        foreach (var component in _components)
            component.Initialize();
        _initialized = true;
    }

    public void Dispose()
    {
        foreach (var component in _components)
        {
            component.OnRemoved();
            component.OnDisable();
            component.OnDestroy();
            if (SceneManager.CurrentScene != null)
                SceneManager.CurrentScene.RemoveComponent(component);
        }
        _components.Clear();
    }
}