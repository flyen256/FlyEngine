using System.Numerics;
using FlyEngine.Components.Common;
using FlyEngine.Components.Renderer._2D;
using FlyEngine.Physics;
using FlyEngine.Physics.Colliders;
using FlyEngine.Renderer;
using FlyEngine.Test;
using FlyEngine.UI;
using FlyEngine.UI.ImGui;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Texture = FlyEngine.Renderer.Texture;

namespace FlyEngine;

public class Application
{
    private readonly IWindow _window;
    private OpenGl? _gl;
    private Camera2D? _camera;
    public float AspectRatio;
    public static readonly List<Behaviour> Behaviours = new();
    public static readonly List<GameObject> GameObjects = new();
    public static readonly List<Element> Elements = new();

    public Application()
    {
        var windowOptions = WindowOptions.Default with
        {
            Size = new Vector2D<int>(800, 600),
            Title = "My first silk.net window"
        };
        _window = Window.Create(windowOptions);

        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.FramebufferResize += OnResize;

        _window.Run();
    }

    private void OnResize(Vector2D<int> newSize)
    {
        if(_gl == null) return;
        _gl.Gl.Viewport(0, 0, (uint)newSize.X, (uint)newSize.Y);
        AspectRatio = (float)newSize.X / newSize.Y;
        _gl.UpdateProjectionMatrix();
    }

    private unsafe void OnLoad()
    {
        _gl = new OpenGl(_window, this);
        _gl.Initialize();
        _gl.BindBuffers();
        _gl.ProcessShaders();

        var texture = new Texture("silk.jpg", _gl);

        _camera = new GameObject(
            new Vector3D<float>(0f, 0f, 0f),
            new Vector3D<float>(0f, 0f, 0f),
            new Vector4(0f, 0f, 0f, 0f),
            texture,
            [new Camera2D(Vector3D<float>.Zero)]
        ).ComponentStore.GetComponent<Camera2D>();

        new GameObject(
            new Vector3D<float>(0, 0, 0f),
            new Vector3D<float>(.25f, .25f, 0f),
            new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
            texture,
            [
                new PlayerMovement(_camera, 1f),
                new Collider(new Vector2D<float>(.25f, .25f), new Vector2D<float>(0, 0))
            ]
        );

        new GameObject(
            new Vector3D<float>(1, 1, 0f),
            new Vector3D<float>(.25f, .25f, 0f),
            new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
            texture,
            [
                new Collider(new Vector2D<float>(.25f, .25f), new Vector2D<float>(0, 0))
            ]
        );

        Behaviours.Add(new PhysicsManager());

        foreach (var behaviour in Behaviours)
            behaviour.OnLoad();

        Input.Initialize(_window);
        if (Input.InputContext != null)
        {
            ImGui.Initialize(
                _gl.Gl,
                _window,
                Input.InputContext
            );
        }
    }

    private void OnUpdate(double deltaTime)
    {
        foreach (var behaviour in Behaviours)
            behaviour.OnUpdate(deltaTime);
    }

    private unsafe void OnRender(double deltaTime)
    {
        if (_gl == null || _camera == null) return;
        _gl.Gl.Clear(ClearBufferMask.ColorBufferBit);

        _camera.UpdateMatrices(_window.Size.X, _window.Size.Y);

        var projection = _camera.ProjectionMatrix;
        var view = _camera.ViewMatrix;

        _gl.Gl.UseProgram(_gl.Program);
        var projLoc = _gl.Gl.GetUniformLocation(_gl.Program, "uProjection");
        var viewLoc = _gl.Gl.GetUniformLocation(_gl.Program, "uView");

        _gl.Gl.UniformMatrix4(projLoc, 1, false, (float*)&projection);
        _gl.Gl.UniformMatrix4(viewLoc, 1, false, (float*)&view);

        foreach (var gameObject in GameObjects)
            gameObject.BeginDraw(_gl);
        foreach (var element in Elements)
            element.BeginDraw(_gl);

        foreach (var behaviour in Behaviours)
            behaviour.OnRender(deltaTime);

        if (ImGui.Initialized)
            ImGui.Render((float)deltaTime);
    }
}
