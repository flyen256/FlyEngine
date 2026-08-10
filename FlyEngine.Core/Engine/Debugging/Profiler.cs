using System.Diagnostics;

namespace FlyEngine.Core.Debugging;

public static class Profiler
{
    public static bool Enabled { get; set; } = false;
    
    public static int UpdatesPerSecond { get; private set; }
    public static int FramesPerSecond { get; private set; }
    public static int CpuLatencyMilliseconds { get; private set; }
    public static double GpuLatencyMilliseconds { get; set; }
    
    public static readonly Stopwatch Stopwatch = new();

    public static void UpdateMetrics(int updatesPerSecond)
    {
        UpdatesPerSecond = updatesPerSecond;
    }
    
    public static void RenderMetrics(float deltaTime)
    {
        FramesPerSecond = (int)System.Math.Floor(1f / deltaTime);
        CpuLatencyMilliseconds = Stopwatch.Elapsed.Milliseconds;
    }
}