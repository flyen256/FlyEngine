using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlyEngine.Core.SceneManagement;
using FlyEngine.Core.Serialization;
using MemoryPack;

namespace FlyEngine.Core.Components.Common;

[MemoryPackable]
public partial class ComponentStore
{
    [MemoryPackIgnore]
    public GameObject GameObject { get; init; }

    [MemoryPackIgnore]
    public IReadOnlyList<Component> List => _components;
    
    [MemoryPackIgnore]
    private readonly List<Component> _components = [];
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true,
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Converters = { new AssetConverterFactory() }
    };

    [MemoryPackInclude]
    private List<ComponentDataHolder> SerializedData
    {
        get
        {
            var holders = new List<ComponentDataHolder>();
            foreach (var comp in List)
            {
                holders.Add(new ComponentDataHolder
                {
                    TypeName = comp.GetType().AssemblyQualifiedName,
                    JsonPayload = JsonSerializer.Serialize(comp, comp.GetType(), JsonOptions)
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
                var type = Type.GetType(holder.TypeName);
                if (Application.Window is { IsEditor: true } && type == null)
                    type = Application.Window.EditorScriptLoader.LoadFromAssemblyName(
                        new AssemblyName(Application.ScriptsAssemblyName)).GetType(holder.TypeName.Split(",")[0]);
                if (type == null) continue;

                var comp = (Component)JsonSerializer.Deserialize(holder.JsonPayload, type, JsonOptions);
                if (comp != null)
                {
                    comp.GameObject = GameObject;
                    _components.Add(comp);
                }
            }
        }
    }

    [MemoryPackIgnore]
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
        SceneManager.CurrentScene?.RegisterComponent(instance, GameObject);
        if (!Application.IsRunning) return instance;
        instance.Initialize();
        if (instance is Behaviour behaviour)
            behaviour.OnLoad();
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
        if (instance is Behaviour behaviour)
            behaviour.OnLoad();
        return instance;
    }

    public T AddComponent<T>(T component) where T : Component
    {
        SceneManager.CurrentScene?.RegisterComponent(component, GameObject);
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
}