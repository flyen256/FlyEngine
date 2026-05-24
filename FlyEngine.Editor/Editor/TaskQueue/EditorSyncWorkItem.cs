namespace FlyEngine.Editor.TaskQueue;

public class EditorSyncWorkItem(Action action) : EditorQueueItem
{
    public override void Execute() => action.Invoke();
}