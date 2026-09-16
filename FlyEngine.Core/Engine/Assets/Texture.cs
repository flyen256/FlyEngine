using FlyEngine.Core.Renderer;
using MemoryPack;
using Silk.NET.OpenGL;

namespace FlyEngine.Core.Assets;

public enum TextureType
{
    Texture2D = 3553
}

[MemoryPackable]
public partial class Texture : Asset
{
    [MemoryPackInclude]
    public TextureType TextureType { get; set; } = TextureType.Texture2D;
    [MemoryPackInclude]
    public string? AssimpPath { get; }

    private readonly uint _width, _height;
    private readonly byte[] _data;
    private readonly OpenGl _openGl;
    
    public uint Handle { get; private set; }
    public ulong BindlessHandle { get; private set; }
    
    [MemoryPackConstructor]
    public Texture(Guid guid): base(guid) { }

    public Texture(
        Guid guid,
        byte[] data,
        uint width,
        uint height,
        OpenGl openGl,
        string assimpPath) : base(guid)
    {
        _data = data;
        _width = width;
        _height = height;
        _openGl = openGl;
        AssimpPath = assimpPath;
        AssetsManager.AddAsset(this);
    }

    public override unsafe void Load(GL? gl = null)
    {
        if (gl == null) return;
        Handle = gl.CreateTexture((TextureTarget)TextureType);
        gl.TextureStorage2D(Handle, 1, SizedInternalFormat.Rgba8, 1, 1);

        SetParameters();
        
        fixed (void* ptr = _data)
            gl.TextureSubImage2D(
                Handle, 
                0, 0, 0,
                _width, _height,
                PixelFormat.Rgba, PixelType.UnsignedByte,
                ptr);

        var bindless = _openGl.BindlessTexture.GetTextureHandle(Handle);
        _openGl.BindlessTexture.MakeTextureHandleResident(bindless);
        BindlessHandle = bindless;

        base.Load(gl);
    }
    
    private void SetParameters()
    {
        _openGl.Gl.TextureParameter(Handle, TextureParameterName.TextureWrapS, (int) GLEnum.ClampToEdge);
        _openGl.Gl.TextureParameter(Handle, TextureParameterName.TextureWrapT, (int) GLEnum.ClampToEdge);
        _openGl.Gl.TextureParameter(Handle, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);
        _openGl.Gl.TextureParameter(Handle, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);
        _openGl.Gl.TextureParameter(Handle, TextureParameterName.TextureBaseLevel, 0);
        _openGl.Gl.TextureParameter(Handle, TextureParameterName.TextureMaxLevel, 8);
        _openGl.Gl.GenerateTextureMipmap(Handle);
    }

    public override void Unload()
    {
        _openGl.Gl.DeleteTexture(Handle);
        base.Unload();
    }
}