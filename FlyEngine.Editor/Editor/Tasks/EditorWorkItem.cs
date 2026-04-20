namespace FlyEngine.Editor.Tasks;

public class EditorWorkItem(Func<Task> taskFunc) : EditorQueueItem
{
    private readonly TaskCompletionSource _tcs = new();

    public Task ResultTask => _tcs.Task;

    public override async Task ExecuteAsync()
    {
        try
        {
            await taskFunc();
            _tcs.SetResult();
        }
        catch (Exception ex)
        {
            _tcs.SetException(ex);
        }
    }
}