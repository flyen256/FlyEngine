using System.Numerics;
using Silk.NET.OpenGL;

namespace Flyeng.UI;

public class Element : Component
{
    public Vector4 Color;

    public Element()
    {
        Application.Elements.Add(this);
    }

    public virtual unsafe void BeginDraw(OpenGL openGL)
    {
        GL gl = openGL.GL;
        Matrix4x4 model = Matrix4x4.CreateScale(Transform.Size.X, Transform.Size.Y, 1.0f) *
                Matrix4x4.CreateTranslation(Transform.Position.X, Transform.Position.Y, 0.0f);

        int modelLoc = gl.GetUniformLocation(openGL.Program, "uModel");
        int colorLoc = gl.GetUniformLocation(openGL.Program, "uColor");

        float* modelPtr = (float*)&model;
        gl.UniformMatrix4(modelLoc, 1, false, modelPtr);

        gl.Uniform4(colorLoc, Color);
        Draw(openGL);
    }

    public virtual unsafe void Draw(OpenGL openGL)
    {
        openGL.GL.BindVertexArray(openGL.vao);
        openGL.GL.UseProgram(openGL.Program);
        openGL.GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, (void*)0);
    }
}
