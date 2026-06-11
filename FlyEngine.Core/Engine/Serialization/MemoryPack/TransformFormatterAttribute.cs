using FlyEngine.Core.Components.Common;
using MemoryPack;

namespace FlyEngine.Core.Serialization;

public class TransformFormatterAttribute : MemoryPackCustomFormatterAttribute<Transform>
{
    private readonly TransformFormatter _formatter = new();

    public override IMemoryPackFormatter<Transform> GetFormatter() => _formatter;
}