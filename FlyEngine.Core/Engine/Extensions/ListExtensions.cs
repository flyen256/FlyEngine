namespace FlyEngine.Core.Extensions;

public static class ListExtensions
{
    public static void RemoveAtSwapBack<T>(this List<T> list, int index)
    {
        int lastIndex = list.Count - 1;
        if (index < lastIndex)
            list[index] = list[lastIndex];
        list.RemoveAt(lastIndex);
    }
}