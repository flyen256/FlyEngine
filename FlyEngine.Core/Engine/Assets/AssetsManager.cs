namespace FlyEngine.Core.Engine.Assets;

public static class AssetsManager
{
    private static List<Asset> _loadedAssets = [];
    
    public static IReadOnlyList<Asset> LoadedAssets => _loadedAssets;
}