using System.Numerics;
using FlyEngine.Core.Renderer;
using MemoryPack;
using Silk.NET.OpenGL;

namespace FlyEngine.Core.Assets;

[MemoryPackable]
public partial class SubMesh
{
    public uint IndexCount { get; init; }
    [MemoryPackIgnore] public int GlobalBaseVertexOffset { get; init; }
    [MemoryPackIgnore] public uint GlobalFirstIndexOffset { get; init; }

    [MemoryPackConstructor]
    public SubMesh() { }
}

public class Mesh : Asset
{
    public List<MeshVertex> Vertices { get; }
    public List<uint> Indices { get; }
    public List<SubMesh> SubMeshes { get; } = [];
    
    private readonly uint _indexCount;
    private readonly List<Texture> _textures = [];
    
    public IReadOnlyList<Texture> Textures => _textures;

    public Mesh(
        Guid guid,
        string name,
        List<Texture> textures,
        List<MeshVertex> vertices,
        List<uint> indices,
        uint indexCount,
        OpenGl? openGl = null) : base(guid)
    {
        Name = name;
        _textures = textures;
        Vertices = vertices;
        Indices = indices;
        _indexCount = indexCount;
        
        if (openGl != null)
            RegisterInBudgetManager(openGl);
        
        AssetsManager.AddAsset(this);
    }

    private Mesh(
        Guid guid,
        string name,
        OpenGl openGl,
        float[] vertices,
        uint[] indices,
        uint indexCount, 
        int stride = 8) : base(guid)
    {
        Name = name;
        _indexCount = indexCount;
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
        
        RegisterInBudgetManager(openGl);
        AssetsManager.AddAsset(this);
    }

    public override void Load(GL? gl = null)
    {
        if (Application.Window?.OpenGl != null)
            RegisterInBudgetManager(Application.Window.OpenGl);
        else
            throw new NullReferenceException("OpenGl контекст не инициализирован при вызове Load");
    }

    private void RegisterInBudgetManager(OpenGl openGl)
    {
        var budgetManager = openGl.MeshBudgetManager;
        SubMeshes.Clear();

        var vertexData = BuildVertices();
        var indexData = Indices.ToArray();

        var allocation = budgetManager.AllocateMesh(vertexData, indexData);

        var mainSubMesh = new SubMesh
        {
            IndexCount = _indexCount,
            GlobalBaseVertexOffset = allocation.baseVertex,
            GlobalFirstIndexOffset = allocation.firstIndex
        };
        SubMeshes.Add(mainSubMesh);
        
        Loaded = true;
    }

    public override void Unload()
    {
        Loaded = false;
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

    public static Mesh Create(string name, Guid guid, OpenGl openGl, float[] vertices, uint[] indices, uint indexCount)
    {
        return new Mesh(guid, name, openGl, vertices, indices, indexCount);
    }
}