using FlyEngine.Core;
using FlyEngine.Core.Windowing;
using FlyEngine.Editor.Scripting;
using Silk.NET.Maths;

namespace FlyEngine.Editor;

internal static class Program
{
    private static void Main()
    {
        var windowOptions = ApplicationWindowOptions.Default with
        {
            MinSize = new Vector2D<int>(800, 600)
        };
        Application.Initialize(true);
        Editor.Start(new Window(windowOptions));
    }
}