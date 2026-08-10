using System.Numerics;
using ImGuiNET;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Editor.Systems;

public abstract class EditorGuiWindow
{
    protected virtual string Title => "Editor GUI Window";
    public virtual bool IsVisible => true;
    protected virtual ImGuiWindowFlags Flags => ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse;
    
    protected internal virtual void OnLoad() { }
    protected internal virtual void OnUnload() { }
    protected internal virtual void OnUpdate(double deltaTime) { }
    protected virtual void OnRender(double deltaTime) { }
    protected virtual void BeforeBegin() { }

    protected virtual bool Begin()
    {
        return ImGuiNet.Begin(Title, Flags);
    }

    protected virtual void End()
    {
        ImGuiNet.End();
    }
    
    private Vector2 _lastWindowSize;
    
    public virtual void Render(double deltaTime)
    {
        BeforeBegin();
        if (Begin())
        {
            var currentSize = ImGuiNet.GetWindowSize();
            
            if (currentSize != _lastWindowSize)
            {
                EditorAction.WindowResize(currentSize);
                _lastWindowSize = currentSize;
            }
            OnRender(deltaTime);
        }
        End();
    }
}