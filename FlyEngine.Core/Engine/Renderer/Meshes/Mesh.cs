using Silk.NET.OpenGL;

namespace FlyEngine.Core.Renderer.Meshes;

public class Mesh
{
    public List<MeshVertex> Vertices = [];
    public List<uint> Indices = [];
    public uint IndexCount;
    
    public VertexArrayObject<float, uint> Vao;
    public BufferObject<uint> Ebo;
    public BufferObject<float> Vbo;

    private GL _gl;

    public Mesh(GL gl)
    {
        _gl = gl;
    }
    
    public Mesh(GL gl, List<MeshVertex> vertices, List<uint> indices, uint indexCount)
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
    
    public Mesh(GL gl, float[] vertices, uint[] indices, uint indexCount)
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