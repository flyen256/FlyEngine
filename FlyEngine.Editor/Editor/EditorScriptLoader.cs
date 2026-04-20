using System.Runtime.Loader;

namespace FlyEngine.Editor;

public class EditorScriptLoader() : AssemblyLoadContext(isCollectible: true);