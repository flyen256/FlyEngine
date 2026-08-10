namespace FlyEngine.Core.CustomAttributes;

/// <summary>
/// Limits property or field number value in inspector and show it as slider
/// </summary>
/// <param name="min">Min value</param>
/// <param name="max">Max value</param>
/// <typeparam name="T">Number type</typeparam>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyRangeAttribute<T>(T min, T max) : Attribute
{
    public T Min { get; } = min;
    public T Max { get; } = max;
}