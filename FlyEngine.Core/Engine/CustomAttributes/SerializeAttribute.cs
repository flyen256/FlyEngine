using System.Text.Json.Serialization;

namespace FlyEngine.Core.CustomAttributes;

/// <summary>
/// Marks property or field for component data json serialization
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class SerializeAttribute : Attribute;