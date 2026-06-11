using System.Runtime.CompilerServices;
using Silk.NET.OpenGL;

namespace FlyEngine.Core.Gui.ImGui;

internal class ImGuiShader
{
  private readonly Dictionary<string, int> _uniformToLocation = new Dictionary<string, int>();
  private readonly Dictionary<string, int> _attribLocation = new Dictionary<string, int>();
  private bool _initialized;
  private GL _gl;
  private (ShaderType Type, string Path)[] _files;

  public uint Program { get; private set; }

  public ImGuiShader(GL gl, string vertexShader, string fragmentShader)
  {
    _gl = gl;
    _files = new (ShaderType, string)[2]
    {
      (ShaderType.VertexShader, vertexShader),
      (ShaderType.FragmentShader, fragmentShader)
    };
    Program = CreateProgram(_files);
  }

  public void UseShader() => _gl.UseProgram(Program);

  public void Dispose()
  {
    if (!_initialized)
      return;
    _gl.DeleteProgram(Program);
    _initialized = false;
  }

  public ImGuiUniformFieldInfo[] GetUniforms()
  {
    int @params;
    _gl.GetProgram(Program, GLEnum.ActiveUniforms, out @params);
    var uniforms = new ImGuiUniformFieldInfo[@params];
    for (var uniformIndex = 0; uniformIndex < @params; ++uniformIndex)
    {
      int size;
      UniformType type;
      var activeUniform = _gl.GetActiveUniform(Program, (uint) uniformIndex, out size, out type);
      ImGuiUniformFieldInfo uniformFieldInfo;
      uniformFieldInfo.Location = GetUniformLocation(activeUniform);
      uniformFieldInfo.Name = activeUniform;
      uniformFieldInfo.Size = size;
      uniformFieldInfo.Type = type;
      uniforms[uniformIndex] = uniformFieldInfo;
    }
    return uniforms;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public int GetUniformLocation(string uniform)
  {
    int uniformLocation;
    if (!_uniformToLocation.TryGetValue(uniform, out uniformLocation))
    {
      uniformLocation = _gl.GetUniformLocation(Program, uniform);
      _uniformToLocation.Add(uniform, uniformLocation);
    }
    return uniformLocation;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public int GetAttribLocation(string attrib)
  {
    int attribLocation;
    if (!_attribLocation.TryGetValue(attrib, out attribLocation))
    {
      attribLocation = _gl.GetAttribLocation(Program, attrib);
      _attribLocation.Add(attrib, attribLocation);
    }
    return attribLocation;
  }

  private uint CreateProgram(
    params (ShaderType Type, string source)[] shaderPaths)
  {
    var program = _gl.CreateProgram();
    Span<uint> span1 = stackalloc uint[shaderPaths.Length];
    for (var index = 0; index < shaderPaths.Length; ++index)
      span1[index] = CompileShader(shaderPaths[index].Type, shaderPaths[index].source);
    var span2 = span1;
    for (var index = 0; index < span2.Length; ++index)
    {
      var shader = span2[index];
      _gl.AttachShader(program, shader);
    }
    _gl.LinkProgram(program);
    int @params;
    _gl.GetProgram(program, GLEnum.LinkStatus, out @params);
    if (@params == 0)
      _gl.GetProgramInfoLog(program);
    var span3 = span1;
    for (var index = 0; index < span3.Length; ++index)
    {
      var shader = span3[index];
      _gl.DetachShader(program, shader);
      _gl.DeleteShader(shader);
    }
    _initialized = true;
    return program;
  }

  private uint CompileShader(ShaderType type, string source)
  {
    var shader = _gl.CreateShader(type);
    _gl.ShaderSource(shader, source);
    _gl.CompileShader(shader);
    int @params;
    _gl.GetShader(shader, ShaderParameterName.CompileStatus, out @params);
    if (@params == 0)
      _gl.GetShaderInfoLog(shader);
    return shader;
  }
}