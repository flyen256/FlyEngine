using FlyEngine.Core.Debugging;
using FlyEngine.Core.SceneManagement;
using MemoryPack;

namespace FlyEngine.Core.Project;

[MemoryPackable]
public partial class ProjectFile : IDisposable
{
    private static ProjectFile _currentProject = new();

    public static ProjectFile CurrentProject
    {
        get => _currentProject;
        set
        {
            if (_currentProject.Equals(value)) return;
            _currentProject.Dispose();
            _currentProject = value;
        }
    }
    
    [MemoryPackInclude] private VideoSettings _videoSettings = VideoSettings.Default;
    [MemoryPackInclude] private CpuSettings _cpuSettings = CpuSettings.Default;
    
    [MemoryPackIgnore] public ref VideoSettings VideoSettings => ref _videoSettings;
    [MemoryPackIgnore] public ref CpuSettings CpuSettings => ref _cpuSettings;
    
    public string Name { get; set; } = "My Awesome Game";
    public string? Path { get; set; }
    public string? LastLoadedScenePath { get; private set; }

    [MemoryPackConstructor]
    private ProjectFile()
    {
        SceneManager.OnLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene? scene)
    {
        LastLoadedScenePath = scene?.Path;
        SaveProject();
    }
    
    public void SaveProject(string path)
    {
        Path = path;
        var bytes = MemoryPackSerializer.Serialize(CurrentProject);
        File.WriteAllBytes(Path, bytes);
    }

    public void SaveProject()
    {
        if (Path == null) return;
        var bytes = MemoryPackSerializer.Serialize(CurrentProject);
        File.WriteAllBytes(Path, bytes);
    }

    public void Dispose()
    {
        SceneManager.OnLoaded -= OnSceneLoaded;
        GC.SuppressFinalize(this);
    }
}