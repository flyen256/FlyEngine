using System.Numerics;
using Microsoft.Extensions.Logging;
using Silk.NET.OpenGL;

namespace FlyEngine.Core.Renderer;

public class Shader
{
    private readonly ILogger _logger = new Logger<Shader>(LoggerFactory.Create(builder => builder.AddConsole()));
    
    public readonly uint Handle;
    private readonly GL _gl;

    public Shader(GL gl, string vertexCode, string fragmentCode)
    {
        _gl = gl;

        var vertexInt = LoadShader(ShaderType.VertexShader, vertexCode);
        var fragmentInt = LoadShader(ShaderType.FragmentShader, fragmentCode);
        if (vertexInt < 0 || fragmentInt < 0)
        {
            _logger.LogError("Failed to load vertex and fragment shaders");
            return;
        }
        var vertex = (uint)vertexInt;
        var fragment = (uint)fragmentInt;
        Handle = _gl.CreateProgram();
        _gl.AttachShader(Handle, vertex);
        _gl.AttachShader(Handle, fragment);
        _gl.LinkProgram(Handle);
        _gl.GetProgram(Handle, GLEnum.LinkStatus, out var status);
        if (status == 0)
        {
            _logger.LogError("Program failed to link with error: {GetProgramInfoLog}", _gl.GetProgramInfoLog(Handle));
            return;
        }
        _gl.DetachShader(Handle, vertex);
        _gl.DetachShader(Handle, fragment);
        _gl.DeleteShader(vertex);
        _gl.DeleteShader(fragment);
    }
    
    public void Use()
    {
        _gl.UseProgram(Handle);
    }

    public void SetUniform(string name, int value)
    {
        var location = _gl.GetUniformLocation(Handle, name);
        if (location == -1)
        {
            _logger.LogError("{Name} uniform not found on shader.", name);
            return;
        }
        _gl.Uniform1(location, value);
    }

    public unsafe void SetUniform(string name, Matrix4x4 value)
    {
        var location = _gl.GetUniformLocation(Handle, name);
        if (location == -1)
        {
            _logger.LogError("{Name} uniform not found on shader.", name);
            return;
        }
        _gl.UniformMatrix4(location, 1, false, (float*) &value);
    }

    public void SetUniform(string name, float value)
    {
        var location = _gl.GetUniformLocation(Handle, name);
        if (location == -1)
        {
            _logger.LogError("{Name} uniform not found on shader.", name);
            return;
        }
        _gl.Uniform1(location, value);
    }

    public void SetUniform(string name, Vector3 value)
    {
        var location = _gl.GetUniformLocation(Handle, name);
        if (location == -1)
        {
            _logger.LogError("{Name} uniform not found on shader.", name);
            return;
        }
        _gl.Uniform3(location, value.X, value.Y, value.Z);
    }
    
    public void SetUniform(string name, Vector2 value)
    {
        var location = _gl.GetUniformLocation(Handle, name);
        if (location == -1)
        {
            _logger.LogError("{Name} uniform not found on shader.", name);
            return;
        }
        _gl.Uniform2(location, value.X, value.Y);
    }
    
    public void SetUniform(string name, Vector4 value)
    {
        var location = _gl.GetUniformLocation(Handle, name);
        if (location == -1)
        {
            _logger.LogError("{Name} uniform not found on shader.", name);
            return;
        }
        _gl.Uniform4(location, value.X, value.Y, value.Z, value.W);
    }
    
    private int LoadShader(ShaderType type, string code)
    {
        var handle = _gl.CreateShader(type);
        _gl.ShaderSource(handle, code);
        _gl.CompileShader(handle);
        var infoLog = _gl.GetShaderInfoLog(handle);
        if (string.IsNullOrWhiteSpace(infoLog)) return (int)handle;
        _logger.LogError("Error compiling shader of type {ShaderType}, failed with error {InfoLog}", type, infoLog);
        return -1;
    }
}