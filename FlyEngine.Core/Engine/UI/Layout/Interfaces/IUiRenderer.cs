namespace FlyEngine.Core.Engine.UI.Layout.Interfaces;

public interface IUiRenderer
{
    public bool IsActive();
    public void Render();
    public void OnLoadUi();
}