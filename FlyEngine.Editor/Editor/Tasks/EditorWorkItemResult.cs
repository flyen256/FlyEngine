namespace FlyEngine.Editor.Tasks;

public class EditorWorkItemResult<TResult>(Func<Task<TResult>> taskFunc) : EditorQueueItem
{
    private readonly TaskCompletionSource<TResult> _tcs = new();

    public Task<TResult> ResultTask => _tcs.Task;

    public override async Task ExecuteAsync()
    {
        try
        {
            var result = await taskFunc();
            _tcs.SetResult(result);
        }
        catch (Exception ex)
        {
            _tcs.SetException(ex);
        }
    }
}