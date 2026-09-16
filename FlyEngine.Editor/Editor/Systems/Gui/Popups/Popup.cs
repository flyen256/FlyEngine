using ImGuiNET;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Editor.Systems;

public abstract class Popup
{
    protected virtual string Name => GetType().Name;
    protected virtual ImGuiWindowFlags Flags => ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove;

    protected bool Begin;
    private bool _opened;
    private bool _shouldOpen;

    protected abstract void OnRender();

    protected virtual void BeforeBegin() { }
    protected virtual void IfNotBegin() { }
    public virtual void OnLoad() { }
    protected virtual void OnOpen() { }
    
    public void Render(float deltaTime)
    {
        if (_shouldOpen)
        {
            _opened = true;
            ImGuiNet.OpenPopup(Name);
            _shouldOpen = false;
        }

        BeforeBegin();

        Begin = ImGuiNet.BeginPopupModal(Name, ref _opened, Flags);
        if (!Begin) 
        {
            IfNotBegin();
            return;
        }

        OnRender();
        ImGuiNet.EndPopup();
    }

    public void Open()
    {
        OnOpen();
        _shouldOpen = true;
    }

    public void Close()
    {
        _opened = false;
        _shouldOpen = false;
        ImGuiNet.CloseCurrentPopup();
    }
}
