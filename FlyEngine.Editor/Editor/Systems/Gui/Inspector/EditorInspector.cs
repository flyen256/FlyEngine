using System.Numerics;
using System.Text.RegularExpressions;
using FlyEngine.Core;
using FlyEngine.Core.Assets;
using FlyEngine.Core.Components;
using ImGuiNET;
using ImGuiNet = ImGuiNET.ImGui;
using Object = FlyEngine.Core.Components.Object;

namespace FlyEngine.Editor.Systems;

public class EditorInspector : EditorGuiWindow
{
    protected override string Title => "Inspector";
    
    public static EditorInspector? Instance { get; private set; }
    
    public Object? LastSelectedObject;
    public Type? CurrentAssetsType { get; set; }

    private static Object? SelectedObject => Selection.SelectedObject;
    private readonly Dictionary<Type, Inspector> _inspectors;
    
    private bool _assetSelectorModal;
    private string _searchAsset = string.Empty;
    
    private VariableInfo? _selectedVariableInfo;
    private Component? _selectedComponent;
    
    private readonly List<Asset> _assets = [];

    public EditorInspector()
    {
        Instance = this;
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
        AssetsManager.OnAssetsChanged += OnReloadAssets;
    }

    protected internal override void OnUnload()
    {
        foreach (var inspector in _inspectors.Values)
            inspector.OnUnload();
        AssetsManager.OnAssetsChanged -= OnReloadAssets;
    }
    
    private void OnReloadAssets()
    {
        RefreshAssets();
    }
    
    private void RefreshAssets()
    {
        _assets.Clear();
        _assets.AddRange(AssetsManager.Assets);
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
        RenderAssetSelectorModal();
    }
    
    private void RenderAssetSelectorModal()
    {
        var center = ImGuiNet.GetMainViewport().GetCenter();
        ImGuiNet.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        if (ImGuiNet.BeginPopupModal("SelectAsset", ref _assetSelectorModal, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
        {
            ImGuiNet.InputText("Search", ref _searchAsset, 1024);

            var assets = SearchAssets();
            if (ImGuiNet.BeginChild("SelectAssetList", new Vector2(0, 400)))
            {
                for (var i = 0; i < assets.Count; i++)
                {
                    var asset = assets[i];
                    if (!ImGuiNet.Selectable(
                            (!string.IsNullOrEmpty(asset.Name) ?
                                asset.Name :
                                asset.Guid) + $"##Asset_{i}")) continue;
                    SelectAsset(asset);
                    _assetSelectorModal = false;
                    ImGuiNet.CloseCurrentPopup();
                }
                ImGuiNet.EndChild();
            }
            ImGuiNet.Spacing();

            ImGuiNet.EndPopup();
        }
    }
    
    private List<Asset> SearchAssets() =>
        _assets.Where(c =>
            (CurrentAssetsType == null || c.GetType().IsAssignableFrom(CurrentAssetsType)) &&
            Regex.IsMatch(c.Name.ToLower(), _searchAsset.ToLower())).ToList();
    
    private void SelectAsset(Asset asset)
    {
        if (_selectedVariableInfo == null || _selectedComponent == null) return;
        _selectedVariableInfo.SetValue(_selectedComponent, asset);
        _assetSelectorModal = false;
        _selectedVariableInfo = null;
        _selectedComponent = null;
        EditorAction.MarkDirty();
    }
    
    public void OpenAssetSelector(VariableInfo variableInfo, Component component)
    {
        _assetSelectorModal = true;
        ImGuiNet.OpenPopup("SelectAsset");
        _selectedVariableInfo = variableInfo;
        _selectedComponent = component;
    }
}