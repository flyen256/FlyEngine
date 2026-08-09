using MemoryPack;

namespace FlyEngine.Core.Assets;

[MemoryPackable]
public partial class Model : Asset
{
    [MemoryPackIgnore]
    private readonly List<Mesh> _meshes = [];
    
    [MemoryPackIgnore]
    public IReadOnlyList<Mesh> Meshes => _meshes;

    [MemoryPackIgnore]
    public List<Guid> MeshesGuids { get; private set; } = [];

    [MemoryPackInclude]
    private List<Guid> MeshesGuidsData
    {
        get
        {
            MeshesGuids = _meshes.Select(x => x.Guid).ToList();
            return MeshesGuids;
        }
        init => MeshesGuids = value;
    }

    [MemoryPackConstructor]
    private Model(Guid guid): base(guid) {}
    
    public Model(Guid guid, string path, string name, List<Mesh> meshes) : base(guid)
    {
        Name = name;
        Path = path;
        _meshes = meshes;
        AssetsManager.SaveAssetGlobal(this);
        AssetsManager.AddAsset(this);
    }
}