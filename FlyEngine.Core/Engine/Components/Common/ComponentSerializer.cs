using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using FlyEngine.Core.CustomAttributes;
using FlyEngine.Core.Serialization.Json;

namespace FlyEngine.Core.Components;

public static class ComponentSerializer
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true,
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { CustomAttributeModifier }
        },
        Converters =
        {
            new AssetReferenceConverterFactory(),
            new AssetArrayConverterFactory(),
            new ComponentRefConverterFactory()
        },
    };

    private static void CustomAttributeModifier(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object) return;

        var fields = typeInfo.Type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var field in fields)
        {
            if (field.GetCustomAttribute<SerializeAttribute>() == null) continue;
            var exists = typeInfo.Properties.Any(prop => prop.Name == field.Name);

            if (exists) continue;
            var jsonProperty = typeInfo.CreateJsonPropertyInfo(field.FieldType, field.Name);
            jsonProperty.Get = field.GetValue;
            jsonProperty.Set = field.SetValue;
                
            typeInfo.Properties.Add(jsonProperty);
        }
    }
}