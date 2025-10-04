using System.Drawing;
using System.Numerics;
using Flyeng.Reactive;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using StbImageSharp;

namespace Flyeng;

public class OpenGL
{
    public GL GL { get; private set; }
    public uint Program { get; private set; }
    public ReactiveList<Texture> Textures { get; private set; } = new();
    public uint vao { get; private set; }

    private uint _vbo;
    private uint _ebo;
    private Application _main;
    private IWindow _window;
    private Matrix4x4 _projection;

    public OpenGL(IWindow window, Application main)
    {
        _window = window;
        GL = window.CreateOpenGL();
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
        GL.Viewport(0, 0, (uint)_window.Size.X, (uint)_window.Size.Y);
        _main.AspectRatio = (float)_window.Size.X / _window.Size.Y;
        UpdateProjectionMatrix();
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.ClearColor(Color.Gray);
    }

    public unsafe void BindBuffers()
    {
        vao = GL.GenVertexArray();
        GL.BindVertexArray(vao);
        float[] vertices =
        {
        //       aPosition     | aTexCoords
            0.5f,  0.5f, 0.0f,  1.0f, 0.0f,
            0.5f, -0.5f, 0.0f,  1.0f, 1.0f,
            -0.5f, -0.5f, 0.0f,  0.0f, 1.0f,
            -0.5f, 0.5f, 0.0f,  0.0f, 0.0f
        };
        _vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        fixed (float* buf = vertices)
            GL.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);
        uint[] indices = {
            0u, 1u, 3u,
            1u, 2u, 3u
        };
        _ebo = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        fixed (uint* buf = indices)
            GL.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), buf, BufferUsageARB.StaticDraw);
    }

    public unsafe void ProcessShaders()
    {
        string vertexCode = File.ReadAllText("src/Renderer/Shaders/vertex.vert");
        string fragmentCode = File.ReadAllText("src/Renderer/Shaders/fragment.frag");
        uint vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexCode);
        uint fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentCode);
        GL.CompileShader(vertexShader);
        GL.CompileShader(fragmentShader);
        GL.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int vStatusVert);
        GL.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out int vStatusFrag);
        if (vStatusVert != (int)GLEnum.True || vStatusFrag != (int)GLEnum.True)
            throw new Exception($"Shaders failed to compile: vertex - {GL.GetShaderInfoLog(vertexShader)}, fragment - {GL.GetShaderInfoLog(fragmentShader)}");
        Program = GL.CreateProgram();
        GL.AttachShader(Program, vertexShader);
        GL.AttachShader(Program, fragmentShader);
        GL.LinkProgram(Program);
        GL.GetProgram(Program, ProgramPropertyARB.LinkStatus, out int lStatus);
        if (lStatus != (int)GLEnum.True)
            throw new Exception("Program failed to link: " + GL.GetProgramInfoLog(Program));
        GL.DetachShader(Program, vertexShader);
        GL.DetachShader(Program, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
        const uint texCoordLoc = 1;
        GL.EnableVertexAttribArray(texCoordLoc);
        GL.VertexAttribPointer(texCoordLoc, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)(3 * sizeof(float)));
        const uint positionLoc = 0;
        GL.EnableVertexAttribArray(positionLoc);
        GL.VertexAttribPointer(positionLoc, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)0);
        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
    }

    public unsafe void UpdateProjectionMatrix()
    {
        float viewHeight = 2.0f;
        float viewWidth = viewHeight * _main.AspectRatio;

        _projection = Matrix4x4.CreateOrthographicOffCenter(
            -viewWidth/2, viewWidth/2,
            -viewHeight/2, viewHeight/2,
            -1.0f, 1.0f);

        GL.UseProgram(Program);
        int projLoc = GL.GetUniformLocation(Program, "uProjection");

        fixed (float* matrixPtr = &_projection.M11)
        {
            GL.UniformMatrix4(projLoc, 1, false, matrixPtr);
        }
    }
}
