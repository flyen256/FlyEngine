using Microsoft.Extensions.Logging;

namespace FlyEngine.Editor.Systems.Console;

public struct EditorConsoleMessage
{
    public LogLevel Level { get; set; }
    public string Message { get; set; }
}