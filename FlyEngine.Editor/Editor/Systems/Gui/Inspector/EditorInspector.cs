using FlyEngine.Core;
using FlyEngine.Core.Assets;
using FlyEngine.Core.Components;
using ImGuiNet = ImGuiNET.ImGui;
using Object = FlyEngine.Core.Assets.Object;

namespace FlyEngine.Editor.Systems;

public class EditorInspector : EditorGuiWindow
{
    protected override string Title => "Inspector";
    
    public Object? LastSelectedObject;

    private static Object? SelectedObject => Selection.SelectedObject;
    private readonly Dictionary<Type, Inspector> _inspectors;

    public EditorInspector()
    {
        _inspectors = new Dictionary<Type, Inspector>()
        {
            { typeof(GameObject), new GameObjectInspector(this) }
        };
        foreach (var inspector in _inspectors.Values)
            inspector.Initialize();
    }

    protected internal override void OnLoad()
    {
        foreach (var inspector in _inspectors.Values)
            inspector.OnLoad();
    }

    protected internal override void OnUnload()
    {
        foreach (var inspector in _inspectors.Values)
            inspector.OnUnload();
    }

    protected override void BeforeBegin()
    {
        ImGuiNet.SetNextWindowDockID(EditorGui.RightDockId);
    }

    protected override void OnRender(double deltaTime)
    {
        if (EditorHierarchy.Instance == null || SelectedObject == null) return;
        var currentType = SelectedObject.GetType();
        if (currentType.IsGenericType)
        {
            var genericDefinition = currentType.GetGenericTypeDefinition();
        
            if (_inspectors.TryGetValue(genericDefinition, out var inspector))
            {
                Render(inspector);
                return;
            }
        }
        
        while (currentType != null)
        {
            if (currentType.IsEnum &&
                _inspectors.TryGetValue(typeof(Enum), out var inspector) ||
                _inspectors.TryGetValue(currentType, out inspector))
            {
                Render(inspector);
                return;
            }

            currentType = currentType.BaseType;
        }
    }

    private void Render(Inspector inspector)
    {
        inspector.Render();
    }
}