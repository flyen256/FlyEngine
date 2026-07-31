using MemoryPack;
using GameObject = FlyEngine.Core.Components.GameObject;

namespace FlyEngine.Core.Serialization.MemoryPack;

public class GameObjectFormatterAttribute : MemoryPackCustomFormatterAttribute<GameObject>
{
    private readonly GameObjectFormatter _formatter = new();

    public override IMemoryPackFormatter<GameObject> GetFormatter() => _formatter;
}