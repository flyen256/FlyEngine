using System.Numerics;
using FlyEngine.Renderer;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Texture = FlyEngine.Renderer.Texture;

namespace FlyEngine.Components.Common;

public class GameObject : Object
{
    public string Name;
    public readonly Transform Transform;
    public Texture? Texture { get; private set; }
    public Vector4 Color;

    public readonly ComponentStore ComponentStore;

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
        Transform = new Transform();
        Transform.GameObject = this;
        ComponentStore = new ComponentStore(this);
        Application.GameObjects.Add(this);
        Transform.Position = position;
        Transform.Size = size;
        Color = color;
        Texture = texture;
        if (components != null)
        {
            foreach (var component in components)
                ComponentStore.AddComponent(component);
        }
    }

    public virtual unsafe void BeginDraw(OpenGl openGl)
    {
        var model = Matrix4x4.CreateScale(Transform.Size.X, Transform.Size.Y, 1.0f) *
                    Matrix4x4.CreateTranslation(Transform.Position.X, Transform.Position.Y, 0.0f);

        var modelLoc = openGl.Gl.GetUniformLocation(openGl.Program, "uModel");
        var colorLoc = openGl.Gl.GetUniformLocation(openGl.Program, "uColor");

        var modelPtr = (float*)&model;
        openGl.Gl.UniformMatrix4(modelLoc, 1, false, modelPtr);

        openGl.Gl.Uniform4(colorLoc, Color);
        Draw(openGl);
    }

    public virtual unsafe void Draw(OpenGl openGl)
    {
        openGl.Gl.BindVertexArray(openGl.Vao);
        openGl.Gl.UseProgram(openGl.Program);
        if (Texture != null)
        {
            openGl.Gl.ActiveTexture(TextureUnit.Texture0);
            openGl.Gl.BindTexture(TextureTarget.Texture2D, Texture.Id);
        }
        openGl.Gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, (void*)0);
    }
}
