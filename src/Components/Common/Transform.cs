using System.Drawing;
using FlyEngine.Physics.Colliders;
using Silk.NET.Maths;

namespace FlyEngine.Components.Common;

public class Transform : Component
{
    public Transform? Parent = null;
    private Vector3D<float> _position = Vector3D<float>.Zero;
    public Vector3D<float> Position
    {
        get => _position;
        set => OnPositionChanged(value);
    }
    public Vector3D<float> Size = Vector3D<float>.Zero;
    public Quaternion<float> Rotation = Quaternion<float>.Identity;

    public void OnPositionChanged(Vector3D<float> newPosition)
    {
        var collider = GetComponent<Collider>();
        if(collider == null || collider.IsTrigger)
        {
            _position = newPosition;
            return;
        }
        var testPosX = new Vector3D<float>(
            newPosition.X,
            _position.Y,
            _position.Z);

        var canMoveX = true;
        var testBoxX = new RectangleF(
            testPosX.X,
            testPosX.Y,
            Size.X,
            Size.Y);

        var targetObjects = Application.GameObjects.FindAll((g) => g.ComponentStore.GetComponent<Collider>() != null);
        foreach (var obj in targetObjects)
        {
            var objCollider = obj.ComponentStore.GetComponent<Collider>();
            if(objCollider == null) return;
            if (obj != GameObject &&
                testBoxX.IntersectsWith(objCollider.Collider2D))
            {
                canMoveX = false;
                break;
            }
        }

        var testPosY = new Vector3D<float>(
            Position.X,
            newPosition.Y,
            Position.Z
        );

        var canMoveY = true;
        var testBoxY = new RectangleF(
            testPosY.X,
            testPosY.Y,
            Size.X,
            Size.Y);

        foreach (var obj in targetObjects)
        {
            var objCollider = obj.ComponentStore.GetComponent<Collider>();
            if(objCollider == null) return;
            if (obj != GameObject && objCollider != null &&
                testBoxY.IntersectsWith(objCollider.Collider2D))
            {
                canMoveY = false;
                break;
            }
        }

        if (canMoveX)
            _position.X = newPosition.X;
        if(canMoveY)
            _position.Y = newPosition.Y;
    }
}
