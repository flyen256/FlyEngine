using System.Numerics;

namespace FlyEngine.Components.Common;

public class Transform
{
    public Transform? Parent = null;
    public Vector3 Position;
    public Vector3 Size = Vector3.One;
    public Quaternion Rotation = Quaternion.Identity;
}
