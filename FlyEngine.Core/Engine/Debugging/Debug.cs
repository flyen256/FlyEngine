using Microsoft.Extensions.Logging;

namespace FlyEngine.Core.Debugging;

public static class Debug
{
    private static readonly List<Log> Logs = [];
    
    public static IReadOnlyList<Log> LogList => Logs;

    public static void Log(LogLevel level, string message)
    {
        Logs.Add(new Log(level, message));
    }

    public static void LogInfo(string message)
    {
        Log(LogLevel.Information, message);
    }

    public static void LogWarning(string message)
    {
        Log(LogLevel.Warning, message);
    }

    public static void LogError(string message)
    {
        Log(LogLevel.Error, message);
    }

    public static void ClearLogs()
    {
        Logs.Clear();
    }
}