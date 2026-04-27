using System.Runtime.Loader;

namespace FlyEngine.Core;

public class EditorScriptLoader() : AssemblyLoadContext(isCollectible: true);