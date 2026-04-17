using System.Numerics;

namespace FlyEngine.Core.Engine.Extensions;

public static class QuaternionUtils
{
    public static Quaternion FromVector3(Vector3 vector)
    {
        const float toRad = MathF.PI / 180f;
        return Quaternion.CreateFromYawPitchRoll(vector.Y * toRad, vector.X * toRad, vector.Z * toRad);
    }
}