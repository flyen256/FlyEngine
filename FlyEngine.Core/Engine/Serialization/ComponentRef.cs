using System.Text.Json.Serialization;
using FlyEngine.Core.Components.Common;
using FlyEngine.Core.SceneManagement;

namespace FlyEngine.Core.Serialization;

[JsonConverter(typeof(ComponentRefConverterFactory))]
public class ComponentRef<T> where T : Component
{
    private T? _cachedComponent;
    
    public Guid Guid { get; set; } = Guid.Empty;

    [JsonIgnore]
    public T? Value
    {
        get
        {
            if (_cachedComponent != null || Guid == Guid.Empty) return _cachedComponent;
            var component = SceneManager.CurrentScene?.GetComponentByGuid(Guid);
            if (component is T t)
                _cachedComponent = t;
            return _cachedComponent;
        }
        set
        {
            _cachedComponent = value;
            Guid = value?.Guid ?? Guid.Empty;
        }
    }

    public ComponentRef() { }

    public ComponentRef(Guid guid)
    {
        Guid = guid;
    }

    public ComponentRef(T? component)
    {
        Value = component;
    }

    public static implicit operator T?(ComponentRef<T> @ref) => @ref?.Value;
    public static implicit operator ComponentRef<T>(T? component) => new(component);
}