using FlyEngine.Core.Extensions;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Core.UI.ImGui;

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
        var currentCamera = Application.Instance.CurrentCamera;
        ImGuiNet.Begin("UI", ref pOpen, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize);
        ImGuiNet.Text($"Fps {(int)(1f / deltaTime)}");
        ImGuiNet.Text($"Current camera position: {currentCamera?.Transform.Position.ToString()}");
        ImGuiNet.Text($"Current camera rotation: {currentCamera?.Transform.Rotation.ToEulerAngles().ToString()}");
        ImGuiNet.Text($"Current camera forward: {currentCamera?.Transform.Forward.ToString()}");
        ImGuiNet.End();
        _controller.Render();
    }
}