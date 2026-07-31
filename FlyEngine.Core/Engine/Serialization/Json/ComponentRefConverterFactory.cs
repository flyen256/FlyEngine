using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlyEngine.Core.Serialization.Json;

public class ComponentRefConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType && 
               typeToConvert.GetGenericTypeDefinition() == typeof(ComponentRef<>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return (JsonConverter?)Activator.CreateInstance(
            typeof(ComponentRefConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;
    }
}