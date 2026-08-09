using Object = FlyEngine.Core.Assets.Object;

namespace FlyEngine.Editor;

public static class EditorClipboard
{
    private static Object? _objectCopy;

    public static void SetObjectCopy(Object? obj)
    {
        if (obj == null)
        {
            _objectCopy = null;
            return;
        }
        obj.CopyTo(ref _objectCopy);
    }

    public static void PasteObjectCopy()
    {
        if (_objectCopy == null) return;
        _objectCopy.PasteCopy();
    }
}