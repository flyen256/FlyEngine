using System.Drawing;
using Silk.NET.Maths;

namespace Flyeng;

public class PhysicsManager : Behaviour
{
    public override void OnUpdate(double deltaTime)
    {
        var targetObject = Application.GameObjects.FindAll((g) => g.Components.GetComponent<Collider>() != null);
        for (int i = 0; i < targetObject.Count; i++)
        {
            for (int o = i + 1; o < targetObject.Count; o++)
            {
                var go1 = targetObject[i];
                var go2 = targetObject[o];
                if (go1.Components.GetComponent<Collider>() == null || go2.Components.GetComponent<Collider>() == null)
                    return;
                var firstCollider = go1.Components.GetComponent<Collider>();
                var secondCollider = go2.Components.GetComponent<Collider>();
                if (firstCollider == null || secondCollider == null)
                    return;
                var testCollision = TestCollision(
                    firstCollider,
                    secondCollider);
                if (testCollision.IsColliding)
                {
                    foreach (var c1 in go1.Components.List)
                        if (c1 is Behaviour behaviour)
                            behaviour.OnCollision(testCollision);
                    foreach (var c2 in go2.Components.List)
                        if (c2 is Behaviour behaviour1)
                            behaviour1.OnCollision(testCollision);
                }
            }
        }
    }

    private CollisionResult TestCollision(Collider a, Collider b)
    {
        RectangleF aBox = a.Collider2D;
        RectangleF bBox = b.Collider2D;

        float overlapX = Math.Min(aBox.Right - bBox.Left, bBox.Right - aBox.Left);
        float overlapY = Math.Min(aBox.Bottom - bBox.Top, bBox.Bottom - aBox.Top);

        if (overlapX > 0 && overlapY > 0)
        {
            return new CollisionResult
            {
                IsColliding = true,
                OverlapX = overlapX,
                OverlapY = overlapY
            };
        }

        return new CollisionResult { IsColliding = false };
    }
}

public struct CollisionResult
{
    public bool IsColliding;
    public float OverlapX;
    public float OverlapY;
}
