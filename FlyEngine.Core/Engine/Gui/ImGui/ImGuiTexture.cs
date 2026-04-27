using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace FlyEngine.Core.Gui.ImGui;

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
    this._gl = gl;
    MaxAniso.GetValueOrDefault();
    if (!MaxAniso.HasValue)
      MaxAniso = new float?(gl.GetFloat(GLEnum.MaxTextureMaxAnisotropy));
    this.Width = (uint) width;
    this.Height = (uint) height;
    this.InternalFormat = srgb ? SizedInternalFormat.Srgb8Alpha8 : SizedInternalFormat.Rgba8;
    this.MipmapLevels = !generateMipmaps ? 1U : (uint) (int) System.Math.Floor(System.Math.Log((double) System.Math.Max(this.Width, this.Height), 2.0));
    this.GlTexture = this._gl.GenTexture();
    this.Bind();
    PixelFormat format = PixelFormat.Bgra;
    this._gl.TexStorage2D(GLEnum.Texture2D, this.MipmapLevels, this.InternalFormat, this.Width, this.Height);
    this._gl.TexSubImage2D(GLEnum.Texture2D, 0, 0, 0, this.Width, this.Height, format, PixelType.UnsignedByte, (void*) data);
    if (generateMipmaps)
      this._gl.GenerateTextureMipmap(this.GlTexture);
    this.SetWrap(TextureCoordinate.S, TextureWrapMode.Repeat);
    this.SetWrap(TextureCoordinate.T, TextureWrapMode.Repeat);
    GL gl1 = this._gl;
    uint num = this.MipmapLevels - 1U;
    ref uint local = ref num;
    gl1.TexParameterI(GLEnum.Texture2D, TextureParameterName.TextureMaxLevel, ref local);
  }

  public void Bind() => this._gl.BindTexture(GLEnum.Texture2D, this.GlTexture);

  public void SetMinFilter(TextureMinFilter filter)
  {
    GL gl = this._gl;
    int num = (int) filter;
    ref int local = ref num;
    gl.TexParameterI(GLEnum.Texture2D, TextureParameterName.TextureMinFilter, ref local);
  }

  public void SetMagFilter(TextureMagFilter filter)
  {
    GL gl = this._gl;
    int num = (int) filter;
    ref int local = ref num;
    gl.TexParameterI(GLEnum.Texture2D, TextureParameterName.TextureMagFilter, ref local);
  }

  public void SetAnisotropy(float level)
  {
    this._gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMaxAnisotropy, Util.Clamp(level, 1f, MaxAniso.GetValueOrDefault()));
  }

  public void SetLod(int @base, int min, int max)
  {
    this._gl.TexParameterI(GLEnum.Texture2D, TextureParameterName.TextureLodBias, ref @base);
    this._gl.TexParameterI(GLEnum.Texture2D, TextureParameterName.TextureMinLod, ref min);
    this._gl.TexParameterI(GLEnum.Texture2D, TextureParameterName.TextureMaxLod, ref max);
  }

  public void SetWrap(TextureCoordinate coord, TextureWrapMode mode)
  {
    GL gl = this._gl;
    int pname = (int) coord;
    int num = (int) mode;
    ref int local = ref num;
    gl.TexParameterI(GLEnum.Texture2D, (TextureParameterName) pname, ref local);
  }

  public void Dispose() => this._gl.DeleteTexture(this.GlTexture);
}