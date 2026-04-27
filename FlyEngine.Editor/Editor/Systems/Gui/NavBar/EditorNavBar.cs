using FlyEngine.Core;
using FlyEngine.Core.SceneManagement;
using FlyEngine.Editor.SceneManagement;
using ImGuiNET;

namespace FlyEngine.Editor.Systems.Gui;

public class EditorNavBar : EditorGuiWindow
{
    protected override string Title => "Nav Bar";

    protected override void BeforeBegin()
    {
        ImGui.SetNextWindowDockID(EditorGui.UpDockId);
    }

    protected override async void OnRender(double deltaTime)
    {
        if (ImGui.Button(Application.IsRunning ? "+" : "-"))
        {
            if (Editor.CompileError)
            {
                await Editor.TaskQueue.Enqueue(Editor.CompileScriptsAsync, "Compiling scripts");
                return;
            }
            if (Application.IsRunning)
            {
                Application.Stop();
                await Editor.TaskQueue.Enqueue(SceneSnapshot.RestoreSnapshotAsync, "Restoring scene snapshot");
            }
            else if (SceneManager.CurrentScene != null)
            {
                await Editor.TaskQueue.Enqueue(SceneSnapshot.CreateSnapshotAsync, SceneManager.CurrentScene, "Creating scene snapshot");
                Application.Run();
            }
        }
    }
}