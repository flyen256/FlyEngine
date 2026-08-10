using System.Collections.Concurrent;
using FlyEngine.Core.Debugging;

namespace FlyEngine.Core.Threading;

public static class Dispatcher
{
    private static ConcurrentQueue<Action> MainThreadQueue { get; } = new();
    
    public static void Dispatch(Action action)
    {
        MainThreadQueue.Enqueue(action);
    }

    public static void ExecuteDispatchedActions()
    {
        while (MainThreadQueue.TryDequeue(out var action))
        {
            try
            {
                action.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing dispatched action: {ex.Message}");
            }
        }
    }
}