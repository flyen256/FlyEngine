using System.Numerics;

namespace FlyEngine.Core.Engine.Extensions;

public static class Vector3Extensions
{
    public static Vector3 Normalize(this Vector3 vector)
    {
        var normalize = Vector3.Normalize(vector);
        vector = normalize;
        return vector;
    } 
}