using FlyEngine.Core.Components.Common;
using FlyEngine.Core.Extensions;
using FlyEngine.Core.SceneManagement;
using MemoryPack;

namespace FlyEngine.Core.Serialization;

public class GameObjectFormatter : MemoryPackFormatter<GameObject>
{
    public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref GameObject? value)
    {
        if (value == null)
        {
            writer.WriteNullObjectHeader();
            return;
        }

        writer.WriteObjectHeader(1);
        writer.WriteString(value.Name);
    }

    public override void Deserialize(ref MemoryPackReader reader, scoped ref GameObject? value)
    {
        if (!reader.TryReadObjectHeader(out var memberCount))
        {
            value = null;
            return;
        }
        if (memberCount != 1)
            MemoryPackSerializationException.ThrowInvalidPropertyCount(1, memberCount);
        var name = reader.ReadString();
        var gameObject = SceneManager.CurrentScene?.GameObjects.Find(g => g.Name == name);
        if (name == null ||
            SceneManager.CurrentScene == null ||
            gameObject == null) return;
        value = GameObject.CreateWithLazyReference(name);
    }
}