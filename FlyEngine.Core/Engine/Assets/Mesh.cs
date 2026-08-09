using System.Numerics;
using FlyEngine.Core.Renderer;
using MemoryPack;
using Silk.NET.OpenGL;

namespace FlyEngine.Core.Assets;

[MemoryPackable]
public partial class Mesh : Asset
{
    public List<MeshVertex> Vertices { get; init; } = [];
    public List<uint> Indices { get; init; } = [];
    public uint IndexCount { get; init; }

    [MemoryPackIgnore] private VertexArrayObject<float, uint>? _vao;
    [MemoryPackIgnore] private BufferObject<uint>? _ebo;
    [MemoryPackIgnore] private BufferObject<float>? _vbo;

    [MemoryPackConstructor]
    private Mesh(Guid guid): base(guid) {}

    [MemoryPackOnDeserialized]
    private void OnDeserialized()
    {
        if (Application.Window?.OpenGl != null)
            SetupMesh(Application.Window.OpenGl.Gl);
        AssetsManager.AddAsset(this);
    }
    
    public Mesh(Guid guid, List<MeshVertex> vertices, List<uint> indices, uint indexCount, GL? gl = null) : base(guid)
    {
        Vertices = vertices;
        Indices = indices;
        IndexCount = indexCount;
        
        if (gl != null)
            SetupMesh(gl);
        
        AssetsManager.AddAsset(this);
    }

    public Mesh(Guid guid, GL gl, float[] vertices, uint[] indices, uint indexCount, int stride = 8) : base(guid)
    {
        IndexCount = indexCount;
        Indices = [.. indices];

        var meshVerticesList = new List<MeshVertex>();
        for (var i = 0; i < vertices.Length; i += stride)
        {
            var vertex = new MeshVertex
            {
                Position = new Vector3(vertices[i], vertices[i + 1], vertices[i + 2]),
                TextureCoordinates = stride >= 5 ? new Vector2(vertices[i + 3], vertices[i + 4]) : Vector2.Zero,
                Normal = stride >= 8 ? new Vector3(vertices[i + 5], vertices[i + 6], vertices[i + 7]) : Vector3.UnitY
            };
            meshVerticesList.Add(vertex);
        }
        Vertices = meshVerticesList;
        
        SetupMesh(gl);
        AssetsManager.AddAsset(this);
    }

    public override void Load(GL? gl = null)
    {
        if (gl == null)
            throw new NullReferenceException(nameof(gl));
            
        SetupMesh(gl);
    }

    private void SetupMesh(GL gl)
    {
        _vbo?.Dispose();
        _ebo?.Dispose();
        _vao?.Dispose();

        _vbo = new BufferObject<float>(gl, BuildVertices(), BufferTargetARB.ArrayBuffer);
        _ebo = new BufferObject<uint>(gl, BuildIndices(), BufferTargetARB.ElementArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(gl, _vbo, _ebo);

        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 8, 0);
        _vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 8, 3);
        _vao.VertexAttributePointer(2, 3, VertexAttribPointerType.Float, 8, 5);
        _vao.Unbind();
        
        Loaded = true;
    }

    public override void Unload()
    {
        _vbo?.Dispose();
        _ebo?.Dispose();
        _vao?.Dispose();
        base.Unload();
    }

    private float[] BuildVertices()
    {
        var vertices = new float[Vertices.Count * 8];
        var index = 0;

        foreach (var vertex in Vertices)
        {
            vertices[index++] = vertex.Position.X;
            vertices[index++] = vertex.Position.Y;
            vertices[index++] = vertex.Position.Z;
            
            vertices[index++] = vertex.TextureCoordinates.X;
            vertices[index++] = vertex.TextureCoordinates.Y;
            
            vertices[index++] = vertex.Normal.X;
            vertices[index++] = vertex.Normal.Y;
            vertices[index++] = vertex.Normal.Z;
        }

        return vertices;
    }

    private uint[] BuildIndices()
    {
        return Indices.ToArray();
    }

    public void Bind() => _vao?.Bind();

    public static Mesh Create(string name, Guid guid, GL gl, float[] vertices, uint[] indices, uint indexCount)
    {
        return new Mesh(guid, gl, vertices, indices, indexCount)
        {
            Name = name
        };
    }
}
