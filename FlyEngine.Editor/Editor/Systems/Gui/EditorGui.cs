using System.Numerics;
using ImGuiNET;
using Microsoft.Extensions.Logging;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Editor.Systems.Gui;

public class EditorGui : EditorSystem
{
    private readonly ILogger _logger = new Logger<EditorGui>(LoggerFactory.Create(builder => builder.AddConsole()));

    private readonly List<EditorGuiWindow> _windows = [];

    private bool _initialized;
    
    public static uint LeftDockId { get; private set; }
    public static uint RightDockId { get; private set; }
    public static uint CenterDockId { get; private set; }
    public static uint BottomDockId { get; private set; }
    public static uint UpDockId { get; private set; }

    public static uint DockSpaceId { get; private set; }

    public EditorGui()
    {
        AddWindow<EditorGame>();
        AddWindow<EditorScene>();
        AddWindow<EditorFileBrowser>();
        AddWindow<EditorHierarchy>();
        AddWindow<EditorInspector>();
        AddWindow<EditorConsoleGui>();
        AddWindow<EditorNavBar>();
    }

    public override void OnUpdate(double deltaTime)
    {
        foreach (var window in _windows)
            window.OnUpdate(deltaTime);
    }

    public override void OnRender(double deltaTime)
    {
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
        DockSpaceId = ImGuiNet.DockSpaceOverViewport(0, ImGuiNet.GetMainViewport(),
            ImGuiDockNodeFlags.PassthruCentralNode |
            ImGuiDockNodeFlags.NoDockingOverCentralNode |
            ImGuiDockNodeFlags.NoUndocking);
        if (!_initialized)
        {
            SetupDefaultLayout(DockSpaceId);
            _initialized = true;
        }
        RenderMainMenuBar();
        foreach (var window in _windows)
            window.Render(deltaTime);
        RenderTaskModal();
    }
    
    private void RenderTaskModal()
    {
        if (Editor.IsRunningTask)
            ImGuiNet.OpenPopup("TaskOverlay");

        var center = ImGuiNet.GetMainViewport().GetCenter();
        ImGuiNet.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        if (ImGuiNet.BeginPopupModal("TaskOverlay", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove))
        {
            ImGuiNet.Spacing();
            ImGuiNet.Text($"   {Editor.TaskQueue.CurrentItem?.Message ?? "Running unnamed task"}...   ");
            ImGuiNet.Spacing();
            
            var t = (float)ImGuiNet.GetTime();
            var dots = new string('.', (int)(t * 2) % 4);
            ImGuiNet.Text($"Please wait{dots}");

            if (!Editor.IsRunningTask)
                ImGuiNet.CloseCurrentPopup();

            ImGuiNet.EndPopup();
        }
    }

    private void RenderMainMenuBar()
    {
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("New", "Ctrl+N")) { /* Действие */ }
                if (ImGui.MenuItem("Open", "Ctrl+O")) { /* Действие */ }
                ImGui.Separator();
                if (ImGui.MenuItem("Exit")) { /* Закрыть приложение */ }
                ImGui.EndMenu();
            }
    
            ImGui.EndMainMenuBar();
        }
    }
    
    private unsafe void SetupDefaultLayout(uint dockspaceId)
    {
        ImGuiDockingInternal.igDockBuilderRemoveNode(dockspaceId);
        ImGuiDockingInternal.igDockBuilderAddNode(dockspaceId, (int)ImGuiDockNodeFlags.None);

        uint leftId, remainingId, remainingId2, upId, rightId, centerPartId;

        ImGuiDockingInternal.igDockBuilderSplitNode(dockspaceId, (int)ImGuiDir.Up, 0.1f, &upId, &remainingId2);
        ImGuiDockingInternal.igDockBuilderSplitNode(remainingId2, (int)ImGuiDir.Left, 0.2f, &leftId, &remainingId);
        ImGuiDockingInternal.igDockBuilderSplitNode(remainingId, (int)ImGuiDir.Right, 0.25f, &rightId, &centerPartId);

        uint centerId, bottomId;
        ImGuiDockingInternal.igDockBuilderSplitNode(centerPartId, (int)ImGuiDir.Down, 0.3f, &bottomId, &centerId);

        ImGuiDockingInternal.igDockBuilderDockWindow("Hierarchy###EditorHierarchy", leftId);
        ImGuiDockingInternal.igDockBuilderDockWindow("Inspector", rightId);
        ImGuiDockingInternal.igDockBuilderDockWindow("Scene", centerId);
        ImGuiDockingInternal.igDockBuilderDockWindow("File Explorer", bottomId);

        LeftDockId = leftId;
        RightDockId = rightId;
        CenterDockId = centerId;
        BottomDockId = bottomId;
        UpDockId = upId;

        ImGuiDockingInternal.igDockBuilderFinish(dockspaceId);
    }

    private void AddWindow<T>() where T : EditorGuiWindow
    {
        var instance = Activator.CreateInstance<T>();
        _windows.Add(instance);
        instance.OnLoad();
    }

    private void RemoveWindow<T>() where T : EditorGuiWindow
    {
        var instance = _windows.FirstOrDefault(w => w.GetType() == typeof(T));
        if (instance != null)
        {
            _windows.Remove(instance);
            instance.OnUnload();
        }
    }
}