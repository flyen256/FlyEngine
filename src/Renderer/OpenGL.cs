using System.Drawing;
using System.Numerics;
using FlyEngine.Reactive;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace FlyEngine.Renderer;

public class OpenGl
{
    public GL Gl { get; private set; }
    public uint Program { get; private set; }
    public ReactiveList<Texture> Textures { get; private set; } = new();
    public uint Vao { get; private set; }

    private uint _vbo;
    private uint _ebo;
    private readonly Application _main;
    private readonly IWindow _window;
    private Matrix4x4 _projection;

    public OpenGl(IWindow window, Application main)
    {
        _window = window;
        Gl = window.CreateOpenGL();
        _main = main;

        Textures.OnAdd += OnAddTexture;
        Textures.OnRemove += OnRemoveTexture;
    }

    private void OnAddTexture(Texture texture)
    {
        texture.Load();
    }

    public void OnRemoveTexture(Texture texture)
    {
        texture.UnLoad();
    }

    public void Initialize()
    {
        Gl.Viewport(0, 0, (uint)_window.Size.X, (uint)_window.Size.Y);
        _main.AspectRatio = (float)_window.Size.X / _window.Size.Y;
        UpdateProjectionMatrix();
        Gl.Enable(EnableCap.Blend);
        Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        Gl.ClearColor(Color.Gray);
    }

    public unsafe void BindBuffers()
    {
        Vao = Gl.GenVertexArray();
        Gl.BindVertexArray(Vao);
        float[] vertices =
        {
        //       aPosition     | aTexCoords
            0.5f,  0.5f, 0.0f,  1.0f, 0.0f,
            0.5f, -0.5f, 0.0f,  1.0f, 1.0f,
            -0.5f, -0.5f, 0.0f,  0.0f, 1.0f,
            -0.5f, 0.5f, 0.0f,  0.0f, 0.0f
        };
        _vbo = Gl.GenBuffer();
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        fixed (float* buf = vertices)
            Gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);
        uint[] indices = {
            0u, 1u, 3u,
            1u, 2u, 3u
        };
        _ebo = Gl.GenBuffer();
        Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        fixed (uint* buf = indices)
            Gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), buf, BufferUsageARB.StaticDraw);
    }

    private string LoadShader(string shader)
    {
        var assembly = typeof(OpenGl).Assembly;
    
        // Этот код поможет вам увидеть в консоли реальные имена всех ресурсов, 
        // если вы ошибетесь в названии:
        // var names = assembly.GetManifestResourceNames();
        // foreach (var name in names) Console.WriteLine(name);

        var resourceName = $"flyengine2D.src.Renderer.Shaders.{shader}";

        using var stream = assembly.GetManifestResourceStream(resourceName) 
                           ?? throw new Exception($"Resource {resourceName} not found!");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public unsafe void ProcessShaders()
    {
        var vertexCode = LoadShader("vertex.vert");
        var fragmentCode = LoadShader("fragment.frag");
        var vertexShader = Gl.CreateShader(ShaderType.VertexShader);
        Gl.ShaderSource(vertexShader, vertexCode);
        var fragmentShader = Gl.CreateShader(ShaderType.FragmentShader);
        Gl.ShaderSource(fragmentShader, fragmentCode);
        Gl.CompileShader(vertexShader);
        Gl.CompileShader(fragmentShader);
        Gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out var vStatusVert);
        Gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out var vStatusFrag);
        if (vStatusVert != (int)GLEnum.True || vStatusFrag != (int)GLEnum.True)
            throw new Exception($"Shaders failed to compile: vertex - {Gl.GetShaderInfoLog(vertexShader)}, fragment - {Gl.GetShaderInfoLog(fragmentShader)}");
        Program = Gl.CreateProgram();
        Gl.AttachShader(Program, vertexShader);
        Gl.AttachShader(Program, fragmentShader);
        Gl.LinkProgram(Program);
        Gl.GetProgram(Program, ProgramPropertyARB.LinkStatus, out var lStatus);
        if (lStatus != (int)GLEnum.True)
            throw new Exception("Program failed to link: " + Gl.GetProgramInfoLog(Program));
        Gl.DetachShader(Program, vertexShader);
        Gl.DetachShader(Program, fragmentShader);
        Gl.DeleteShader(vertexShader);
        Gl.DeleteShader(fragmentShader);
        const uint texCoordLoc = 1;
        Gl.EnableVertexAttribArray(texCoordLoc);
        Gl.VertexAttribPointer(texCoordLoc, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)(3 * sizeof(float)));
        const uint positionLoc = 0;
        Gl.EnableVertexAttribArray(positionLoc);
        Gl.VertexAttribPointer(positionLoc, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)0);
        Gl.BindVertexArray(0);
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
    }

    public unsafe void UpdateProjectionMatrix()
    {
        var viewHeight = 2.0f;
        var viewWidth = viewHeight * _main.AspectRatio;

        _projection = Matrix4x4.CreateOrthographicOffCenter(
            -viewWidth/2, viewWidth/2,
            -viewHeight/2, viewHeight/2,
            -1.0f, 1.0f);

        Gl.UseProgram(Program);
        var projLoc = Gl.GetUniformLocation(Program, "uProjection");

        fixed (float* matrixPtr = &_projection.M11)
        {
            Gl.UniformMatrix4(projLoc, 1, false, matrixPtr);
        }
    }
}
