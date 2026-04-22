using FlyEngine.Core.Engine.Assets;
using Silk.NET.OpenGL;

namespace FlyEngine.Core.Engine.Renderer.Meshes;

public class Mesh : Asset
{
    public readonly List<MeshVertex> Vertices = [];
    public readonly List<uint> Indices = [];
    public readonly uint IndexCount;
    
    public readonly VertexArrayObject<float, uint> Vao;
    public readonly BufferObject<uint> Ebo;
    public readonly BufferObject<float> Vbo;

    private GL _gl;
    
    public Mesh(Guid guid, GL gl, List<MeshVertex> vertices, List<uint> indices, uint indexCount) : base(guid)
    {
        _gl = gl;
        Vertices = vertices;
        Indices = indices;
        IndexCount = indexCount;
        Vbo = new BufferObject<float>(gl, BuildVertices(), BufferTargetARB.ArrayBuffer);
        Ebo = new BufferObject<uint>(gl, BuildIndices(), BufferTargetARB.ElementArrayBuffer);
        Vao = new VertexArrayObject<float, uint>(gl, Vbo, Ebo);
        Vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 5, 0);
        Vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 5, 3);
        Vao.VertexAttributePointer(2, 3, VertexAttribPointerType.Float, 5, 0);
        Vao.Unbind();
    }
    
    public Mesh(Guid guid, GL gl, float[] vertices, uint[] indices, uint indexCount) : base(guid)
    {
        _gl = gl;
        IndexCount = indexCount;
        Vbo = new BufferObject<float>(gl, vertices, BufferTargetARB.ArrayBuffer);
        Ebo = new BufferObject<uint>(gl, indices, BufferTargetARB.ElementArrayBuffer);
        Vao = new VertexArrayObject<float, uint>(gl, Vbo, Ebo);
        Vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 8, 0);
        Vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 8, 3);
        Vao.VertexAttributePointer(2, 3, VertexAttribPointerType.Float, 8, 5);
        Vao.Unbind();
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

    public void Bind() => Vao.Bind();
}