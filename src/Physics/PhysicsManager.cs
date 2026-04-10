using System.Drawing;
using FlyEngine.Components.Common;
using FlyEngine.Physics.Colliders;

namespace FlyEngine.Physics;

public class PhysicsManager : Behaviour
{
    public override void OnUpdate(double deltaTime)
    {
        var targetObject = Application.GameObjects.FindAll((g) => g.ComponentStore.GetComponent<Collider>() != null);
        for (var i = 0; i < targetObject.Count; i++)
        {
            for (var o = i + 1; o < targetObject.Count; o++)
            {
                var go1 = targetObject[i];
                var go2 = targetObject[o];
                if (go1.ComponentStore.GetComponent<Collider>() == null || go2.ComponentStore.GetComponent<Collider>() == null)
                    return;
                var firstCollider = go1.ComponentStore.GetComponent<Collider>();
                var secondCollider = go2.ComponentStore.GetComponent<Collider>();
                if (firstCollider == null || secondCollider == null)
                    return;
                var testCollision = TestCollision(
                    firstCollider,
                    secondCollider);
                if (testCollision.IsColliding)
                {
                    foreach (var c1 in go1.ComponentStore.List)
                        if (c1 is Behaviour behaviour)
                            behaviour.OnCollision(testCollision);
                    foreach (var c2 in go2.ComponentStore.List)
                        if (c2 is Behaviour behaviour1)
                            behaviour1.OnCollision(testCollision);
                }
            }
        }
    }

    private CollisionResult TestCollision(Collider a, Collider b)
    {
        var aBox = a.Collider2D;
        var bBox = b.Collider2D;

        var overlapX = Math.Min(aBox.Right - bBox.Left, bBox.Right - aBox.Left);
        var overlapY = Math.Min(aBox.Bottom - bBox.Top, bBox.Bottom - aBox.Top);

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
