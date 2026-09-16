using MemoryPack;
using FlyEngine.Editor.Localization;

namespace FlyEngine.Editor.Storage;

[MemoryPackable]
public partial class EditorPreferences : EditorStorageFile<EditorPreferences>
{
    [MemoryPackInclude]
    private EditorLanguage _language = EditorLocalization.FallbackLanguage;
    [MemoryPackIgnore]
    public EditorLanguage Language
    {
        get => _language;
        set
        {
            if (_language == value) return;
            _language = value;
            EditorLocalization.UpdateTranslation();
            
            _ = Save();
        }
    }
}