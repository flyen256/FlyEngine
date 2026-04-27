using System.Runtime.InteropServices;
using FlyEngine.Core.Components.Renderer;
using FlyEngine.Core.Components.Renderer._3D;
using FlyEngine.Core.Gui.ImGui;
using FlyEngine.Core.Renderer;
using FlyEngine.Core.SceneManagement;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Scene = FlyEngine.Core.SceneManagement.Scene;

namespace FlyEngine.Core;

public class OpenGlWindow(ApplicationWindowOptions windowOptions) : BaseWindow(windowOptions)
{
    private readonly ILogger _logger = new Logger<OpenGlWindow>(LoggerFactory.Create(builder => builder.AddConsole()));

    private static Scene? Scene => SceneManager.CurrentScene;
    
    private bool _graphicsReady;
    
    protected override void OnLoad()
    {
        OpenGl = new OpenGl(Handle, this);
        OpenGl.Initialize();
        OpenGl.ProcessShaders();

        Input.Initialize(Handle);

        if (Input.InputContext != null)
            ImGui.Initialize(
                OpenGl.Gl,
                Handle,
                Input.InputContext,
                WindowOptions.MinSize
            );

        _graphicsReady = true;
        base.OnLoad();
    }

    protected override void OnFramebufferResize(Vector2D<int> newSize)
    {
        var targetSize = newSize;
        if (!_graphicsReady || OpenGl == null) return;
        OpenGl.Gl.Viewport(0, 0, (uint)targetSize.X, (uint)targetSize.Y);
        OpenGl.RenderPipeline.ResizeGBuffer(targetSize);
    }

    protected override void OnRender(double deltaTime)
    {
        var activeCameras = Scene?.Cameras.Where(camera => camera.IsActive()).ToList();
        Camera3D? camera3D = null;
        if (activeCameras != null)
        {
            camera3D = activeCameras.Count > 0 && activeCameras.OfType<Camera3D>().Any() ?
                activeCameras.OfType<Camera3D>().First(c => c.IsActive()) :
                null;
            Camera.CurrentCamera = camera3D;
        }
        camera3D?.UpdateMatrices(AspectRatio);
        
        if (OpenGl == null) return;

        OpenGl.RenderPipeline.Render(deltaTime);

        if (!ImGui.Initialized || ImGui.Controller == null) return;
        ImGui.Controller.Update((float)deltaTime);
        if (Scene != null)
        {
            var renderers = CollectionsMarshal.AsSpan(Scene.GuiWindows.ToList());
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (!renderer.IsActive()) continue;
                renderer.Render();
            }
        }
        ImGui.Controller.Render();
        Scene?.Update(deltaTime);
        base.OnRender(deltaTime);
    }
}