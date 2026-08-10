using MemoryPack;

namespace FlyEngine.Core.Project;

[MemoryPackable]
public partial struct CpuSettings
{
    public int UpdatesPerSecond { get; set; }

    public static CpuSettings Default => new()
    {
        UpdatesPerSecond = 60
    };
}