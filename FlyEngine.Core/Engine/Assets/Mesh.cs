using System.Net.Mime;
using FlyEngine.Core.Renderer;
using MemoryPack;
using Silk.NET.OpenGL;

namespace FlyEngine.Core.Assets;

[MemoryPackable]
public partial class Mesh : Asset
{
    [MemoryPackInclude]
    public readonly List<MeshVertex> Vertices = [];
    [MemoryPackInclude]
    public readonly List<uint> Indices = [];
    [MemoryPackInclude]
    public readonly uint IndexCount;

    [MemoryPackIgnore]
    public IReadOnlyList<Texture> Textures => _textures;
    
    [MemoryPackIgnore]
    private readonly List<Texture> _textures;
    [MemoryPackIgnore]
    private VertexArrayObject<float, uint> _vao;
    [MemoryPackIgnore]
    private BufferObject<uint> _ebo;
    [MemoryPackIgnore]
    private BufferObject<float> _vbo;

    [MemoryPackConstructor]
    public Mesh(Guid guid, List<MeshVertex> vertices, List<uint> indices, uint indexCount) : base(guid)
    {
        if (Application.OpenGl == null)
            throw new NullReferenceException(nameof(Application.OpenGl));
        AssetsManager.AddAsset(this);
        var gl = Application.OpenGl.Gl;
        Vertices = vertices;
        Indices = indices;
        IndexCount = indexCount;
        _textures = [];
        _vbo = new BufferObject<float>(gl, BuildVertices(), BufferTargetARB.ArrayBuffer);
        _ebo = new BufferObject<uint>(gl, BuildIndices(), BufferTargetARB.ElementArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(gl, _vbo, _ebo);
        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 5, 0);
        _vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 5, 3);
        _vao.VertexAttributePointer(2, 3, VertexAttribPointerType.Float, 5, 0);
        _vao.Unbind();
        Loaded = true;
    }
    
    public Mesh(Guid guid, List<Texture> textures, List<MeshVertex> vertices, List<uint> indices, uint indexCount) : base(guid)
    {
        AssetsManager.AddAsset(this);
        Vertices = vertices;
        Indices = indices;
        _textures = textures;
        IndexCount = indexCount;
    }
    
    public Mesh(Guid guid, GL gl, List<Texture> textures, float[] vertices, uint[] indices, uint indexCount) : base(guid)
    {
        AssetsManager.AddAsset(this);
        IndexCount = indexCount;
        _textures = textures;
        _vbo = new BufferObject<float>(gl, vertices, BufferTargetARB.ArrayBuffer);
        _ebo = new BufferObject<uint>(gl, indices, BufferTargetARB.ElementArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(gl, _vbo, _ebo);
        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 8, 0);
        _vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 8, 3);
        _vao.VertexAttributePointer(2, 3, VertexAttribPointerType.Float, 8, 5);
        _vao.Unbind();
        Loaded = true;
    }

    public override void Load(GL? gl = null)
    {
        if (gl == null)
            throw new NullReferenceException(nameof(gl));
        _vbo = new BufferObject<float>(gl, BuildVertices(), BufferTargetARB.ArrayBuffer);
        _ebo = new BufferObject<uint>(gl, BuildIndices(), BufferTargetARB.ElementArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(gl, _vbo, _ebo);
        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 5, 0);
        _vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 5, 3);
        _vao.VertexAttributePointer(2, 3, VertexAttribPointerType.Float, 5, 0);
        _vao.Unbind();
        base.Load(gl);
    }

    public override void Unload()
    {
        _vbo.Dispose();
        _ebo.Dispose();
        _vao.Dispose();
        base.Unload();
    }

    private float[] BuildVertices()
    {
        var vertices = new List<float>();

        foreach (var vertex in Vertices)
        {
            vertices.Add(vertex.Position.X);
            vertices.Add(vertex.Position.Y);
            vertices.Add(vertex.Position.Z);
            vertices.Add(vertex.TextureCoordinates.X);
            vertices.Add(vertex.TextureCoordinates.Y);
        }

        return vertices.ToArray();
    }

    private float[] BuildVerticesWithoutTextureCoordinates()
    {
        var vertices = new List<float>();
        
        foreach (var vertex in Vertices)
        {
            vertices.Add(vertex.Position.X);
            vertices.Add(vertex.Position.Y);
            vertices.Add(vertex.Position.Z);
        }

        return vertices.ToArray();
    }

    private uint[] BuildIndices()
    {
        return Indices.ToArray();
    }

    public void Bind() => _vao.Bind();
}