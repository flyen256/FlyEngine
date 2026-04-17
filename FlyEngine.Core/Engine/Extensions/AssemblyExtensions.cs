using System.Reflection;

namespace FlyEngine.Core.Engine.Extensions;

public static class AssemblyExtensions
{
    public static byte[] GetManifestResourceMemory(this Assembly assembly, string name)
    {
        using var resFilestream = assembly.GetManifestResourceStream(name);
        if (resFilestream == null) return [];
        var ba = new byte[resFilestream.Length];
        resFilestream.ReadExactly(ba, 0, ba.Length);
        return ba;
    }
}