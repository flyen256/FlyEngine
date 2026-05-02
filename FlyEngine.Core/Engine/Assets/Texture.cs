using FlyEngine.Core.Renderer;
using Silk.NET.Assimp;
using Silk.NET.OpenGL;
using StbImageSharp;

namespace FlyEngine.Core.Assets;

public class Texture : Asset
{
    public AssimpString? AssimpPath { get; private set; }
    public TextureType Type { get; }

    private uint _handle;
    private readonly OpenGl _gl;
    
    public Texture(Guid guid, TextureType type, AssimpString path, OpenGl gl) : base(guid)
    {
        AssimpPath = path;
        Type = type;
        _gl = gl;
    }

    public Texture(Guid guid, TextureType type, string path, OpenGl gl) : base(guid)
    {
        Path = path;
        Type = type;
        _gl = gl;
    }

    public override unsafe void Load(GL? _ = null)
    {
        var gl = _gl.Gl;
        _handle = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, _handle);
        var imageResult = LoadImage() ?? LoadAssimpImage();
        if (imageResult != null)
        {
            gl.BindTexture(TextureTarget.Texture2D, 0);
            fixed (byte* ptr = imageResult.Data)
                gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)imageResult.Width, (uint)imageResult.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            SetParameters();
        }

        gl.BindTexture(TextureTarget.Texture2D, 0);
        base.Load(gl);
    }
    
    public void Bind(TextureUnit textureSlot = TextureUnit.Texture0)
    {
        _gl.Gl.ActiveTexture(textureSlot);
        _gl.Gl.BindTexture(TextureTarget.Texture2D, _handle);
    }

    private ImageResult? LoadImage()
    {
        if (Path == null) return null;
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
        if (AssimpPath == null) return null;
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

    public override void Unload()
    {
        var gl = _gl.Gl;
        gl.DeleteTexture(_handle);
        base.Unload();
    }
}