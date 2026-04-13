namespace FlyEngine.Math;

public static class MathHelper
{
    public const float Pi = (float)System.Math.PI;
    public static float DegreesToRadians(float degrees) => degrees * Pi / 180f;
}