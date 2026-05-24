namespace FlyEngine.Core.Assets;

public class Model : Asset
{
    private readonly List<Mesh> _meshes;
    public IReadOnlyList<Mesh> Meshes => _meshes;

    public Model(Guid guid, List<Mesh> meshes) : base(guid)
    {
        _meshes = meshes;
        AssetsManager.AddAsset(this);
    }
}