using System.Text.Json;
using System.Text.Json.Serialization;
using Transform = FlyEngine.Core.Components.Transform;

namespace FlyEngine.Core.Serialization.Json;

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