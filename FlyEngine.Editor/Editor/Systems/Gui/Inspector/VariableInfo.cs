using System.Reflection;

namespace FlyEngine.Editor.Systems.Gui;

public class VariableInfo(MemberInfo memberInfo)
{
    public Type? VariableType => FieldType ?? PropertyType;
    public string? Name => FieldInfo?.Name ?? PropertyInfo?.Name;
    
    private Type? PropertyType => PropertyInfo?.PropertyType;
    private Type? FieldType => FieldInfo?.FieldType;
    
    private FieldInfo? FieldInfo => memberInfo as FieldInfo;
    private PropertyInfo? PropertyInfo => memberInfo as PropertyInfo;

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