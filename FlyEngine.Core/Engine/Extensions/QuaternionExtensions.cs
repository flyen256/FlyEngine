using System.Numerics;

namespace FlyEngine.Core.Extensions;

public static class QuaternionExtensions
{
    public static Vector3 ToEulerAngles(this Quaternion q)
    {
        Vector3 angles = new();

        var sinCos = 2 * (q.W * q.X + q.Y * q.Z);
        var cosCos = 1 - 2 * (q.X * q.X + q.Y * q.Y);
        angles.X = MathF.Atan2(sinCos, cosCos);

        var snip = 2 * (q.W * q.Y - q.Z * q.X);
        angles.Y = MathF.Abs(snip) >= 0.99999f ? MathF.CopySign(MathF.PI / 2, snip) : MathF.Asin(snip);

        var sinyCosp = 2 * (q.W * q.Z + q.X * q.Y);
        var cosyCosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
        angles.Z = MathF.Atan2(sinyCosp, cosyCosp);

        return angles * (180f / MathF.PI);
    }
}