using System.Diagnostics;
using Silk.NET.OpenGL;

namespace FlyEngine.Core.Gui.ImGui;

internal static class Util
{
    public static float Clamp(float value, float min, float max)
    {
        if ((double) value < (double) min)
            return min;
        return (double) value <= (double) max ? value : max;
    }

    [Conditional("DEBUG")]
    public static void CheckGlError(this GL gl, string title)
    {
        int error = (int) gl.GetError();
    }
}