using System.Reflection;
using Silk.NET.Core.Contexts;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace FlyEngine.Core;

public struct ApplicationWindowOptions(
    bool isVisible,
    Vector2D<int> position,
    Vector2D<int> size,
    Vector2D<int> minSize,
    double framesPerSecond,
    double updatesPerSecond,
    GraphicsAPI api,
    string title,
    WindowState windowState,
    WindowBorder windowBorder,
    bool isVSync,
    bool shouldSwapAutomatically,
    VideoMode videoMode,
    int? preferredDepthBufferBits = null,
    int? preferredStencilBufferBits = null,
    Vector4D<int>? preferredBitDepth = null,
    bool transparentFramebuffer = false,
    bool topMost = false,
    bool isEventDriven = false,
    IGLContext? sharedContext = null,
    int? samples = null,
    string? windowClass = null,
    bool isContextControlDisabled = false)
    : IWindowProperties
{
    public bool IsVisible { get; set; } = isVisible;
    public bool ShouldSwapAutomatically { get; set; } = shouldSwapAutomatically;
    public bool IsEventDriven { get; set; } = isEventDriven;
    public bool IsContextControlDisabled { get; set; } = isContextControlDisabled;
    public VideoMode VideoMode { get; set; } = videoMode;
    public int? PreferredDepthBufferBits { get; set; } = preferredDepthBufferBits;
    public int? PreferredStencilBufferBits { get; set; } = preferredStencilBufferBits;
    public Vector4D<int>? PreferredBitDepth { get; set; } = preferredBitDepth;
    public int? Samples { get; set; } = samples;
    public Vector2D<int> Position { get; set; } = position;
    public Vector2D<int> Size { get; set; } = size;
    public Vector2D<int> MinSize { get; set; } = minSize;
    public double FramesPerSecond { get; set; } = framesPerSecond;
    public double UpdatesPerSecond { get; set; } = updatesPerSecond;
    public GraphicsAPI API { get; set; } = api;
    public bool VSync { get; set; } = isVSync;
    public string Title { get; set; } = title;
    public WindowState WindowState { get; set; } = windowState;
    public WindowBorder WindowBorder { get; set; } = windowBorder;
    public bool TransparentFramebuffer { get; set; } = transparentFramebuffer;
    public bool TopMost { get; set; } = topMost;
    public IGLContext? SharedContext { get; set; } = sharedContext;
    public string? WindowClass { get; set; } = windowClass;

    public static ApplicationWindowOptions Default { get; }
    public static ApplicationWindowOptions DefaultVulkan { get; }
    
    static ApplicationWindowOptions()
    {
        var title = "Silk.NET Window";
        var name = Assembly.GetEntryAssembly()?.GetName().Name;
        if (name != null)
            title = name;

        Default = new ApplicationWindowOptions(
            true,
            new Vector2D<int>(50, 50),
            new Vector2D<int>(1280, 720),
            new Vector2D<int>(640, 480),
            0.0,
            0.0,
            GraphicsAPI.Default,
            title,
            WindowState.Normal,
            WindowBorder.Resizable,
            true,
            true,
            VideoMode.Default);
        DefaultVulkan = new ApplicationWindowOptions(
            true,
            new Vector2D<int>(50, 50),
            new Vector2D<int>(1280, 720),
            new Vector2D<int>(640, 480),
            0.0,
            0.0,
            GraphicsAPI.DefaultVulkan,
            title,
            WindowState.Normal,
            WindowBorder.Resizable,
            false,
            false,
            VideoMode.Default);
    }

    public WindowOptions AsWindowOptions()
    {
        return new WindowOptions
        {
            IsVisible = IsVisible,
            Position = Position,
            Size = Size,
            FramesPerSecond = FramesPerSecond,
            UpdatesPerSecond = UpdatesPerSecond,
            API = API,
            Title = Title,
            WindowState = WindowState,
            WindowBorder = WindowBorder,
            ShouldSwapAutomatically = ShouldSwapAutomatically,
            VideoMode = VideoMode,
            PreferredDepthBufferBits = PreferredDepthBufferBits,
            TransparentFramebuffer = TransparentFramebuffer,
            TopMost = TopMost,
            IsEventDriven = IsEventDriven,
            VSync = VSync,
            SharedContext = SharedContext,
            PreferredStencilBufferBits = PreferredStencilBufferBits,
            PreferredBitDepth = PreferredBitDepth,
            Samples = Samples,
            WindowClass = WindowClass,
            IsContextControlDisabled = IsContextControlDisabled,
        };
    }
}