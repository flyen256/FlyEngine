using System.Numerics;
using System.Globalization;
using MemoryPack;

namespace FlyEngine.Core.Renderer.Common;

[MemoryPackable]
public partial struct Color
{
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }

    public Color(string hex)
    {
        hex = hex.StartsWith($"#") ? hex[1..] : hex;

        if (hex.Length != 6)
        {
            this = White;
            return;
        }

        R = byte.Parse(hex[..2], NumberStyles.HexNumber);
        G = byte.Parse(hex[2..4], NumberStyles.HexNumber);
        B = byte.Parse(hex[4..6], NumberStyles.HexNumber);
    }

    public Color(Vector3 rgb)
    {
        R = (byte)System.Math.Clamp(rgb.X, 0, 255);
        G = (byte)System.Math.Clamp(rgb.Y, 0, 255);
        B = (byte)System.Math.Clamp(rgb.Z, 0, 255);
    }

    public Color(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
    }

    public string ToHexadecimal() => $"#{R:X2}{G:X2}{B:X2}";

    public Vector3 ToVector3() =>
        new(R / 255f, G / 255f, B / 255f);

    public static Color FromVector3(Vector3 value) =>
        new(
            (byte)System.Math.Clamp(System.Math.Round(value.X * 255f), 0, 255),
            (byte)System.Math.Clamp(System.Math.Round(value.Y * 255f), 0, 255),
            (byte)System.Math.Clamp(System.Math.Round(value.Z * 255f), 0, 255));

    public static readonly Color White = new(255, 255, 255);
    public static readonly Color Black = new(0, 0, 0);
    public static readonly Color Red = new(255, 0, 0);
    public static readonly Color Green = new(0, 255, 0);
    public static readonly Color Blue = new(0, 0, 255);
}