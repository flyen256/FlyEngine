namespace FlyEngine.Core.Assets;

public abstract class Object
{
    public abstract void Destroy();
    public abstract void CopyTo(ref Object? copy);
    public abstract void PasteCopy();
    public static void Destroy(Object obj) => obj.Destroy();
}