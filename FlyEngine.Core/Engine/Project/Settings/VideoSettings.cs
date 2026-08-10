using MemoryPack;

namespace FlyEngine.Core.Project;

[MemoryPackable]
public partial struct VideoSettings
{
    public int FramesPerSecond { get; set; }
    public bool VSync { get; set; }
    
    public static VideoSettings Default => new()
    {
        FramesPerSecond = 0,
        VSync = true
    };
}