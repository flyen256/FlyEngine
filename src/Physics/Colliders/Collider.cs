using System.Drawing;
using Silk.NET.Maths;

namespace Flyeng;

public class Collider : Behaviour
{
    public Vector2D<float> ColliderSize2D;
    public Vector2D<float> ColliderCenter2D;
    public bool IsTrigger = false;

    public RectangleF Collider2D => new RectangleF(
        Position.X + ColliderCenter2D.X,
        Position.Y + ColliderCenter2D.Y,
        ColliderSize2D.X,
        ColliderSize2D.Y
    );

    public Collider(Vector2D<float> size, Vector2D<float> center, bool isTrigger = false)
    {
        ColliderSize2D = size;
        ColliderCenter2D = center;
        IsTrigger = isTrigger;
    }
}
