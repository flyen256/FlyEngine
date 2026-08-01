using Microsoft.Extensions.Logging;

namespace FlyEngine.Core.Debugging;

public readonly struct Log(LogLevel level, string message)
{
    public LogLevel Level { get; } = level;
    public string Message { get; } = message;
}