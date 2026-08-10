namespace FlyEngine.Core.CustomAttributes;

/// <summary>
/// Hide property or field in inspector but still serialize it if it public
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class HideInInspectorAttribute : Attribute;