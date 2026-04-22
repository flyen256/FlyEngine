using System.Runtime.Loader;

namespace FlyEngine.Core.Engine;

public class EditorScriptLoader() : AssemblyLoadContext(isCollectible: true);