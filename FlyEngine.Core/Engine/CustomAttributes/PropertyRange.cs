namespace FlyEngine.Core.CustomAttributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyRange<T>(T min, T max) : Attribute
{
    public T Min { get; init; } = min;
    public T Max { get; init; } = max;
}