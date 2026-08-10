using ImGuiNet = ImGuiNET.ImGui;
using System.Numerics;
using FlyEngine.Core.Debugging;
using FlyEngine.Core.Project;
using ImGuiNET;

namespace FlyEngine.Editor.Systems;

public class EditorSettings : EditorGuiWindow
{
    protected override string Title => "Settings";
    
    private static ProjectFile Project => ProjectFile.CurrentProject;
    public override bool IsVisible => EditorGui.Instance?.OpenedWindows.Contains(Title) ?? false;
    
    private SettingTab _currentTab = SettingTab.General;

    protected override bool Begin()
    {
        var visible = IsVisible;
        var begin = ImGuiNet.Begin(Title, ref visible, Flags);
        if (!visible) EditorGui.Instance?.OpenedWindows.Remove(Title);
        return begin;
    }

    protected override void OnRender(double deltaTime)
    {
        if (ImGuiNet.BeginTable("SettingsTable", 2, ImGuiTableFlags.Resizable))
        {
            ImGuiNet.TableSetupColumn("Tabs", ImGuiTableColumnFlags.WidthFixed, 150.0f);
            ImGuiNet.TableSetupColumn("Content", ImGuiTableColumnFlags.WidthStretch);

            ImGuiNet.TableNextRow();

            ImGuiNet.TableSetColumnIndex(0);

            if (ImGuiNet.BeginChild("TabsList", new Vector2(0, 0), ImGuiChildFlags.None))
            {
                DrawTabButton("General", SettingTab.General);
                DrawTabButton("Video", SettingTab.Video);
                DrawTabButton("Cpu", SettingTab.Cpu);
                
                ImGuiNet.EndChild();
            }

            ImGuiNet.TableSetColumnIndex(1);

            if (ImGuiNet.BeginChild("TabContent", new Vector2(0, 0), ImGuiChildFlags.None))
            {
                switch (_currentTab)
                {
                    default:
                        Debug.LogError($"Unknown tab {_currentTab}");
                        break;
                    case SettingTab.General:
                        DrawGeneralSettings();
                        break;
                    case SettingTab.Video:
                        DrawVideoSettings();
                        break;
                    case SettingTab.Cpu:
                        DrawCpuSettings();
                        break;
                }
                
                ImGuiNet.EndChild();
            }

            ImGuiNet.EndTable();
        }
    }

    private void DrawTabButton(string label, SettingTab tab)
    {
        var isSelected = _currentTab == tab;

        if (ImGuiNet.Selectable(label, isSelected))
            _currentTab = tab;
    }

    private static void DrawGeneralSettings()
    {
        SettingsTitle("General Settings");
        
        var projectName = Project.Name;
        if (ImGuiNet.InputText("Project Name##SettingsPROJECTNAME", ref projectName, 64))
        {
            Project.Name = projectName;
            Project.SaveProject();
        }
    }

    private static void DrawVideoSettings()
    {
        SettingsTitle("Video Settings");
        
        var vsync = Project.VideoSettings.VSync;
        if (ImGuiNet.Checkbox("Enable VSync##SettingsVSYNC", ref vsync))
        {
            Project.VideoSettings.VSync = vsync;
            Project.SaveProject();
        }
        var framesPerSecond = Project.VideoSettings.FramesPerSecond;
        if (ImGuiNet.DragInt("Frames per second##SettingsFPS", ref framesPerSecond, 0.1f))
        {
            Project.VideoSettings.FramesPerSecond = framesPerSecond;
            Project.SaveProject();
        }
    }

    private static void DrawCpuSettings()
    {
        SettingsTitle("CPU Settings");
        
        var updatesPerSecond = Project.CpuSettings.UpdatesPerSecond;
        if (ImGuiNet.DragInt("Updates per second##SettingsUPS", ref updatesPerSecond, 0.1f, 1, 1000))
        {
            Project.CpuSettings.UpdatesPerSecond = updatesPerSecond;
            Project.SaveProject();
        }
    }

    private static void SettingsTitle(string fmt)
    {
        ImGuiNet.Text(fmt);
        ImGuiNet.Spacing();
        ImGuiNet.Separator();
        ImGuiNet.Spacing();
    }
}
