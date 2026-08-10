namespace FlyEngine.Core.CustomAttributes;

/// <summary>
/// Show private property or field in editor inspector
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ShowInInspectorAttribute : Attribute;