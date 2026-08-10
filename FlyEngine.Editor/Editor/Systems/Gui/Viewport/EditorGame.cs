using System.Numerics;
using FlyEngine.Core;
using FlyEngine.Core.Debugging;
using FlyEngine.Core.Input;
using ImGuiNET;
using Silk.NET.Maths;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Editor.Systems;

public class EditorGame : EditorGuiWindow
{
    protected override string Title => "Game";
    
    protected override void BeforeBegin()
    {
        ImGuiNet.SetNextWindowDockID(EditorGui.CenterDockId);
    }

    protected override void OnRender(double deltaTime)
    {
        Editor.IsSceneOpened = false;
        if (Application.Window == null || Editor.Window == null || Application.Window.OpenGl == null) return;
        var windowPos = ImGuiNet.GetCursorScreenPos();
        var regionSize = ImGuiNet.GetContentRegionAvail();
        const float targetAspect = 16f / 9f;
        var windowAspect = regionSize.X / regionSize.Y;
        
        var displaySize = regionSize;
        var offset = Vector2.Zero;
        
        if (windowAspect > targetAspect)
        {
            displaySize.X = regionSize.Y * targetAspect;
            offset.X = (regionSize.X - displaySize.X) * 0.5f;
        }
        else
        {
            displaySize.Y = regionSize.X / targetAspect;
            offset.Y = (regionSize.Y - displaySize.Y) * 0.5f;
        }
        Editor.Window.EditorViewport = new Vector2D<int>((int)displaySize.X, (int)displaySize.Y);
        Application.Window.UpdateAspectRatio();
        var pipeline = Application.Window.OpenGl.RenderPipeline;
        if (pipeline.FinalTexture == 0) return;
        
        ImGuiNet.SetCursorScreenPos(windowPos + offset);

        ImGuiNet.Image((IntPtr)pipeline.FinalTexture, displaySize);
        
        if (ImGuiNet.IsItemHovered() && ImGuiNet.IsMouseClicked(ImGuiMouseButton.Left) && EditorInput.InputReleased)
            EditorInput.UnReleaseInput();
    }
}