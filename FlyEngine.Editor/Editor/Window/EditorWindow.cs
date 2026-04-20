using System.Runtime.InteropServices;
using FlyEngine.Core.Engine;
using FlyEngine.Core.Engine.Components.Renderer;
using FlyEngine.Core.Engine.Components.Renderer._3D;
using FlyEngine.Core.Engine.Gui.ImGui;
using FlyEngine.Core.Engine.Renderer;
using FlyEngine.Core.Engine.SceneManagement;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace FlyEngine.Editor.Window;

public class EditorWindow(ApplicationWindowOptions windowOptions) : BaseWindow(windowOptions)
{
    public override bool IsEditor => true;
    
    private readonly ILogger _logger = new Logger<OpenGlWindow>(LoggerFactory.Create(builder => builder.AddConsole()));

    private static Scene? Scene => SceneManager.CurrentScene;
    
    private bool _graphicsReady;
    
    protected override void OnLoad()
    {
        OpenGl = new OpenGl(Handle);
        OpenGl.Initialize();
        OpenGl.ProcessShaders();

        Input.Initialize(Handle);

        if (Input.InputContext != null)
            ImGui.Initialize(OpenGl.Gl, Handle, Input.InputContext, WindowOptions.MinSize);

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
        UpdateMatrices();

        if (OpenGl == null)
        {
            Scene?.Update(deltaTime);
            return;
        }

        OpenGl.RenderPipeline.Render(deltaTime);

        if (!ImGui.Initialized || ImGui.Controller == null)
        {
            Scene?.Update(deltaTime);
            return;
        }
        OpenGl.Gl.Clear(ClearBufferMask.ColorBufferBit);
        ImGui.Controller.Update((float)deltaTime);
        if (Scene != null)
        {
            var renderers = CollectionsMarshal.AsSpan(Scene.UiWindows.ToList());
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (!renderer.IsActive()) continue;
                renderer.Render();
            }
        }
        Scene?.Update(deltaTime);
        base.OnRender(deltaTime);
        ImGui.Controller.Render();
    }
}