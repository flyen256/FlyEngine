using FlyEngine.Core.Debugging;
using ImGuiNET;

namespace FlyEngine.Editor.Systems;

public class EditorProfiler : EditorGuiWindow
{
    protected override string Title => "Profiler";

    protected override void BeforeBegin()
    {
        ImGui.SetNextWindowDockID(EditorGui.BottomDockId);
    }

    protected override void OnRender(double deltaTime)
    {
        if (Core.Gui.ImGui.Controller == null) return;
        var enabled = Profiler.Enabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
            Profiler.Enabled = enabled;
        ImGui.Text($"Updates per second(UPS): {Profiler.UpdatesPerSecond}");
        ImGui.Text($"Frames per second(FPS): {Profiler.FramesPerSecond}");
        ImGui.Text($"Cpu latency: {Profiler.CpuLatencyMilliseconds:F4} ms");
        ImGui.Text($"Gpu latency: {Profiler.GpuLatencyMilliseconds:F4} ms");
    }
}