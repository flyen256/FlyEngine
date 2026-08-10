using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace FlyEngine.Core.Gui;

public static class ImGui
{
    public static bool Initialized => Controller != null;
    public static ImGuiController? Controller { get; private set; }
    
    public static void Initialize(GL gl, IWindow window, IInputContext inputContext, Vector2D<int> minSize)
    {
        Controller = new ImGuiController(
            gl,
            window,
            inputContext,
            minSize
        );
        var style = ImGuiNET.ImGui.GetStyle();
        var padding = new Vector2(8f, 4f);
        style.FramePadding = padding;
        style.WindowPadding = padding;
        style.FrameRounding = 4f;
        style.WindowRounding = 8f;
        style.TabRounding = 4f;
        style.ChildRounding = 8f;
    }
}