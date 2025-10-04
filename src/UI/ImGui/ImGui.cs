using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using ImGuiNet = ImGuiNET.ImGui;

namespace Flyeng;

public static class ImGui
{
    public static bool Initialized => _controller != null;
    private static ImGuiController? _controller;

    public static void Initialize(GL gl, IWindow window, IInputContext inputContext)
    {
        _controller = new(
            gl,
            window,
            inputContext
        );
    }

    public static void Render(float deltaTime)
    {
        if (_controller == null) return;
        _controller.Update((float)deltaTime);
        bool p_open = true;
        ImGuiNet.Begin("UI", ref p_open, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove);
        ImGuiNet.Button("Test");
        ImGuiNet.End();
        _controller.Render();
    }
}