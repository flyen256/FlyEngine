using Silk.NET.OpenGL;

namespace FlyEngine.Core.Renderer;

public class MeshBudgetManager
{
    private readonly GL _gl;
    
    public uint Vao { get; private set; }
    public uint Vbo { get; private set; }
    public uint Ebo { get; private set; }

    private uint _currentVertexOffset;
    private uint _currentIndexOffset;

    private const uint MaxVertices = 10_000_000;
    private const uint MaxIndices = 30_000_000;
    
    private const uint StrideInFloats = 8;
    private const uint SizeOfFloat = 4;
    private const uint StrideInBytes = StrideInFloats * SizeOfFloat;

    public MeshBudgetManager(GL gl)
    {
        _gl = gl;
        InitBuffers();
    }

    private unsafe void InitBuffers()
    {
        Vao = _gl.GenVertexArray();
        _gl.BindVertexArray(Vao);

        Vbo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, Vbo);
        const UIntPtr vboSize = MaxVertices * StrideInBytes;
        _gl.BufferData(BufferTargetARB.ArrayBuffer, vboSize, null, BufferUsageARB.StaticDraw);

        Ebo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, Ebo);
        const UIntPtr eboSize = MaxIndices * SizeOfFloat;
        _gl.BufferData(BufferTargetARB.ElementArrayBuffer, eboSize, null, BufferUsageARB.StaticDraw);

        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, StrideInBytes, (void*)0);

        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, StrideInBytes, (void*)(3 * SizeOfFloat));

        _gl.EnableVertexAttribArray(2);
        _gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, StrideInBytes, (void*)(5 * SizeOfFloat));

        _gl.BindVertexArray(0);
    }

    public unsafe (int baseVertex, uint firstIndex) AllocateMesh(float[] vertexData, uint[] indexData)
    {
        var vertexCount = (uint)(vertexData.Length / StrideInFloats);
        var indexCount = (uint)indexData.Length;

        if (_currentVertexOffset + vertexCount > MaxVertices || _currentIndexOffset + indexCount > MaxIndices)
        {
            throw new Exception("MeshBudgetManager: Превышен лимит выделенной видеопамяти под геометрию!");
        }

        var baseVertex = (int)_currentVertexOffset;
        var firstIndex = _currentIndexOffset;

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, Vbo);
        fixed (float* ptr = vertexData)
        {
            var offsetInBytes = (nuint)(_currentVertexOffset * StrideInBytes);
            var sizeInBytes = (nuint)(vertexData.Length * SizeOfFloat);
            _gl.BufferSubData(BufferTargetARB.ArrayBuffer, (nint)offsetInBytes, sizeInBytes, ptr);
        }

        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, Ebo);
        fixed (uint* ptr = indexData)
        {
            var offsetInBytes = (nuint)(_currentIndexOffset * SizeOfFloat);
            var sizeInBytes = (nuint)(indexData.Length * SizeOfFloat);
            _gl.BufferSubData(BufferTargetARB.ElementArrayBuffer, (nint)offsetInBytes, sizeInBytes, ptr);
        }

        _currentVertexOffset += vertexCount;
        _currentIndexOffset += indexCount;

        return (baseVertex, firstIndex);
    }
}