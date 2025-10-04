using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Flyeng;

public class GameObject : Object
{
    public string Name;
    public Transform Transform;
    public Texture? Texture { get; private set; }
    public Vector4 Color;

    public Components Components;

    public GameObject() : this(default, default, default, null)
    { }

    public GameObject(
        Vector3D<float> position,
        Vector3D<float> size,
        Vector4 color,
        Texture? texture,
        Component[]? components = null,
        string name = "New game object"
    )
    {
        Name = name;
        Transform = new();
        Transform.GameObject = this;
        Components = new(this);
        Application.GameObjects.Add(this);
        Transform.Position = position;
        Transform.Size = size;
        Color = color;
        Texture = texture;
        if (components != null)
        {
            foreach (var component in components)
                Components.AddComponent(component);
        }
    }

    public virtual unsafe void BeginDraw(OpenGL openGL)
    {
        Matrix4x4 model = Matrix4x4.CreateScale(Transform.Size.X, Transform.Size.Y, 1.0f) *
            Matrix4x4.CreateTranslation(Transform.Position.X, Transform.Position.Y, 0.0f);

        int modelLoc = openGL.GL.GetUniformLocation(openGL.Program, "uModel");
        int colorLoc = openGL.GL.GetUniformLocation(openGL.Program, "uColor");

        float* modelPtr = (float*)&model;
        openGL.GL.UniformMatrix4(modelLoc, 1, false, modelPtr);

        openGL.GL.Uniform4(colorLoc, Color);
        Draw(openGL);
    }

    public virtual unsafe void Draw(OpenGL openGL)
    {
        openGL.GL.BindVertexArray(openGL.vao);
        openGL.GL.UseProgram(openGL.Program);
        if (Texture != null)
        {
            openGL.GL.ActiveTexture(TextureUnit.Texture0);
            openGL.GL.BindTexture(TextureTarget.Texture2D, Texture.ID);
        }
        openGL.GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, (void*)0);
    }
}
