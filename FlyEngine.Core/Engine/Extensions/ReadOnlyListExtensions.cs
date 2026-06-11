using System.Runtime.InteropServices;

namespace FlyEngine.Core.Extensions;

public static class ReadOnlyListExtensions
{
    public static T? Find<T>(this IReadOnlyList<T> list, Predicate<T> predicate) where T : class
    {
        var span = CollectionsMarshal.AsSpan(list.ToList());
        for (var i = 0; i < span.Length; i++)
        {
            var el = span[i];
            if (predicate(el))
                return el;
        }
        return null;
    }
}