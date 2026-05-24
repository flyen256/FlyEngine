using FlyEngine.Core.Renderer;
using Silk.NET.OpenGL;

namespace FlyEngine.Core.Assets;

public class Mesh : Asset
{
    public readonly List<MeshVertex> Vertices = [];
    public readonly List<uint> Indices = [];
    public readonly uint IndexCount;

    public IReadOnlyList<Texture> Textures => _textures;
    
    private readonly List<Texture> _textures;
    private VertexArrayObject<float, uint> _vao;
    private BufferObject<uint> _ebo;
    private BufferObject<float> _vbo;
    
    public Mesh(Guid guid, List<Texture> textures, List<MeshVertex> vertices, List<uint> indices, uint indexCount) : base(guid)
    {
        Vertices = vertices;
        Indices = indices;
        _textures = textures;
        IndexCount = indexCount;
        AssetsManager.AddAsset(this);
    }
    
    public Mesh(Guid guid, GL gl, List<Texture> textures, float[] vertices, uint[] indices, uint indexCount) : base(guid)
    {
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
        AssetsManager.AddAsset(this);
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
    }

    public override void Unload()
    {
        _vbo.Dispose();
        _ebo.Dispose();
        _vao.Dispose();
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