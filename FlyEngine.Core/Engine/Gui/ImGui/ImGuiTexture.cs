using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace FlyEngine.Core.Gui;

internal class ImGuiTexture : IDisposable
{
  public const SizedInternalFormat Srgb8Alpha8 = SizedInternalFormat.Srgb8Alpha8;
  public const SizedInternalFormat Rgb32F = SizedInternalFormat.Rgb32f;
  public const GLEnum MaxTextureMaxAnisotropy = GLEnum.MaxTextureMaxAnisotropy;
  public static float? MaxAniso;
  private readonly GL _gl;
  public readonly string Name;
  public readonly uint GlTexture;
  public readonly uint Width;
  public readonly uint Height;
  public readonly uint MipmapLevels;
  public readonly SizedInternalFormat InternalFormat;

  public unsafe ImGuiTexture(
    GL gl,
    int width,
    int height,
    IntPtr data,
    bool generateMipmaps = false,
    bool srgb = false)
  {
    _gl = gl;
    MaxAniso.GetValueOrDefault();
    if (!MaxAniso.HasValue)
      MaxAniso = new float?(gl.GetFloat(GLEnum.MaxTextureMaxAnisotropy));
    Width = (uint) width;
    Height = (uint) height;
    InternalFormat = srgb ? SizedInternalFormat.Srgb8Alpha8 : SizedInternalFormat.Rgba8;
    MipmapLevels = !generateMipmaps ? 1U : (uint) (int) System.Math.Floor(System.Math.Log((double) System.Math.Max(Width, Height), 2.0));
    GlTexture = _gl.GenTexture();
    Bind();
    var format = PixelFormat.Bgra;
    _gl.TexStorage2D(GLEnum.Texture2D, MipmapLevels, InternalFormat, Width, Height);
    _gl.TexSubImage2D(GLEnum.Texture2D, 0, 0, 0, Width, Height, format, PixelType.UnsignedByte, (void*) data);
    if (generateMipmaps)
      _gl.GenerateTextureMipmap(GlTexture);
    SetWrap(TextureCoordinate.S, TextureWrapMode.Repeat);
    SetWrap(TextureCoordinate.T, TextureWrapMode.Repeat);
    var gl1 = _gl;
    var num = MipmapLevels - 1U;
    ref var local = ref num;
    gl1.TexParameterI(GLEnum.Texture2D, TextureParameterName.TextureMaxLevel, ref local);
  }

  public void Bind() => _gl.BindTexture(GLEnum.Texture2D, GlTexture);

  public void SetMinFilter(TextureMinFilter filter)
  {
    var gl = _gl;
    var num = (int) filter;
    ref var local = ref num;
    gl.TexParameterI(GLEnum.Texture2D, TextureParameterName.TextureMinFilter, ref local);
  }

  public void SetMagFilter(TextureMagFilter filter)
  {
    var gl = _gl;
    var num = (int) filter;
    ref var local = ref num;
    gl.TexParameterI(GLEnum.Texture2D, TextureParameterName.TextureMagFilter, ref local);
  }

  public void SetAnisotropy(float level)
  {
    _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMaxAnisotropy, Util.Clamp(level, 1f, MaxAniso.GetValueOrDefault()));
  }

  public void SetLod(int @base, int min, int max)
  {
    _gl.TexParameterI(GLEnum.Texture2D, TextureParameterName.TextureLodBias, ref @base);
    _gl.TexParameterI(GLEnum.Texture2D, TextureParameterName.TextureMinLod, ref min);
    _gl.TexParameterI(GLEnum.Texture2D, TextureParameterName.TextureMaxLod, ref max);
  }

  public void SetWrap(TextureCoordinate coord, TextureWrapMode mode)
  {
    var gl = _gl;
    var pname = (int) coord;
    var num = (int) mode;
    ref var local = ref num;
    gl.TexParameterI(GLEnum.Texture2D, (TextureParameterName) pname, ref local);
  }

  public void Dispose() => _gl.DeleteTexture(GlTexture);
}