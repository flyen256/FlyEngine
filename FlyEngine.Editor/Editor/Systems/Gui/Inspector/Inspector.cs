namespace FlyEngine.Editor.Systems;

public abstract class Inspector(EditorInspector editorInspector)
{
    protected readonly EditorInspector EditorInspector = editorInspector;
    
    public virtual void Initialize() { }
    public abstract void Render();
    public virtual void OnLoad() { }
    public virtual void OnUnload() { }
}