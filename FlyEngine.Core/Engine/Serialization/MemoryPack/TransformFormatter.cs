using MemoryPack;
using Transform = FlyEngine.Core.Components.Transform;

namespace FlyEngine.Core.Serialization.MemoryPack;

public class TransformFormatter : MemoryPackFormatter<Transform?>
{
    public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Transform? value)
    {
        if (value == null)
        {
            writer.WriteNullObjectHeader();
            return;
        }

        writer.WriteObjectHeader(1);
        writer.WriteString(value.GameObject.Name);
    }

    public override void Deserialize(ref MemoryPackReader reader, scoped ref Transform? value)
    {
        if (!reader.TryReadObjectHeader(out var memberCount) || memberCount == 0)
        {
            value = null;
            return;
        }
        var name = reader.ReadString();
        if (string.IsNullOrEmpty(name))
        {
            value = null;
            return;
        }
        value = Transform.CreateWithLazyReference(name);
    }
}