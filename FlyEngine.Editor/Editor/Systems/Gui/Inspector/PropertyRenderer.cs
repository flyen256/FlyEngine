using System.Numerics;
using System.Reflection;
using FlyEngine.Core.Assets;
using FlyEngine.Core.Components;
using FlyEngine.Core.CustomAttributes;
using FlyEngine.Core.Renderer;
using FlyEngine.Core.Serialization;
using Microsoft.Extensions.Logging;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Editor.Systems;

public class PropertyRenderer
{
    private static readonly ILogger Logger = new Logger<PropertyRenderer>(LoggerFactory.Create(b => b.AddConsole()));
    
    private delegate bool PropertyRendererDelegate(VariableInfo variableInfo, Component component, out bool changed);
    
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
            { typeof(bool), RenderBool },
            { typeof(ComponentRef<>), RenderComponentRef }
        };
    }

    public void Render(VariableInfo variableInfo, Component component)
    {
        if (variableInfo.VariableType == null) return;
        if (variableInfo.GetCustomAttribute(typeof(HideInInspectorAttribute), true) != null) return;

        var currentType = variableInfo.VariableType;

        if (currentType.IsGenericType)
        {
            var genericDefinition = currentType.GetGenericTypeDefinition();
        
            if (_types.TryGetValue(genericDefinition, out var genericRenderer))
            {
                genericRenderer(variableInfo, component, out var changed);
                return;
            }
        }

        while (currentType != null)
        {
            if (currentType.IsEnum &&
                _types.TryGetValue(typeof(Enum), out var renderer) ||
                _types.TryGetValue(currentType, out renderer))
            {
                renderer(variableInfo, component, out var changed);
                return;
            }

            currentType = currentType.BaseType;
        }
    }

    private static bool RenderFloat(VariableInfo variableInfo, Component component, out bool changed)
    {
        changed = false;
        if (variableInfo.GetValue(component) is not float f) return false;
        if (variableInfo.GetCustomAttribute(typeof(PropertyRangeAttribute<float>), true) is PropertyRangeAttribute<float> range)
            changed = ImGuiNet.DragFloat(variableInfo.DisplayName + "##slider", ref f, 1f, range.Min, range.Max, "%.2f");
        else
            changed = ImGuiNet.DragFloat(variableInfo.DisplayName + $"##{component.GetType().Name}", ref f);
        if (!changed) return false;
        variableInfo.SetValue(component, f);
        EditorAction.MarkDirty();
        return true;
    }
    
    private static bool RenderInt(VariableInfo variableInfo, Component component, out bool changed)
    {
        changed = false;
        if (variableInfo.GetValue(component) is not int i) return false;
        if (variableInfo.GetCustomAttribute(typeof(PropertyRangeAttribute<int>), true) is PropertyRangeAttribute<int> range)
            changed = ImGuiNet.DragInt(variableInfo.DisplayName + "##slider", ref i, 1f, range.Min, range.Max);
        else
            changed = ImGuiNet.DragInt(variableInfo.DisplayName + $"##{component.GetType().Name}", ref i);
        if (!changed) return false;
        variableInfo.SetValue(component, i);
        EditorAction.MarkDirty();
        return true;
    }

    private static bool RenderEnum(VariableInfo variableInfo, Component component, out bool changed)
    {
        changed = false;
        if (variableInfo.VariableType == null || variableInfo.GetValue(component) is not Enum e) return false;
        if (ImGuiNet.BeginCombo(variableInfo.DisplayName + $"##{component.GetType().Name}", e.ToString()))
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
        if (variableInfo.GetValue(component) is not Enum ee || Equals(e, ee)) return false;
        changed = true;
        variableInfo.SetValue(component, e);
        EditorAction.MarkDirty();
        return false;
    }
    
    private static bool RenderVector2(VariableInfo variableInfo, Component component, out bool changed)
    {
        changed = false;
        if (variableInfo.GetValue(component) is not Vector2 v2) return false;
        if (variableInfo.GetCustomAttribute(typeof(PropertyRangeAttribute<float>), true) is PropertyRangeAttribute<float> range)
            changed = ImGuiNet.DragFloat2(variableInfo.DisplayName + $"##{component.GetType().Name}", ref v2, 1f, range.Min, range.Max, "%.2f");
        else
            changed = ImGuiNet.DragFloat2(variableInfo.DisplayName + $"##{component.GetType().Name}", ref v2);
        if (variableInfo.GetValue(component) is not Vector2 vv2 || v2 == vv2) return false;
        changed = true;
        variableInfo.SetValue(component, v2);
        EditorAction.MarkDirty();
        return true;
    }

    private static bool RenderVector3(VariableInfo variableInfo, Component component, out bool changed)
    {
        changed = false;
        if (variableInfo.GetValue(component) is not Vector3 v3) return false;
        if (variableInfo.GetCustomAttribute(typeof(PropertyRangeAttribute<float>), true) is PropertyRangeAttribute<float> range)
            changed = ImGuiNet.DragFloat3(variableInfo.DisplayName + $"##{component.GetType().Name}", ref v3, 1f, range.Min, range.Max, "%.2f");
        else
            changed = ImGuiNet.DragFloat3(variableInfo.DisplayName + $"##{component.GetType().Name}", ref v3);
        if (variableInfo.GetValue(component) is not Vector3 vv3 || v3 == vv3) return false;
        variableInfo.SetValue(component, v3);
        EditorAction.MarkDirty();
        return true;
    }
    
    private static bool RenderColor(VariableInfo variableInfo, Component component, out bool changed)
    {
        changed = false;
        if (variableInfo.GetValue(component) is not Color c) return false;
        var vec = c.ToVector3();
        changed = ImGuiNet.ColorPicker3(variableInfo.DisplayName + $"##{component.GetType().Name}", ref vec);
        if (variableInfo.GetValue(component) is not Color cc || cc.ToVector3() == vec) return false;
        variableInfo.SetValue(component, Color.FromVector3(vec));
        EditorAction.MarkDirty();
        return false;
    }

    private bool RenderAsset(VariableInfo variableInfo, Component component, out bool changed)
    {
        changed = false;
        var label = $"Select Asset##{variableInfo.Name}{component.GetType().Name}";
        if (variableInfo.GetValue(component) is Asset asset)
            label = asset.Name + $"##{variableInfo.Name}{component.GetType().Name}";
        if (ImGuiNet.Button(label))
        {
            _inspector.OpenAssetSelector(variableInfo, component);
            _inspector.CurrentAssetsType = variableInfo.VariableType;
        }
        ImGuiNet.SameLine();
        ImGuiNet.Text(variableInfo.DisplayName);
        return false;
    }

    private static bool RenderBool(VariableInfo variableInfo, Component component, out bool changed)
    {
        changed = false;
        if (variableInfo.GetValue(component) is not bool b) return false;
        ImGuiNet.Checkbox(variableInfo.DisplayName + $"##{component.GetType().Name}", ref b);
        if (variableInfo.GetValue(component) is not bool bb || b == bb) return false;
        changed = true;
        variableInfo.SetValue(component, b);
        EditorAction.MarkDirty();
        return false;
    }

    private static bool RenderComponentRef(VariableInfo variableInfo, Component component, out bool changed)
    {
        changed = false;
        var refType = variableInfo.VariableType;
        if (refType == null) return false;

        var componentType = refType.GetGenericArguments()[0];
        var currentRefInstance = variableInfo.GetValue(component);
        
        Component? currentComponent = null;
        if (currentRefInstance != null)
        {
            var valueProperty = refType.GetProperty("Value");
            currentComponent = valueProperty?.GetValue(currentRefInstance) as Component;
        }

        var buttonText = currentComponent != null 
            ? $"{currentComponent.GameObject.Name} ({currentComponent.GetType().Name})" 
            : $"None ({componentType.Name})";

        var label = $"{buttonText}##{variableInfo.Name}_{component.GetHashCode()}";

        if (ImGuiNet.Button(label))
        {
        }

        if (ImGuiNet.BeginDragDropTarget())
        {
            unsafe
            {
                var payload = ImGuiNet.AcceptDragDropPayload("GAMEOBJECT_NODE");
                if (payload.NativePtr != null)
                {
                    var draggedOuter = *(GameObject*)payload.Data;
                    var foundComponent = draggedOuter.GetComponent(componentType);
            
                    if (foundComponent != null)
                    {
                        var valueProperty = refType.GetProperty("Value");

                        if (currentRefInstance == null)
                        {
                            currentRefInstance = Activator.CreateInstance(
                                refType, 
                                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, 
                                null,
                                [foundComponent], 
                                null);

                            if (currentRefInstance != null) variableInfo.SetValue(component, currentRefInstance);
                        }
                        else
                            valueProperty?.SetValue(currentRefInstance, foundComponent);
                
                        changed = true;
                        EditorAction.MarkDirty();
                    }
                }
                ImGuiNet.EndDragDropTarget();
            }
        }

        ImGuiNet.SameLine();
        ImGuiNet.Text(variableInfo.DisplayName);

        return changed;
    }
}