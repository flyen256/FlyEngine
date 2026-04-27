using System.Numerics;
using FlyEngine.Core.Components.Common;
using FlyEngine.Core.Gui.Layout;
using ImGuiNET;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Core.Gui;

public abstract class GuiWindow : Component
{
    public bool IsOpened = true;
    
    public GuiAnchor Anchor = GuiAnchor.TopLeft;
    
    public Vector2 Offset = Vector2.Zero;

    protected virtual string Name => GetType().Name;
    protected virtual ImGuiWindowFlags Flags =>
        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize;

    protected GuiElement Element = new();

    public virtual void OnLoadUi() { }

    public void Render()
    {
        if (!IsOpened) return;

        UpdatePosition();

        ImGuiNet.Begin(Name, ref IsOpened, Flags);
        Element.Draw();
        ImGuiNet.End();
    }

    private void UpdatePosition()
    {
        var viewport = ImGuiNet.GetMainViewport();
        var screenPos = viewport.Pos;
        var screenSize = viewport.Size;

        Vector2 anchorPos = Vector2.Zero;
        Vector2 pivot = Vector2.Zero;

        switch (Anchor)
        {
            case GuiAnchor.TopLeft:
                anchorPos = screenPos;
                pivot = new Vector2(0f, 0f);
                break;
            case GuiAnchor.TopCenter:
                anchorPos = new Vector2(screenPos.X + screenSize.X / 2f, screenPos.Y);
                pivot = new Vector2(0.5f, 0f);
                break;
            case GuiAnchor.TopRight:
                anchorPos = new Vector2(screenPos.X + screenSize.X, screenPos.Y);
                pivot = new Vector2(1f, 0f);
                break;
            case GuiAnchor.CenterLeft:
                anchorPos = new Vector2(screenPos.X, screenPos.Y + screenSize.Y / 2f);
                pivot = new Vector2(0f, 0.5f);
                break;
            case GuiAnchor.Center:
                anchorPos = screenPos + screenSize / 2f;
                pivot = new Vector2(0.5f, 0.5f);
                break;
            case GuiAnchor.CenterRight:
                anchorPos = new Vector2(screenPos.X + screenSize.X, screenPos.Y + screenSize.Y / 2f);
                pivot = new Vector2(1f, 0.5f);
                break;
            case GuiAnchor.BottomLeft:
                anchorPos = new Vector2(screenPos.X, screenPos.Y + screenSize.Y);
                pivot = new Vector2(0f, 1f);
                break;
            case GuiAnchor.BottomCenter:
                anchorPos = new Vector2(screenPos.X + screenSize.X / 2f, screenPos.Y + screenSize.Y);
                pivot = new Vector2(0.5f, 1f);
                break;
            case GuiAnchor.BottomRight:
                anchorPos = screenPos + screenSize;
                pivot = new Vector2(1f, 1f);
                break;
        }

        ImGuiNet.SetNextWindowPos(anchorPos + Offset, ImGuiCond.Always, pivot);
    }
}