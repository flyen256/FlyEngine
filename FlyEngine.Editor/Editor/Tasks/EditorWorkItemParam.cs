namespace FlyEngine.Editor.Tasks;

public class EditorWorkItemParam<TParam>(Func<TParam, Task> taskFunc, TParam param) : EditorQueueItem
{
    private readonly TaskCompletionSource _tcs = new();

    public Task ResultTask => _tcs.Task;

    public override async Task ExecuteAsync()
    {
        try
        {
            await taskFunc(param);
            _tcs.SetResult();
        }
        catch (Exception ex)
        {
            _tcs.SetException(ex);
        }
    }
}