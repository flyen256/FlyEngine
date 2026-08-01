namespace FlyEngine.Core.ECS;

public static class ComponentTypeId<T> where T : struct
{
    public static readonly int Id = ComponentTypeCounter.GetUniqueId();
}