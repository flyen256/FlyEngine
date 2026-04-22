namespace FlyEngine.Core.Engine.CustomAttributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyRange(float min, float max) : Attribute
{
    public float Min { get; init; } = min;
    public float Max { get; init; } = max;
}