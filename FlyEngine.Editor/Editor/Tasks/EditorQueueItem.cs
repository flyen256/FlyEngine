namespace FlyEngine.Editor.Tasks;

public abstract class EditorQueueItem
{
    public string Message { get; init; } = string.Empty;
    public virtual void Execute() { }
    public virtual Task ExecuteAsync() { return Task.CompletedTask; }
}