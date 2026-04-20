using System.Numerics;
using FlyEngine.Editor.Systems.Console;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace FlyEngine.Editor.Systems.Gui;

public class EditorConsoleGui : EditorGuiWindow
{
    protected override string Title => "Console";

    protected override void BeforeBegin()
    {
        ImGui.SetNextWindowDockID(EditorGui.BottomDockId);
    }

    protected override void OnRender(double deltaTime)
    {
        if (EditorConsole.Instance == null || Core.Engine.Gui.ImGui.ImGui.Controller == null) return;
        if (ImGui.Button("Clear")) { EditorConsole.Instance.Messages.Clear(); }
        ImGui.SameLine();
        ImGui.TextUnformatted($"Messages: {EditorConsole.Instance.Messages.Count}");
    
        ImGui.Separator();

        if (ImGui.BeginChild("ConsoleMessages", new Vector2(0, 0), ImGuiChildFlags.None, base.Flags | ImGuiWindowFlags.HorizontalScrollbar))
        {
            ImGui.PushTextWrapPos(-1.0f);
            foreach (var msg in EditorConsole.Instance.Messages)
            {
                var color = GetColorForLevel(msg.Level);
                ImGui.PushFont(Core.Engine.Gui.ImGui.ImGui.Controller.ArialFont);
                ImGui.PushStyleColor(ImGuiCol.Text, color);
                ImGui.TextUnformatted($"[{msg.Level}] {msg.Message}");
                ImGui.PopStyleColor();
                ImGui.PopFont();
            }
            ImGui.PopTextWrapPos();

            if (ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
                ImGui.SetScrollHereY(1.0f);
        }
        ImGui.EndChild();
    }

    private Vector4 GetColorForLevel(LogLevel level)
    {
        return level switch
        {
            LogLevel.Error => new Vector4(1.0f, 0.4f, 0.4f, 1.0f),
            LogLevel.Warning => new Vector4(1.0f, 0.8f, 0.0f, 1.0f),
            LogLevel.Information => new Vector4(0.8f, 0.8f, 0.8f, 1.0f),
            _ => new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
        };
    }
}