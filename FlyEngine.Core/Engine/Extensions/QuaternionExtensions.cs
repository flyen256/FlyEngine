using System.Numerics;

namespace FlyEngine.Core.Extensions;

public static class QuaternionExtensions
{
    public static Vector3 ToEulerAngles(this Quaternion q)
    {
        var angles = new Vector3();

        var sinrCosp = 2 * (q.W * q.X + q.Y * q.Z);
        var cosrCosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
        angles.X = MathF.Atan2(sinrCosp, cosrCosp);

        var sinp = 2 * (q.W * q.Y - q.Z * q.X);
        if (MathF.Abs(sinp) >= 1)
            angles.Y = MathF.CopySign(MathF.PI / 2, sinp);
        else
            angles.Y = MathF.Asin(sinp);

        var sinyCosp = 2 * (q.W * q.Z + q.X * q.Y);
        var cosyCosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
        angles.Z = MathF.Atan2(sinyCosp, cosyCosp);
        
        angles *= 180f / MathF.PI;

        return angles;
    }
}