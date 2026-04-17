using System.Numerics;

namespace FlyEngine.Core.Engine.Components.Common;

public class Transform
{
    public Transform? Parent = null;
    public Vector3 Position;
    public Vector3 Size = Vector3.One;
    public Quaternion Rotation = Quaternion.Identity;
    public Vector3 Forward => Vector3.Transform(new Vector3(0, 0, -1), Rotation);
}
