using System.Reflection;
using System.Text.Json;
using FlyEngine.Editor.Storage;

namespace FlyEngine.Editor.Localization;

public static class EditorLocalization
{
    public const EditorLanguage FallbackLanguage = EditorLanguage.English;

    private static readonly Dictionary<string, string> Translation = new();

    static EditorLocalization()
    {
        UpdateTranslation();
    }

    public static void UpdateTranslation()
    {
        var newDictionary = GetLanguageDictionary();

        foreach (var (key, value) in newDictionary)
            Translation[key] = value;
    }

    private static Dictionary<string, string> GetLanguageDictionary()
    {
        var cultureCode = EditorStorage.Preferences.Language.ToString().ToLower()[..2];

        var resourceName = $"FlyEngine.Editor.Assets.Locales.{cultureCode}.json";

        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        
        if (stream == null)
            return new Dictionary<string, string>();

        using var reader = new StreamReader(stream);
        var jsonContent = reader.ReadToEnd();

        var resultContent = new Dictionary<string, string>();
        using var doc = JsonDocument.Parse(jsonContent);

        FlattenJson(doc.RootElement, string.Empty, resultContent);

        return resultContent;
    }

    private static void FlattenJson(JsonElement element, string prefix, Dictionary<string, string> result)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var newPrefix = string.IsNullOrEmpty(prefix) 
                        ? property.Name 
                        : $"{prefix}.{property.Name}";
                    
                    FlattenJson(property.Value, newPrefix, result);
                }
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    FlattenJson(item, $"{prefix}[{index}]", result);
                    index++;
                }
                break;

            default:
                result[prefix] = element.ToString();
                break;
        }
    }
    
    public static string GetString(string key) =>
        Translation.TryGetValue(key, out var value) ? value : key;
}