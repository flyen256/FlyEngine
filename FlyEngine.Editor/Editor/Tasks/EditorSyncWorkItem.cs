namespace FlyEngine.Editor.Tasks;

public class EditorSyncWorkItem(Action action) : EditorQueueItem
{
    public override void Execute() => action.Invoke();
}