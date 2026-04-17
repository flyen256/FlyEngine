using Silk.NET.Assimp;
using Silk.NET.OpenGL;
using StbImageSharp;
using TextureWrapMode = Silk.NET.OpenGL.TextureWrapMode;

namespace FlyEngine.Core.Engine.Renderer;

public class Texture
{
    public string? Path { get; private set; }
    public AssimpString? AssimpPath { get; private set; }
    public uint Id { get; private set; }

    private readonly OpenGl _gl;
    
    public Texture(AssimpString path, OpenGl gl)
    {
        Id = gl.Gl.GenTexture();
        AssimpPath = path;
        _gl = gl;
        _gl.Textures.Add(this);
        Console.WriteLine(path.AsString);
        
        // Bind();
        //
        // LoadAssimpImage();
    }

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
        SetParameters();

        gl.BindTexture(TextureTarget.Texture2D, 0);
    }
    
    public void Bind(TextureUnit textureSlot = TextureUnit.Texture0)
    {
        _gl.Gl.ActiveTexture(textureSlot);
        _gl.Gl.BindTexture(TextureTarget.Texture2D, Id);
    }

    private ImageResult? LoadImage()
    {
        var assembly = typeof(OpenGl).Assembly;
        var names = assembly.GetManifestResourceNames();
        var findName = names.ToList().Find(s => s.Contains(Path));
        if (findName == null)
            return null;
        var stream = assembly.GetManifestResourceStream(findName);
        return stream == null ? throw new Exception($"Resource {findName} not found!") : ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
    }
    
    private ImageResult? LoadAssimpImage()
    {

        return null;
    }
    
    private void SetParameters()
    {
        _gl.Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) GLEnum.ClampToEdge);
        _gl.Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) GLEnum.ClampToEdge);
        _gl.Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) GLEnum.LinearMipmapLinear);
        _gl.Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) GLEnum.Linear);
        _gl.Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
        _gl.Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 8);
        _gl.Gl.GenerateMipmap(TextureTarget.Texture2D);
    }

    public unsafe void UnLoad()
    {
        var gl = _gl.Gl;
        gl.DeleteTexture(Id);
    }
}
