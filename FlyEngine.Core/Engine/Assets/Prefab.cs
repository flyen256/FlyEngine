namespace FlyEngine.Core.Assets;

public class Prefab(Guid guid) : Asset(guid)
{
    public static string Extension => ".prefab";
}