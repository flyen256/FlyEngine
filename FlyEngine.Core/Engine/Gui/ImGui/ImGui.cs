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
        style.WindowRounding = 10f;
        style.FrameRounding = 5f;
        style.FramePadding = new Vector2(7f);
        style.WindowPadding = new Vector2(10f);
    }
}