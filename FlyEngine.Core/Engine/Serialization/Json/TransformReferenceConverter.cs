using System.Text.Json;
using System.Text.Json.Serialization;
using FlyEngine.Core.Components.Common;
using FlyEngine.Core.SceneManagement;

namespace FlyEngine.Core.Serialization;

public class TransformReferenceConverter<T> : JsonConverter<T> where T : Transform
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        var guid = reader.GetGuid();
        return (T?)Transform.CreateWithLazyGuid(guid);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Guid);
    }
}