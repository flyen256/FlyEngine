using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlyEngine.Core.CustomAttributes;
using FlyEngine.Editor.Systems;
using MemoryPack;
using Object = FlyEngine.Core.Assets.Object;

namespace FlyEngine.Core.Components;

[MemoryPackable]
public partial class Component : Object
{
    [MemoryPackIgnore]
    [JsonInclude]
    public Guid Guid { get; set; } = Guid.NewGuid();
    
    [MemoryPackIgnore]
    [JsonIgnore]
    public Guid LazyGuid { get; set; } = Guid.Empty;

    [HideInInspector]
    [MemoryPackIgnore]
    [JsonInclude]
    public bool Enabled { get; set; } = true;

    [HideInInspector]
    [MemoryPackIgnore]
    [JsonIgnore]
    public virtual bool AllowMultipleInstances => true;
    [MemoryPackIgnore]
    [JsonIgnore]
    [HideInInspector]
    public GameObject GameObject = null!;

    [MemoryPackIgnore]
    [JsonIgnore]
    [HideInInspector]
    public ref TransformComponent Transform => ref GameObject.Transform;
    [MemoryPackIgnore]
    [JsonIgnore]
    [HideInInspector]
    public bool Initialized { get; private set; }
    [MemoryPackIgnore]
    [JsonIgnore]
    [HideInInspector]
    public int SceneIndex { get; internal set; } = -1;
    
    [MemoryPackIgnore]
    [JsonIgnore]
    [HideInInspector]
    public ComponentDataHolder? CopyDataHolder { get; set; }

    protected virtual void OnInitialize() { }

    public virtual void OnLoad() { }
    public virtual void OnEnable() { }
    public virtual void OnDisable() { }
    public virtual void OnDestroy() { }
    protected internal virtual void OnRemoved() { }

    public void Initialize()
    {
        if (Initialized) return;
        Initialized = true;
        OnInitialize();
        OnLoad();
        if (GameObject.Enabled)
            OnEnable();
    }

    public override void Destroy()
    {
        GameObject.ComponentStore.RemoveComponent(this);
    }

    public override void CopyTo(ref Object? copy)
    {
        var component = new Component
        {
            CopyDataHolder = new ComponentDataHolder
            {
                TypeName = GetType().AssemblyQualifiedName!,
                JsonPayload = JsonSerializer.Serialize(this, GetType(), ComponentSerializer.JsonOptions)
            }
        };
        copy = component;
    }

    public override void PasteCopy()
    {
        if (!CopyDataHolder.HasValue || Selection.SelectedObject is not GameObject gameObject) return;
        var type = Type.GetType(CopyDataHolder.Value.TypeName) ?? Application.ScriptsLoader.LoadFromAssemblyName(
            new AssemblyName(Scripting.Scripting.ScriptsAssemblyName)).GetType(CopyDataHolder.Value.TypeName.Split(",")[0]);
        if (type == null) return;

        var compObject = JsonSerializer.Deserialize(CopyDataHolder.Value.JsonPayload, type, ComponentSerializer.JsonOptions);
        if (compObject is not Component comp) return;
        comp.GameObject = GameObject;
        gameObject.AddComponent(comp);
    }

    public void PasteValues(Component component)
    {
        if (!CopyDataHolder.HasValue) return;
    
        var type = component.GetType();

        var sourceType = Type.GetType(CopyDataHolder.Value.TypeName) ?? 
                         Application.ScriptsLoader.LoadFromAssemblyName(
                                 new AssemblyName(Scripting.Scripting.ScriptsAssemblyName))
                             .GetType(CopyDataHolder.Value.TypeName.Split(",")[0]);
    
        if (sourceType == null || !sourceType.IsAssignableFrom(type)) 
            return;

        var sourceComponent = JsonSerializer.Deserialize(
            CopyDataHolder.Value.JsonPayload, 
            type, 
            ComponentSerializer.JsonOptions);
    
        if (sourceComponent == null) 
            return;

        var variables = GetComponentVariables(component);
    
        foreach (var variable in variables)
        {
            switch (variable.Name)
            {
                case null:
                case nameof(Guid) or 
                    nameof(LazyGuid) or 
                    nameof(GameObject) or 
                    nameof(Transform) or 
                    nameof(Initialized) or 
                    nameof(SceneIndex) or 
                    nameof(CopyDataHolder) or
                    nameof(Enabled):
                case "AllowMultipleInstances":
                    continue;
            }

            if (variable.GetCustomAttribute(typeof(JsonIgnoreAttribute), true) != null ||
                variable.GetCustomAttribute(typeof(MemoryPackIgnoreAttribute), true) != null)
                continue;

            var value = variable.GetValue(sourceComponent);
            variable.SetValue(component, value!);
        }
    }
    
    public static VariableInfo[] GetComponentVariables(Component component)
    {
        var type = component.GetType();

        var typeHierarchy = new List<Type>();
        var currentType = type;
        while (currentType != null && currentType != typeof(object))
        {
            typeHierarchy.Insert(0, currentType);
            currentType = currentType.BaseType;
        }

        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => f.GetSetMethod(false) != null);

        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => f.IsPublic || f.GetCustomAttribute<ShowInInspectorAttribute>() != null);

        var orderedVariables = fields.Cast<MemberInfo>()
            .Concat(properties)
            .OrderBy(m => typeHierarchy.IndexOf(m.DeclaringType!))
            .ThenBy(m => 
            {
                if (m is not PropertyInfo prop) return m.MetadataToken;
                var backingField = prop.DeclaringType?.GetField($"<{prop.Name}>k__BackingField", 
                    BindingFlags.Instance | BindingFlags.NonPublic);
                return backingField != null ? backingField.MetadataToken : m.MetadataToken;
            })
            .Select(v => new VariableInfo(v))
            .ToArray();

        return orderedVariables;
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

    public bool TryGetComponent<T>([NotNullWhen(true)] out T? component) where T : Component
    {
        return GameObject.TryGetComponent(out component);
    }

    public static Component CreateWithLazyGuid(Guid guid, Type componentType)
    {
        var instance = (Component)Activator.CreateInstance(componentType)!;
        instance.LazyGuid = guid;
        return instance;
    }
}