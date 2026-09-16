using System.Numerics;
using System.Text.RegularExpressions;
using FlyEngine.Core.Assets;
using FlyEngine.Core.Components;
using ImGuiNET;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Editor.Systems;

public class AssetSelectorPopup : Popup
{
    protected override string Name => "Select Asset";

    public VariableInfo? VariableInfo { get; set; }
    public object? Object { get; set; }
    
    private List<Asset> _assets = [];
    private string _searchQuery = string.Empty;

    protected override void BeforeBegin()
    {
        var center = ImGuiNet.GetMainViewport().GetCenter();
        ImGuiNet.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
    }

    protected override void OnOpen()
    {
        _searchQuery = string.Empty;
        _assets = SearchAssets(_searchQuery);
    }

    protected override void OnRender()
    {
        if (ImGuiNet.InputText("Search", ref _searchQuery, 1024))
            _assets = SearchAssets(_searchQuery);
        if (ImGuiNet.BeginChild("SelectAssetList", new Vector2(0, 400)))
        {
            for (var i = 0; i < _assets.Count; i++)
            {
                var asset = _assets[i];
                if (!RenderItem(asset, i)) continue;
                SelectAsset(asset);
                Close();
            }
            ImGuiNet.EndChild();
        }
        ImGuiNet.Spacing();
    }

    private void SelectAsset(Asset asset)
    {
        if (VariableInfo == null || Object == null) return;
        VariableInfo.SetValue(Object, asset);
        VariableInfo = null;
        Object = null;
        EditorAction.MarkDirty();
    }

    protected virtual bool RenderItem(Asset asset, int index)
    {
        return ImGuiNet.Selectable(
            (!string.IsNullOrEmpty(asset.Name) ? asset.Name : asset.Guid) + $"##Asset_{index}");
    }

    private List<Asset> SearchAssets(string query)
    {
        if (VariableInfo?.VariableType == null) return [];
        
        var queryLower = query.ToLower();
        return AssetsManager.Assets.Where(Predicate).ToList();

        bool Predicate(Asset asset)
        {
            var assetName = asset.Name?.ToLower() ?? "";
            return VariableInfo.VariableType.IsInstanceOfType(asset) &&
                   (string.IsNullOrEmpty(queryLower) || Regex.IsMatch(assetName, queryLower));
        }
    }
}