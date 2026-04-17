using FlyEngine.Core.Engine.Extensions;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Core.Engine.UI.ImGui;

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
    }

    public static void Render(float deltaTime)
    {
        if (Controller == null) return;
        var pOpen = true;
        var currentCamera = Application.Instance.CurrentCamera;
        ImGuiNet.Begin("UI", ref pOpen, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize);
        ImGuiNet.Text($"Fps {(int)(1f / deltaTime)}");
        ImGuiNet.Text($"Current camera position: {currentCamera?.Transform.Position.ToString()}");
        ImGuiNet.Text($"Current camera rotation: {currentCamera?.Transform.Rotation.ToEulerAngles().ToString()}");
        ImGuiNet.Text($"Current camera forward: {currentCamera?.Transform.Forward.ToString()}");
        ImGuiNet.End();
    }
}