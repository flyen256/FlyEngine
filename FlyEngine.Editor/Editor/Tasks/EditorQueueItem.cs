namespace FlyEngine.Editor.Tasks;

public abstract class EditorQueueItem
{
    public string Message { get; init; } = string.Empty;
    public abstract Task ExecuteAsync();
}