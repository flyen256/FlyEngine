using FlyEngine.Core.Components.Common;
using MemoryPack;

namespace FlyEngine.Core.Serialization;

public class GameObjectFormatterAttribute : MemoryPackCustomFormatterAttribute<GameObject>
{
    private readonly GameObjectFormatter _formatter = new();

    public override IMemoryPackFormatter<GameObject> GetFormatter() => _formatter;
}