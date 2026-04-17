using System.ComponentModel;
using FlyEngine.Core.Engine;
using FlyEngine.Core.Engine.Components.Renderer._3D;
using FlyEngine.Core.Engine.Window;
using FlyEngine.Game.UI;
using FlyEngine.Network;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Component = FlyEngine.Core.Engine.Components.Common.Component;

namespace FlyEngine.Game;


public static class Program
{
    public static void Main(string[] args)
    {
        var windowOptions = ApplicationWindowOptions.Default with
        {
            Size = new Vector2D<int>(640, 480),
            MinSize = new Vector2D<int>(640, 480),
            Title = "My first silk.net window",
            VSync = false,
            FramesPerSecond = 144,
        };
        var application = new Application(TestScene, windowOptions);
        application.Run();
    }

    private static void TestScene(Application application)
    {
        var networkManager = Component.CreateGameObject<NetworkManager>();
        var camera = Component.CreateGameObject<Camera3D>();
        var menu = Component.CreateGameObject<Menu>();
    }
}