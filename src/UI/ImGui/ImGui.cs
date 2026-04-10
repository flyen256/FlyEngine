using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.UI.ImGui;

public static class ImGui
{
    public static bool Initialized => _controller != null;
    private static ImGuiController? _controller;

    public static void Initialize(GL gl, IWindow window, IInputContext inputContext)
    {
        _controller = new ImGuiController(
            gl,
            window,
            inputContext
        );
    }

    public static void Render(float deltaTime)
    {
        if (_controller == null) return;
        _controller.Update(deltaTime);
        var pOpen = true;
        ImGuiNet.Begin("UI", ref pOpen, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove);
        ImGuiNet.Button("Test");
        ImGuiNet.End();
        _controller.Render();
    }
}