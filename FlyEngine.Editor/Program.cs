using FlyEngine.Core;
using FlyEngine.Editor.Window;
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
        Editor.Start(new EditorWindow(windowOptions));
    }
}