using System.Text.Json;
using System.Text.Json.Serialization;
using FlyEngine.Core.Assets;

namespace FlyEngine.Core.Serialization.Json;

public class AssetArrayConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(IEnumerable<Asset>).IsAssignableFrom(typeToConvert);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return (JsonConverter)Activator.CreateInstance(
            typeof(AssetArrayReferenceConverter<>).MakeGenericType(typeToConvert))!;
    }
}