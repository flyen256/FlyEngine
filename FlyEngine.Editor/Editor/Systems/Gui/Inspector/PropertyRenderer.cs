using System.Numerics;
using FlyEngine.Core.Assets;
using FlyEngine.Core.Components.Common;
using FlyEngine.Core.CustomAttributes;
using FlyEngine.Core.Renderer.Common;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Editor.Systems.Gui;

public class PropertyRenderer
{
    private delegate void PropertyRendererDelegate(VariableInfo variableInfo, Component component);
    
    private readonly Dictionary<Type, PropertyRendererDelegate> _types;
    private readonly EditorInspector _inspector;

    public PropertyRenderer(EditorInspector inspector)
    {
        _inspector = inspector;
        _types = new Dictionary<Type, PropertyRendererDelegate>
        {
            { typeof(float), RenderFloat },
            { typeof(int), RenderInt },
            { typeof(Enum), RenderEnum },
            { typeof(Vector2), RenderVector2 },
            { typeof(Vector3), RenderVector3 },
            { typeof(Color), RenderColor },
            { typeof(Asset), RenderAsset },
            { typeof(bool), RenderBool }
        };
    }

    public void Render(VariableInfo variableInfo, Component component)
    {
        if (variableInfo.VariableType == null) return;
        if (variableInfo.GetCustomAttribute(typeof(HideInInspector), true) != null) return;
        if (_types.TryGetValue(variableInfo.VariableType, out var value))
            value(variableInfo, component);
        if (variableInfo.VariableType.BaseType != null &&
            _types.TryGetValue(variableInfo.VariableType.BaseType, out var value2))
            value2(variableInfo, component);
    }

    private void RenderFloat(VariableInfo variableInfo, Component component)
    {
        if (variableInfo.GetValue(component) is not float f) return;
        if (variableInfo.GetCustomAttribute(typeof(PropertyRange<float>), true) is PropertyRange<float> range)
            ImGuiNet.DragFloat(variableInfo.Name + "##slider", ref f, 1f, range.Min, range.Max, "%.2f");
        else
            ImGuiNet.DragFloat(variableInfo.Name + $"##{component.GetType().Name}", ref f);
        if (variableInfo.GetValue(component) is not float ff || !(Math.Abs(ff - f) > 0.001f)) return;
        variableInfo.SetValue(component, f);
        EditorAction.MarkDirty();
    }
    
    private void RenderInt(VariableInfo variableInfo, Component component)
    {
        if (variableInfo.GetValue(component) is not int i) return;
        if (variableInfo.GetCustomAttribute(typeof(PropertyRange<int>), true) is PropertyRange<int> range)
            ImGuiNet.DragInt(variableInfo.Name + "##slider", ref i, 1f, range.Min, range.Max);
        else
            ImGuiNet.DragInt(variableInfo.Name + $"##{component.GetType().Name}", ref i);
        if (variableInfo.GetValue(component) is not int ii || ii == i) return;
        variableInfo.SetValue(component, i);
        EditorAction.MarkDirty();
    }

    private void RenderEnum(VariableInfo variableInfo, Component component)
    {
        if (variableInfo.VariableType == null) return;
        if (variableInfo.GetValue(component) is not Enum e) return;
        if (ImGuiNet.BeginCombo(variableInfo.Name + $"##{component.GetType().Name}", e.ToString()))
        {
            foreach (var state in Enum.GetValues(variableInfo.VariableType))
            {
                var isSelected = Equals(e, (Enum)state);
                if (ImGuiNet.Selectable(state.ToString(), isSelected))
                    e = (Enum)state;

                if (isSelected)
                    ImGuiNet.SetItemDefaultFocus();
            }
            ImGuiNet.EndCombo();
        }
        if (variableInfo.GetValue(component) is not Enum ee || Equals(e, ee)) return;
        variableInfo.SetValue(component, e);
        EditorAction.MarkDirty();
    }
    
    private void RenderVector2(VariableInfo variableInfo, Component component)
    {
        if (variableInfo.GetValue(component) is not Vector2 v2) return;
        if (variableInfo.GetCustomAttribute(typeof(PropertyRange<float>), true) is PropertyRange<float> range)
            ImGuiNet.DragFloat2(variableInfo.Name + $"##{component.GetType().Name}", ref v2, 1f, range.Min, range.Max, "%.2f");
        else
            ImGuiNet.DragFloat2(variableInfo.Name + $"##{component.GetType().Name}", ref v2);
        if (variableInfo.GetValue(component) is not Vector2 vv2 || v2 == vv2) return;
        variableInfo.SetValue(component, v2);
        EditorAction.MarkDirty();
    }

    private void RenderVector3(VariableInfo variableInfo, Component component)
    {
        if (variableInfo.GetValue(component) is not Vector3 v3) return;
        if (variableInfo.GetCustomAttribute(typeof(PropertyRange<float>), true) is PropertyRange<float> range)
            ImGuiNet.DragFloat3(variableInfo.Name + $"##{component.GetType().Name}", ref v3, 1f, range.Min, range.Max, "%.2f");
        else
            ImGuiNet.DragFloat3(variableInfo.Name + $"##{component.GetType().Name}", ref v3);
        if (variableInfo.GetValue(component) is not Vector3 vv3 || v3 == vv3) return;
        variableInfo.SetValue(component, v3);
        EditorAction.MarkDirty();
    }
    
    private void RenderColor(VariableInfo variableInfo, Component component)
    {
        if (variableInfo.GetValue(component) is not Color c) return;
        var vec = c.ToVector3();
        ImGuiNet.ColorPicker3(variableInfo.Name + $"##{component.GetType().Name}", ref vec);
        if (variableInfo.GetValue(component) is not Color cc || cc.ToVector3() == vec) return;
        variableInfo.SetValue(component, Color.FromVector3(vec));
        EditorAction.MarkDirty();
    }

    private void RenderAsset(VariableInfo variableInfo, Component component)
    {
        var label = $"Select Asset##{variableInfo.Name}{component.GetType().Name}";
        if (variableInfo.GetValue(component) is  Asset asset)
            label = asset.Name + $"##{variableInfo.Name}{component.GetType().Name}";
        if (ImGuiNet.Button(label))
            _inspector.OpenAssetSelector(variableInfo, component);
        ImGuiNet.SameLine();
        ImGuiNet.Text(variableInfo.Name);
    }

    private void RenderBool(VariableInfo variableInfo, Component component)
    {
        if (variableInfo.GetValue(component) is not bool b) return;
        ImGuiNet.Checkbox(variableInfo.Name + $"##{component.GetType().Name}", ref b);
        if (variableInfo.GetValue(component) is not bool bb || b == bb) return;
        variableInfo.SetValue(component, b);
        EditorAction.MarkDirty();
    }
}