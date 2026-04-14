using FlyEngine.Core;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace FlyEngine.Game;

public static class Program
{
    public static void Main(string[] args)
    {
        var windowOptions = WindowOptions.Default with
        {
            Size = new Vector2D<int>(1024, 768),
            Title = "My first silk.net window",
            VSync = false,
            FramesPerSecond = 144,
            WindowState = WindowState.Fullscreen
        };
        var application = new Application(TestScene, windowOptions);
        application.Run();
    }

    private static void TestScene(Application application)
    {
        
    }
}