using System.Numerics;
using FlyEngine.Core.Engine.Components.Common;
using FlyEngine.Core.Engine.UI.Layout;
using FlyEngine.Core.Engine.UI.Layout.Interfaces;
using ImGuiNET;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Core.Engine.UI;

public abstract class UiRenderer : Component, IUiRenderer
{
    public bool IsOpened = true;
    
    protected virtual string Name => GetType().Name;
    protected virtual ImGuiWindowFlags Flags =>
        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize;

    protected virtual Vector2 Position => Vector2.Zero;
    
    protected UiElement Element = new();

    protected override void OnInitialize()
    {
        Application.Instance.UiRenderers.Add(this);
    }

    protected internal override void OnRemoved()
    {
        Application.Instance.UiRenderers.Remove(this);
    }

    public virtual void OnLoadUi() {}

    public void Render()
    {
        if (!IsOpened) return;

        ImGuiNet.SetNextWindowPos(Position, ImGuiCond.Always);

        ImGuiNet.Begin(GetType().Name, ref IsOpened, Flags);
        Element.Draw();
        ImGuiNet.End();
    }
}