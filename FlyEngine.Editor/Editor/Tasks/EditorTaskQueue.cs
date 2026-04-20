using System.Threading.Channels;

namespace FlyEngine.Editor.Tasks;

public class EditorTaskQueue
{
    private readonly Channel<EditorQueueItem> _channel = Channel.CreateUnbounded<EditorQueueItem>();
    private int _activeTasksCount;

    public EditorQueueItem? CurrentItem { get; private set; }

    public bool IsProcessing => _activeTasksCount > 0;
    
    public EditorTaskQueue()
    {
        Task.Run(ProcessQueueAsync);
    }

    public Task<TResult> Enqueue<TResult, TParam>(Func<TParam, Task<TResult>> taskFunc, TParam param, string? name = null)
    {
        var workItem = new EditorWorkItemResultParam<TResult, TParam>(taskFunc, param) 
        {
            Message = name ?? "Unnamed Task",
        };
        _channel.Writer.TryWrite(workItem);
        return workItem.ResultTask;
    }
    
    public Task<TResult> Enqueue<TResult>(Func<Task<TResult>> taskFunc, string? message = null)
    {
        var workItem = new EditorWorkItemResult<TResult>(taskFunc) 
        {
            Message = message ?? "Unnamed Task",
        };
        _channel.Writer.TryWrite(workItem);
        return workItem.ResultTask;
    }
    
    public Task Enqueue<TParam>(Func<TParam, Task> taskFunc, TParam param, string? name = null)
    {
        var workItem = new EditorWorkItemParam<TParam>(taskFunc, param) 
        {
            Message = name ?? "Unnamed Task",
        };
        _channel.Writer.TryWrite(workItem);
        return workItem.ResultTask;
    }
    
    public Task Enqueue(Func<Task> taskFunc, string? message = null)
    {
        var workItem = new EditorWorkItem(taskFunc) 
        {
            Message = message ?? "Unnamed Task",
        };
        _channel.Writer.TryWrite(workItem);
        return workItem.ResultTask;
    }

    private async Task ProcessQueueAsync()
    {
        await foreach (var item in _channel.Reader.ReadAllAsync())
        {
            try
            {
                CurrentItem = item;
                Interlocked.Increment(ref _activeTasksCount);
                await item.ExecuteAsync();
            }
            finally
            {
                Interlocked.Decrement(ref _activeTasksCount);
                CurrentItem = null;
            }
        }
    }
}