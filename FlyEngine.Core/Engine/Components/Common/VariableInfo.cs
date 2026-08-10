using System.Reflection;
using System.Text;

namespace FlyEngine.Editor.Systems;

public class VariableInfo(MemberInfo memberInfo)
{
    public Type? VariableType => FieldType ?? PropertyType;
    public string? Name => FieldInfo?.Name ?? PropertyInfo?.Name;
    public string? DisplayName => GetDisplayName();
    
    private Type? PropertyType => PropertyInfo?.PropertyType;
    private Type? FieldType => FieldInfo?.FieldType;
    
    private FieldInfo? FieldInfo => memberInfo as FieldInfo;
    private PropertyInfo? PropertyInfo => memberInfo as PropertyInfo;

    private string? GetDisplayName()
    {
        if (Name == null) return null;

        var rawName = Name;
        if (rawName.StartsWith('_') && rawName.Length > 1)
            rawName = rawName[1..];
        else if (rawName.StartsWith("m_") && rawName.Length > 2)
            rawName = rawName[2..];

        if (string.IsNullOrEmpty(rawName)) return Name;

        var sb = new StringBuilder();

        sb.Append(char.ToUpper(rawName[0]));

        for (var i = 1; i < rawName.Length; i++)
        {
            var current = rawName[i];
            var previous = rawName[i - 1];

            if ((char.IsUpper(current) || char.IsDigit(current)) && !char.IsUpper(previous) && !char.IsDigit(previous))
                sb.Append(' ');
    
            sb.Append(current);
        }

        return sb.ToString();
    }
    
    public Attribute? GetCustomAttribute(Type attributeType, bool inherit)
    {
        return FieldInfo?.GetCustomAttribute(attributeType, inherit) ??
               PropertyInfo?.GetCustomAttribute(attributeType, inherit);
    }

    public object? GetValue(object instance)
    {
        if (FieldInfo != null)
            return FieldInfo.GetValue(instance);
        if (PropertyInfo != null)
            return PropertyInfo.GetValue(instance);
        return null;
    }
    
    public T? GetValue<T>(object instance) where T : class
    {
        if (FieldInfo != null)
            return FieldInfo.GetValue(instance) as T;
        if (PropertyInfo != null)
            return PropertyInfo.GetValue(instance) as T;
        return null;
    }

    public void SetValue(object instance, object value)
    {
        if (FieldInfo != null)
            FieldInfo.SetValue(instance, value);
        if (PropertyInfo != null)
            PropertyInfo.SetValue(instance, value);
    }
}