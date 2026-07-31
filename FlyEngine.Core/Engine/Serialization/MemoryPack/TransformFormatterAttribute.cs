using MemoryPack;
using Transform = FlyEngine.Core.Components.Transform;

namespace FlyEngine.Core.Serialization.MemoryPack;

public class TransformFormatterAttribute : MemoryPackCustomFormatterAttribute<Transform>
{
    private readonly TransformFormatter _formatter = new();

    public override IMemoryPackFormatter<Transform> GetFormatter() => _formatter;
}