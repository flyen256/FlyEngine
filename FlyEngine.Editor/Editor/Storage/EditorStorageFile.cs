using FlyEngine.Core.Debugging;
using MemoryPack;

namespace FlyEngine.Editor.Storage;

[MemoryPackable]
public partial class EditorStorageFile<T> where T : EditorStorageFile<T>, new()
{
    protected static DirectoryInfo RootDir => new(AppContext.BaseDirectory);

    public static FileInfo File => new(Path.Combine(RootDir.FullName, $"{typeof(T).Name.ToLower()}.data"));
    
    public async Task Save()
    {
        try
        {
            await using var snapshotFile = File.Create();
            await MemoryPackSerializer.SerializeAsync(snapshotFile, (T)this);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Storage] Save error: {ex.Message}");
        }
    }

    public static T Load()
    {
        if (!File.Exists) return new T();
        try
        {
            var bytes = System.IO.File.ReadAllBytes(File.FullName);
            var value = MemoryPackSerializer.Deserialize<T>(bytes);
            
            return value ?? new T();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Storage] Load error: {ex.Message}");
        }
        return new T();
    }
}