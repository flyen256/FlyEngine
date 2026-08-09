using System.Text.Json;
using System.Text.Json.Serialization;
using FlyEngine.Core.Serialization.Json;

namespace FlyEngine.Core.Components;

public static class ComponentSerializer
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true,
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Converters =
        {
            new AssetReferenceConverterFactory(),
            new AssetArrayConverterFactory(),
            new ComponentRefConverterFactory()
        },
    };
}