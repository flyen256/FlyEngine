using System.Text.Json;
using System.Text.Json.Serialization;
using FlyEngine.Core.Assets;
using Microsoft.Extensions.Logging;

namespace FlyEngine.Core.Serialization;

public class AssetArrayReferenceConverter<T> : JsonConverter<T> where T : IEnumerable<Asset>
{
    private readonly ILogger _logger =
        new Logger<AssetArrayReferenceConverter<T>>(LoggerFactory.Create(b => b.AddConsole()));
    
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException();

        List<Asset> list = [];
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            var guid = reader.GetGuid();
            var asset = AssetsManager.GetAsset(guid);
            if (asset == null)
            {
                _logger.LogWarning("Asset with guid {guid} not found", guid);
                continue;
            }
            list.Add(asset);
        }
        return (T)list.AsEnumerable();
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value)
            writer.WriteStringValue(item.Guid);
        writer.WriteEndArray();
    }
}