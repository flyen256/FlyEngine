using FlyEngine.Core.Extensions;
using Silk.NET.OpenGL;

namespace FlyEngine.Core.Assets;

public static class AssetsManager
{
    private static List<Asset> _assets = [];
    private static List<string> _loadedAssetsPaths = [];
    
    public static IReadOnlyList<Asset> Assets => _assets;
    public static IReadOnlyList<Mesh> Meshes => GetAssets<Mesh>();
    public static IReadOnlyList<string> LoadedAssetsPaths => _loadedAssetsPaths;

    public static event Action? OnAssetsChanged;

    internal static List<T> GetAssets<T>() where T : Asset =>
        _assets.OfType<T>().ToList();
    internal static T? GetAsset<T>(Guid guid) where T : Asset =>
        GetAssets<T>().Find(a => a.Guid == guid);
    internal static Asset? GetAsset(Guid guid) =>
        _assets.Find(a => a.Guid == guid);

    public static async Task LoadAssetsAsync(GL? gl = null)
    {
        await Task.Run(() => LoadAssets(gl));
    }

    public static void LoadAssets(GL? gl = null)
    {
        foreach (var asset in _assets)
        {
            if (asset.Loaded) continue;
            asset.Load(gl);
        }
    }
    
    internal static void AddAsset<T>(T asset) where T : Asset
    {
        asset.AssetIndex = _assets.Count;
        _assets.Add(asset);
        if (asset.Path != null) _loadedAssetsPaths.Add(asset.Path);
        OnAssetsChanged?.Invoke();
    }

    internal static void AddAssets<T>(List<T> assets) where T : Asset
    {
        foreach (var asset in assets)
        {
            asset.AssetIndex = _assets.Count;
            _assets.Add(asset);
            if (asset.Path != null) _loadedAssetsPaths.Add(asset.Path);
        }
        OnAssetsChanged?.Invoke();
    }

    internal static void UnloadAsset<T>(T asset) where T : Asset
    {
        asset.Unload();
        if (asset.AssetIndex == -1)
            throw new ArgumentOutOfRangeException($"Cannot remove asset {asset.Name}");
        if (asset.Path != null) _loadedAssetsPaths.Remove(asset.Path);
        _assets.RemoveAtSwapBack(asset.AssetIndex);
        OnAssetsChanged?.Invoke();
    }
    
    internal static void UnloadAssets<T>(List<T> assets) where T : Asset
    {
        foreach (var asset in assets)
        {
            asset.Unload();
            if (asset.AssetIndex == -1)
                throw new ArgumentOutOfRangeException($"Cannot remove asset {asset.Name}");
            if (asset.Path != null) _loadedAssetsPaths.Remove(asset.Path);
            _assets.RemoveAtSwapBack(asset.AssetIndex);
        }
        OnAssetsChanged?.Invoke();
    }
}