using FlyEngine.Core.Engine;
using Silk.NET.Maths;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Editor.Systems.Gui;

public class EditorScene : EditorGuiWindow
{
    protected override string Title => "Scene";

    protected override void BeforeBegin()
    {
        ImGuiNet.SetNextWindowDockID(EditorGui.CenterDockId);
    }

    protected override void OnRender(double deltaTime)
    {
        Editor.IsSceneOpened = true;
        if (Application.Window == null || Application.Window.OpenGl == null) return;
        var regionSize = ImGuiNet.GetContentRegionAvail();
        Editor.ViewportSize = new Vector2D<int>((int)regionSize.X, (int)regionSize.Y);
        var pipeline = Application.Window.OpenGl.RenderPipeline;
        if (pipeline.FinalTexture == 0) return;

        ImGuiNet.Image((IntPtr)Application.Window.OpenGl.RenderPipeline.FinalTexture, regionSize);
    }
}