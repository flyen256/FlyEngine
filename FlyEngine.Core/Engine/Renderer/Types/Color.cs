using System.Numerics;

namespace FlyEngine.Core.Renderer.Types;

public struct Color
{
    private byte _red;
    private byte _green;
    private byte _blue;

    public byte Red
    {
        get => _red;
        set => _red = SetValueClamped(value);
    }

    public byte Green
    {
        get => _green;
        set => _green = SetValueClamped(value);
    }

    public byte Blue
    {
        get => _blue;
        set => _blue = SetValueClamped(value);
    }

    private static byte SetValueClamped(byte value) => (byte)System.Math.Clamp((float)value, 0, 255);
    
    public Color(byte red, byte green, byte blue)
    {
        Red = red;
        Green = green;
        Blue = blue;
    }
    
    public Vector3 ToVector3()
    {
        return new Vector3(Red / 255f, Green / 255f, Blue / 255f);
    }

    public static Color FromVector3(Vector3 value)
    {
        return new Color(
            (byte)System.Math.Round(value.X * 255f), 
            (byte)System.Math.Round(value.Y * 255f), 
            (byte)System.Math.Round(value.Z * 255f));
    }

    public static readonly Color White = new(255, 255, 255);
}