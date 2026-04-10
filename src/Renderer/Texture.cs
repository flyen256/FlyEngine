using Silk.NET.OpenGL;
using StbImageSharp;

namespace FlyEngine.Renderer;

public class Texture
{
    public string Path { get; private set; }
    public uint Id { get; private set; }

    private readonly OpenGl _gl;

    public Texture(string path, OpenGl gl)
    {
        Id = gl.Gl.GenTexture();
        Path = path;
        _gl = gl;
        _gl.Textures.Add(this);
    }

    public unsafe void Load()
    {
        var gl = _gl.Gl;
        gl.BindTexture(TextureTarget.Texture2D, Id);
        var imageResult = LoadImage();
        if (imageResult == null)
        {
            gl.BindTexture(TextureTarget.Texture2D, 0);
            return;
        }
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

    private ImageResult? LoadImage()
    {
        var assembly = typeof(OpenGl).Assembly;
        var names = assembly.GetManifestResourceNames();
        var findName = names.ToList().Find(s => s.Contains(Path));
        if (findName == null)
            return null;
        var stream = assembly.GetManifestResourceStream(findName);
        if (stream == null)
            throw new Exception($"Resource {findName} not found!");
        return ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
    }

    public unsafe void UnLoad()
    {
        var gl = _gl.Gl;
        gl.DeleteTexture(Id);
    }
}
