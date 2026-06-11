using System.Text.Json;
using System.Text.Json.Serialization;
using FlyEngine.Core.Components.Common;

namespace FlyEngine.Core.Serialization;

public class TransformReferenceConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(Transform).IsAssignableFrom(typeToConvert);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return (JsonConverter)Activator.CreateInstance(
            typeof(TransformReferenceConverter<>).MakeGenericType(typeToConvert))!;
    }
}