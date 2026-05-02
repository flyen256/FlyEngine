using System.Text.Json;
using System.Text.Json.Serialization;
using FlyEngine.Core.Assets;

namespace FlyEngine.Core.Serialization;

public class AssetReferenceConverter<T> : JsonConverter<T> where T : Asset
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        var guid = reader.GetGuid();
        return AssetsManager.GetAsset<T>(guid);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Guid);
    }
}