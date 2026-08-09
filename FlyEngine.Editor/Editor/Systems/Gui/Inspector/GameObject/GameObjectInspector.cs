using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using FlyEngine.Core;
using FlyEngine.Core.Components;
using FlyEngine.Core.Debugging;
using FlyEngine.Core.Extensions;
using FlyEngine.Core.SceneManagement;
using FlyEngine.Core.Utils;
using FlyEngine.Network;
using ImGuiNET;
using Microsoft.Extensions.Logging;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Editor.Systems;

public class GameObjectInspector(EditorInspector editorInspector) : Inspector(editorInspector)
{
    private readonly List<Type> _componentTypes = [];
    private bool _addComponentModal;
    private string _searchComponent = string.Empty;
    private PropertyRenderer _propertyRenderer = null!;
    
    private static GameObject SelectedObject => (Selection.SelectedObject as GameObject)!;
    
    public override void Initialize()
    {
        _propertyRenderer = new PropertyRenderer(EditorInspector);
    }

    public override void Render()
    {
        if (EditorInspector.LastSelectedObject != SelectedObject)
        {
            EditorInspector.LastSelectedObject = SelectedObject;
            ref var transform = ref SelectedObject.Transform;
            transform.Rotation.ToEulerAngles();
        }
        RenderTransform();
        RenderComponents();
        if (ImGuiNet.Button("Add Component"))
        {
            _searchComponent = string.Empty;
            RefreshComponents();
            _addComponentModal = true;
        }
        RenderAddComponentModal();
    }
    
    public override void OnLoad()
    {
        Editor.Scripts.OnCompileScripts += OnCompileScripts;
    }

    public override void OnUnload()
    {
        Editor.Scripts.OnCompileScripts -= OnCompileScripts;
    }
    
    private void OnCompileScripts()
    {
        RefreshComponents();
    }
    
    private void RenderAddComponentModal()
    {
        if (_addComponentModal)
            ImGuiNet.OpenPopup("AddComponent");

        var center = ImGuiNet.GetMainViewport().GetCenter();
        ImGuiNet.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        if (ImGuiNet.BeginPopupModal("AddComponent", ref _addComponentModal, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
        {
            ImGuiNet.InputText("Search", ref _searchComponent, 1024);

            if (ImGuiNet.BeginChild("AddComponentsList", new Vector2(0, 400)))
            {
                foreach (var componentType in SearchComponents())
                {
                    if (!ImGuiNet.Selectable(componentType.Name)) continue;
                    AddComponent(componentType);
                    _addComponentModal = false;
                    ImGuiNet.CloseCurrentPopup();
                }
                ImGuiNet.EndChild();
            }
            ImGuiNet.Spacing();

            ImGuiNet.EndPopup();
        }
    }

    private static void AddComponent(Type type)
    {
        if (type.IsAbstract) return;
        if (!type.IsSubclassOf(typeof(Component)))
        {
            Debug.LogError($"Type: {type.Name} is not Component");
            return;
        }
        var component = SelectedObject.AddComponent(type);
        if (component != null)
            SceneManager.CurrentScene?.RegisterComponent(component, SelectedObject);
        EditorAction.MarkDirty();
    }

    private void RefreshComponents()
    {
        if (Editor.CurrentProjectPath == null) return;
        _componentTypes.Clear();
        var coreAssembly = Assembly.GetAssembly(typeof(Application));
        var editorAssembly = Assembly.GetAssembly(typeof(Editor));
        var networkAssembly = Assembly.GetAssembly(typeof(NetworkManager));
        if (coreAssembly != null)
            _componentTypes.AddRange(coreAssembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Component))));
        if (editorAssembly != null)
            _componentTypes.AddRange(editorAssembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Component))));
        if (networkAssembly != null)
            _componentTypes.AddRange(networkAssembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Component))));
        if (Editor.Scripts.CompileError || !Editor.Scripts.Compiled) return;
        var gameAssembly = Application.ScriptsLoader.LoadFromAssemblyName(new AssemblyName(Core.Scripting.Scripting.ScriptsAssemblyName));
        _componentTypes.AddRange(gameAssembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Component))));
    }

    private List<Type> SearchComponents() =>
        _componentTypes.Where(c =>
                !c.IsAbstract &&
                Regex.IsMatch(c.Name.ToLower(), _searchComponent.ToLower())).ToList();

    private void RenderComponents()
    {
        for (var i = 0; i < SelectedObject.ComponentStore.List.Count; i++)
        {
            var component = SelectedObject.ComponentStore.List[i];
            var componentEnabled = component.Enabled;
            var variables = component.GetComponentVariables(component);
            ImGuiNet.Checkbox($"###{component.GetType().Name + $"{i}"}", ref componentEnabled);
            ImGuiNet.SameLine();
            if (ImGuiNet.Button($"X##{component.GetType().Name}-{component.SceneIndex}"))
            {
                component.Destroy();
                EditorAction.MarkDirty();
            }
            ImGuiNet.SameLine();
            if (ImGuiNet.CollapsingHeader($"{component.GetType().Name}###{component.GetType().Name + $"{i}"}header", ImGuiTreeNodeFlags.DefaultOpen))
            {
                foreach (var variableInfo in variables)
                    _propertyRenderer.Render(variableInfo, component);
            }

            if (component.Enabled != componentEnabled)
            {
                component.Enabled = componentEnabled;
                EditorAction.MarkDirty();
            }
        }
    }

    private void RenderTransform()
    {
        if (EditorHierarchy.Instance == null) return;
        if (ImGuiNet.CollapsingHeader($"Transform##{SelectedObject.Name}", ImGuiTreeNodeFlags.AllowOverlap | ImGuiTreeNodeFlags.DefaultOpen))
        {
            ref var transform = ref SelectedObject.Transform;
            var pos = transform.Position;
            if (ImGuiNet.DragFloat3("Position", ref pos, 0.1f))
            {
                transform.Position = pos;
                EditorAction.MarkDirty();
            }
            
            var rotation = transform.Euler;
            if (ImGuiNet.DragFloat3("Rotation", ref rotation, 0.5f))
            {
                transform.Euler = rotation;
                transform.Rotation = QuaternionUtils.FromVector3(rotation);
                EditorAction.MarkDirty();
            }

            var scale = transform.Scale;
            if (ImGuiNet.DragFloat3("Scale", ref scale, 0.1f))
            {
                transform.Scale = scale;
                EditorAction.MarkDirty();
            }

            ImGui.Separator();
        }
    }
}