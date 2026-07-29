namespace FlyEngine.Core.Components.Common;

public abstract class Object
{
    public abstract void Destroy();
    public static void Destroy(Object obj) => obj.Destroy();
}