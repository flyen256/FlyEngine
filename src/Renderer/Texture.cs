using Silk.NET.OpenGL;
using StbImageSharp;

namespace Flyeng;

public class Texture
{
    public string Path { get; private set; }
    public uint ID { get; private set; }

    private OpenGL _gl;

    public Texture(string path, OpenGL gl)
    {
        ID = gl.GL.GenTexture();
        Path = path;
        _gl = gl;
        _gl.Textures.Add(this);
    }

    public unsafe void Load()
    {
        var gl = _gl.GL;
        gl.BindTexture(TextureTarget.Texture2D, ID);
        ImageResult imageResult = ImageResult.FromMemory(File.ReadAllBytes(Path), ColorComponents.RedGreenBlueAlpha);
        fixed (byte* ptr = imageResult.Data)
            gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)imageResult.Width, (uint)imageResult.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
        var repeat = (int)TextureWrapMode.Repeat;
        var linear = (int)TextureMinFilter.Linear;
        gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, ref repeat);
        gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, ref repeat);
        gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, ref linear);
        gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, ref linear);

        gl.BindTexture(TextureTarget.Texture2D, 0);
    }

    public unsafe void UnLoad()
    {
        var gl = _gl.GL;
        gl.DeleteTexture(ID);
    }
}
