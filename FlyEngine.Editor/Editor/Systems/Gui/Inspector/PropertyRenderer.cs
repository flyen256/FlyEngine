using FlyEngine.Core.Engine.Components.Common;
using FlyEngine.Core.Engine.CustomAttributes;
using ImGuiNET;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Editor.Systems.Gui;

public static class PropertyRenderer
{
    private delegate object? PropertyRendererDelegate(VariableInfo variableInfo, Component component);
    
    private static readonly Dictionary<Type, PropertyRendererDelegate> Types = new()
    {
        { typeof(float), Float },
        // { typeof(int), Integer },
        // { typeof(bool), Boolean },
    };

    public static void Render(VariableInfo variableInfo, Component component)
    {
        if (variableInfo.VariableType == null) return;
        if (!Types.TryGetValue(variableInfo.VariableType, out var value)) return;
        value(variableInfo, component);
    }

    private static object? Float(VariableInfo variableInfo, Component component)
    {
        if (variableInfo.GetValue(component) is not float f) return null;
        if (variableInfo.GetCustomAttribute(typeof(PropertyRange), true) is PropertyRange range)
        {
            ImGuiNet.SliderFloat(variableInfo.Name + "slider", ref f, range.Min, range.Max);
            ImGuiNet.SameLine();
            ImGuiNet.DragFloat(variableInfo.Name + "drag", ref f);
        }
        else
            ImGuiNet.DragFloat(variableInfo.Name + $"##{component.GetType().Name}", ref f);

        if (variableInfo.GetValue(component) is not float ff || !(Math.Abs(ff - f) > 0.001f)) return f;
        variableInfo.SetValue(component, f);
        EditorAction.MarkDirty();
        return f;
    }

    // private static object? Integer(string name, object value)
    // {
    //     
    // }
    //
    // private static object? Boolean(string name, object value)
    // {
    //     
    // }
    //
    // private static object? String(string name, object value)
    // {
    //     
    // }
    //
    // private static object? Vector2(string name, object value)
    // {
    //     
    // }
    //
    // private static object? Vector3(string name, object value)
    // {
    //     
    // }
}