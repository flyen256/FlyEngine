using System.Numerics;

namespace FlyEngine.Core.Math;

public readonly struct Aabb(Vector3 min, Vector3 max)
{
    public bool IntersectRay(Ray ray, out float t)
    {
        t = 0.0f;
        var tMin = (min.X - ray.Origin.X) / ray.Direction.X;
        var tMax = (max.X - ray.Origin.X) / ray.Direction.X;

        if (tMin > tMax) Swap(ref tMin, ref tMax);

        var tyMin = (min.Y - ray.Origin.Y) / ray.Direction.Y;
        var tyMax = (max.Y - ray.Origin.Y) / ray.Direction.Y;

        if (tyMin > tyMax) Swap(ref tyMin, ref tyMax);

        if ((tMin > tyMax) || (tyMin > tMax)) return false;

        if (tyMin > tMin) tMin = tyMin;
        if (tyMax < tMax) tMax = tyMax;

        var tzMin = (min.Z - ray.Origin.Z) / ray.Direction.Z;
        var tzMax = (max.Z - ray.Origin.Z) / ray.Direction.Z;

        if (tzMin > tzMax) Swap(ref tzMin, ref tzMax);

        if ((tMin > tzMax) || (tzMin > tMax)) return false;

        if (tzMin > tMin) tMin = tzMin;
        if (tzMax < tMax) tMax = tzMax;

        t = tMin;
        return tMax >= MathF.Max(0.0f, tMin);
    }
    private static void Swap(ref float a, ref float b) { (a, b) = (b, a);
    }
}
