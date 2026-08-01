namespace FlyEngine.Core.ECS;

internal static class ComponentTypeCounter
{
    private static int _counter;
    public static int GetUniqueId() => _counter++;
}