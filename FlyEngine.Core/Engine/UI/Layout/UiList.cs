using System.Numerics;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Core.Engine.UI.Layout;

public class UiList<TElement>(
    Func<IEnumerable<TElement>> listGetter,
    Func<TElement, UiElement> elementFactory) : UiElement
{
    private readonly Dictionary<TElement, UiElement> _cache = new();

    public override void Draw()
    {
        var totalSize = Vector2.Zero;
        var currentItems = listGetter().ToList();

        _cache.Keys.ToList().ForEach(k => { if(!currentItems.Contains(k)) _cache.Remove(k); });

        foreach (var item in currentItems)
        {
            if (!_cache.TryGetValue(item, out var visual))
            {
                visual = elementFactory(item);
                _cache[item] = visual;
            }
            if (item == null) continue;

            ImGuiNet.PushID(item.GetHashCode());
            visual.Draw();
            totalSize += visual.Size;
            ImGuiNet.PopID();
        }
        Size = totalSize;
    }
}