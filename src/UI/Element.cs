using System.Numerics;
using FlyEngine.Components.Common;
using FlyEngine.Renderer;
using Silk.NET.OpenGL;

namespace FlyEngine.UI;

public class Element : Component
{
    public Vector4 Color;

    public Element()
    {
        Application.Elements.Add(this);
    }

    public virtual unsafe void BeginDraw(OpenGl openGl)
    {
        var gl = openGl.Gl;
        var model = Matrix4x4.CreateScale(Transform.Size.X, Transform.Size.Y, 1.0f) *
                    Matrix4x4.CreateTranslation(Transform.Position.X, Transform.Position.Y, 0.0f);

        var modelLoc = gl.GetUniformLocation(openGl.Program, "uModel");
        var colorLoc = gl.GetUniformLocation(openGl.Program, "uColor");

        var modelPtr = (float*)&model;
        gl.UniformMatrix4(modelLoc, 1, false, modelPtr);

        gl.Uniform4(colorLoc, Color);
        Draw(openGl);
    }

    public virtual unsafe void Draw(OpenGl openGl)
    {
        openGl.Gl.BindVertexArray(openGl.Vao);
        openGl.Gl.UseProgram(openGl.Program);
        openGl.Gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, (void*)0);
    }
}
