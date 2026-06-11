using System.Text.Json;
using System.Text.Json.Serialization;
using FlyEngine.Core.Components.Common;

namespace FlyEngine.Core.Serialization;

public class ComponentRefConverter<T> : JsonConverter<ComponentRef<T>> where T : Component
{
    public override ComponentRef<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        
        var guid = reader.GetGuid();
        return new ComponentRef<T>(guid);
    }

    public override void Write(Utf8JsonWriter writer, ComponentRef<T>? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }
        writer.WriteStringValue(value.Guid);
    }
}