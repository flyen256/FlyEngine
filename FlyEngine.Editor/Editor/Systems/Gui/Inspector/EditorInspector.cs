using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using FlyEngine.Core.Engine;
using FlyEngine.Core.Engine.Components.Common;
using FlyEngine.Core.Engine.Extensions;
using FlyEngine.Editor.Systems.Console;
using FlyEngine.Network;
using ImGuiNET;
using Microsoft.Extensions.Logging;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Editor.Systems.Gui;

public class EditorInspector : EditorGuiWindow
{
    private readonly ILogger _logger = new Logger<EditorInspector>(LoggerFactory.Create(b => b.AddConsole()));
    
    public static EditorInspector? Instance { get; private set; }
    
    protected override string Title => "Inspector";

    private GameObject? SelectedGameObject => EditorHierarchy.Instance?.SelectedGameObject;
    private GameObject? _lastSelected;
    private Vector3 _eulerRotation;

    private List<Type> _componentTypes = [];

    private bool _addComponentModal;

    private string _searchComponent = string.Empty;

    public EditorInspector()
    {
        Instance = this;
    }

    protected internal override void OnLoad()
    {
        Editor.OnCompileScripts += OnCompileScripts;
    }

    protected internal override void OnUnload()
    {
        Editor.OnCompileScripts -= OnCompileScripts;
    }

    private void OnCompileScripts()
    {
        RefreshComponents();
    }

    protected override void BeforeBegin()
    {
        ImGuiNet.SetNextWindowDockID(EditorGui.RightDockId);
    }

    protected override void OnRender(double deltaTime)
    {
        if (EditorHierarchy.Instance == null || SelectedGameObject == null) return;
        if (_lastSelected != SelectedGameObject)
        {
            _lastSelected = SelectedGameObject;
            _eulerRotation = SelectedGameObject.Transform.Rotation.ToEulerAngles();
        }
        RenderTransform();
        RenderComponents();
        if (ImGuiNet.Button("Add Component"))
            _addComponentModal = true;
        RenderAddComponentModal();
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

    private void AddComponent(Type type)
    {
        if (!type.IsSubclassOf(typeof(Component)))
        {
            EditorConsole.Instance?.Messages.Add(new EditorConsoleMessage
            {
                Level = LogLevel.Error,
                Message = $"Type: {type.Name} is not Component"
            });
            return;
        }
        SelectedGameObject?.AddComponent(type);
        EditorAction.MarkDirty();
    }

    private void RefreshComponents()
    {
        if (Editor.CurrentProjectPath == null) return;
        _componentTypes.Clear();
        var coreAssembly = Assembly.GetAssembly(typeof(Application));
        var editorAssembly = Assembly.GetAssembly(typeof(Editor));
        var networkAssembly = Assembly.GetAssembly(typeof(NetworkManager));
        var gameAssembly = Editor.ScriptLoader?.LoadFromAssemblyName(new AssemblyName(Application.ScriptsAssemblyName));
        if (coreAssembly == null || editorAssembly == null || networkAssembly == null || gameAssembly == null) return;
        _componentTypes.AddRange(coreAssembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Component))));
        _componentTypes.AddRange(editorAssembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Component))));
        _componentTypes.AddRange(networkAssembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Component))));
        _componentTypes.AddRange(gameAssembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Component))));
    }

    private List<Type> SearchComponents()
    {
        return _componentTypes.Where(c => Regex.IsMatch(c.Name, _searchComponent)).ToList();
    }

    private void RenderComponents()
    {
        if (SelectedGameObject == null) return;
        for (var i = 0; i < SelectedGameObject.ComponentStore.List.Count; i++)
        {
            var component = SelectedGameObject.ComponentStore.List[i];
            var componentEnabled = component.Enabled;
            var variables = GetComponentVariables(component);
            ImGuiNet.Checkbox($"###{component.GetType().Name + $"{i}"}", ref componentEnabled);
            ImGuiNet.SameLine();
            if (ImGuiNet.Button($"X##{component.SceneIndex}"))
            {
                component.Destroy();
                EditorAction.MarkDirty();
            }
            ImGuiNet.SameLine();
            if (ImGuiNet.CollapsingHeader($"{component.GetType().Name}###{component.GetType().Name + $"{i}"}header", ImGuiTreeNodeFlags.DefaultOpen))
            {
                foreach (var variableInfo in variables)
                    PropertyRenderer.Render(variableInfo, component);
            }

            if (component.Enabled != componentEnabled)
            {
                component.Enabled = componentEnabled;
                EditorAction.MarkDirty();
            }
        }
    }

    private Span<VariableInfo> GetComponentVariables(Component component)
    {
        var type = component.GetType();

        var properties = type.GetProperties()
            .Where(f => f.GetSetMethod(false) != null).Cast<MemberInfo>();
        var variables =
            type.GetFields().Concat(properties).Where(f => f.DeclaringType == type);

        return CollectionsMarshal.AsSpan(variables.Select(v => new VariableInfo(v)).ToList());
    }

    private void RenderTransform()
    {
        if (EditorHierarchy.Instance == null || SelectedGameObject == null) return;
        if (ImGuiNet.CollapsingHeader($"Transform##{SelectedGameObject.Name}", ImGuiTreeNodeFlags.AllowOverlap | ImGuiTreeNodeFlags.DefaultOpen))
        {
            var transform = SelectedGameObject.Transform;
            var pos = transform.Position;
            if (ImGuiNet.DragFloat3("Position", ref pos, 0.1f))
            {
                transform.Position = pos;
                EditorAction.MarkDirty();
            }

            var scale = transform.Scale;
            if (ImGuiNet.DragFloat3("Scale", ref scale, 0.1f))
            {
                transform.Scale = scale;
                EditorAction.MarkDirty();
            }

            var rotation = transform.Euler;
            if (ImGuiNet.DragFloat3("Rotation", ref rotation, 0.5f))
            {
                transform.Euler = rotation;
                transform.Rotation = QuaternionUtils.FromVector3(rotation);
                EditorAction.MarkDirty();
            }

            ImGui.Separator();
        }
    }
}