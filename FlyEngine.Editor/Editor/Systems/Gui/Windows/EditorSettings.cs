using ImGuiNet = ImGuiNET.ImGui;
using System.Numerics;
using FlyEngine.Core.Debugging;
using FlyEngine.Core.Project;
using FlyEngine.Editor.Localization;
using FlyEngine.Editor.Storage;
using ImGuiNET;

namespace FlyEngine.Editor.Systems;

public class EditorSettings : EditorGuiWindow
{
    protected override string Title => "Settings";
    
    private static ProjectFile Project => ProjectFile.CurrentProject;
    public override bool IsVisible => EditorGui.Instance?.OpenedWindows.Contains(Title) ?? false;

    private enum SettingTab
    {
        General,
        Video,
        Cpu,
        Editor
    }
    
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
                DrawTabButton(EditorLocalization.GetString("settings.general.name"), SettingTab.General);
                DrawTabButton(EditorLocalization.GetString("settings.video.name"), SettingTab.Video);
                DrawTabButton(EditorLocalization.GetString("settings.cpu.name"), SettingTab.Cpu);
                DrawTabButton(EditorLocalization.GetString("settings.editor.name"), SettingTab.Editor);
                
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
                    case SettingTab.Editor:
                        DrawEditorSettings();
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
        SettingsTitle(EditorLocalization.GetString("settings.general.title"));
        
        var projectName = Project.Name;
        if (ImGuiNet.InputText(EditorLocalization.GetString("settings.general.project_name") +
                               "##SettingsPROJECTNAME", ref projectName, 64))
        {
            Project.Name = projectName;
            Project.SaveProject();
        }
    }

    private static void DrawVideoSettings()
    {
        SettingsTitle(EditorLocalization.GetString("settings.video.title"));
        
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
        SettingsTitle(EditorLocalization.GetString("settings.cpu.title"));
        
        var updatesPerSecond = Project.CpuSettings.UpdatesPerSecond;
        if (ImGuiNet.DragInt("Updates per second##SettingsUPS", ref updatesPerSecond, 0.1f, 1, 1000))
        {
            Project.CpuSettings.UpdatesPerSecond = updatesPerSecond;
            Project.SaveProject();
        }
    }

    private static void DrawEditorSettings()
    {
        SettingsTitle(EditorLocalization.GetString("settings.editor.title"));

        if (ImGuiNet.BeginCombo(EditorLocalization.GetString("settings.editor.language") +
                                $"##LANGUAGE", EditorStorage.Preferences.Language.ToString()))
        {
            foreach (var state in typeof(EditorLanguage).GetEnumValues())
            {
                var isSelected = Equals(EditorStorage.Preferences.Language, (Enum)state);
                if (ImGuiNet.Selectable(LanguageToString((EditorLanguage)state), isSelected))
                    EditorStorage.Preferences.Language = (EditorLanguage)state;

                if (isSelected)
                    ImGuiNet.SetItemDefaultFocus();
            }
            ImGuiNet.EndCombo();
        }
    }

    private static string LanguageToString(EditorLanguage language) => language switch
    {
        EditorLanguage.English => "English",
        EditorLanguage.Russian => "Русский",
        _ => language.ToString()
    };

    private static void SettingsTitle(string fmt)
    {
        ImGuiNet.Text(fmt);
        ImGuiNet.Spacing();
        ImGuiNet.Separator();
        ImGuiNet.Spacing();
    }
}
