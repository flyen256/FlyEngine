namespace FlyEngine.Editor.Tasks;

public class EditorWorkItemResultParam<TResult, TParam>(Func<TParam, Task<TResult>> taskFunc, TParam param) : EditorQueueItem
{
    private readonly TaskCompletionSource<TResult> _tcs = new();

    public Task<TResult> ResultTask => _tcs.Task;

    public override async Task ExecuteAsync()
    {
        try
        {
            var result = await taskFunc(param);
            _tcs.SetResult(result);
        }
        catch (Exception ex)
        {
            _tcs.SetException(ex);
        }
    }
}