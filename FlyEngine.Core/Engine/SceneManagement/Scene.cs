using System.Runtime.InteropServices;
using FlyEngine.Core.Engine.Assets;
using FlyEngine.Core.Engine.Components.Colliders;
using FlyEngine.Core.Engine.Components.Common;
using FlyEngine.Core.Engine.Components.Renderer;
using FlyEngine.Core.Engine.Components.Renderer.Lighting;
using FlyEngine.Core.Engine.Extensions;
using FlyEngine.Core.Engine.Gui;
using FlyEngine.Core.Engine.Gui.ImGui;
using FlyEngine.Core.Engine.Renderer.Lighting;
using MemoryPack;

namespace FlyEngine.Core.Engine.SceneManagement;

[MemoryPackable]
public partial class Scene(Guid guid) : Asset(guid)
{
    [MemoryPackIgnore]
    public string? Name { get; set; }
    [MemoryPackIgnore]
    public string? Path { get; set; }
    
    [MemoryPackInclude]
    private List<GameObject> _gameObjects = [];
    [MemoryPackIgnore]
    private readonly List<Behaviour> _behaviours = [];
    [MemoryPackIgnore]
    private readonly List<LightSource> _lights = [];
    [MemoryPackIgnore]
    private readonly List<Camera> _cameras = [];
    [MemoryPackIgnore]
    private readonly List<GuiWindow> _uiWindows = [];
    [MemoryPackIgnore]
    private readonly List<Collider> _colliders = [];

    [MemoryPackIgnore]
    public IReadOnlyList<GameObject> GameObjects => _gameObjects;
    [MemoryPackIgnore]
    public IReadOnlyList<Behaviour> Behaviours => _behaviours;
    [MemoryPackIgnore]
    public IReadOnlyList<LightSource> Lights => _lights;
    [MemoryPackIgnore]
    public IReadOnlyList<Camera> Cameras => _cameras;
    [MemoryPackIgnore]
    public IReadOnlyList<GuiWindow> UiWindows => _uiWindows;
    [MemoryPackIgnore]
    public IReadOnlyList<Collider> Colliders => _colliders;
    
    public DeferredEnvironment Environment { get; private set; } = DeferredEnvironment.Default;

    protected internal void OnLoad()
    {
        if (!Application.IsRunning) return;
        foreach (var gameObject in _gameObjects)
            gameObject.ComponentStore.InitializeComponents();
        foreach (var behaviour in Behaviours.Where(behaviour => behaviour.IsActive()))
            behaviour.OnLoad();

        if (!ImGui.Initialized) return;
        foreach (var uiWindow in UiWindows)
            uiWindow.OnLoadUi();
    }

    public void PreLoad()
    {
        
    }

    public void OnUnload()
    {
        _gameObjects.Clear();
        _behaviours.Clear();
        _lights.Clear();
        _cameras.Clear();
        _uiWindows.Clear();
        _colliders.Clear();
    }

    public void Update(double deltaTime)
    {
        var span = CollectionsMarshal.AsSpan(_behaviours);

        for (var i = 0; i < span.Length; i++)
        {
            var b = span[i];
            if (b.Enabled)
                b.OnUpdate(deltaTime);
        }
        var gameObjects = CollectionsMarshal.AsSpan(_gameObjects);
        for (var i = gameObjects.Length - 1; i >= 0; i--)
        {
            if (gameObjects[i].IsDestroyed)
                RemoveGameObject(i);
        }
    }

    internal void AddGameObject(GameObject go)
    {
        _gameObjects.Add(go);

        var behaviours = CollectionsMarshal.AsSpan(go.GetComponents<Behaviour>());
        for (var i = 0; i < behaviours.Length; i++)
        {
            var b = behaviours[i];
            b.SceneIndex = _behaviours.Count;
            _behaviours.Add(b);
        }
        var lights = CollectionsMarshal.AsSpan(go.GetComponents<LightSource>());
        for (var i = 0; i < lights.Length; i++)
        {
            var light = lights[i];
            light.SceneIndex = _lights.Count;
            _lights.Add(light);
        }
        var cameras = CollectionsMarshal.AsSpan(go.GetComponents<Camera>());
        for (var i = 0; i < cameras.Length; i++)
        {
            var camera = cameras[i];
            camera.SceneIndex = _cameras.Count;
            _cameras.Add(camera);
        }
        var uiWindows = CollectionsMarshal.AsSpan(go.GetComponents<GuiWindow>());
        for (var i = 0; i < uiWindows.Length; i++)
        {
            var uiWindow = uiWindows[i];
            uiWindow.SceneIndex = _uiWindows.Count;
            _uiWindows.Add(uiWindow);
        }
        var colliders = CollectionsMarshal.AsSpan(go.GetComponents<Collider>());
        for (var i = 0; i <  colliders.Length; i++)
        {
            var collider = colliders[i];
            collider.SceneIndex = _colliders.Count;
            _colliders.Add(collider);
        }
    }

    private void RemoveGameObject(int index)
    {
        var go = _gameObjects[index];
        _gameObjects.RemoveAtSwapBack(index);

        var behaviours = CollectionsMarshal.AsSpan(go.GetComponents<Behaviour>());
        for (var i = 0; i < behaviours.Length; i++)
        {
            var behaviour = behaviours[i];
            _behaviours.RemoveAtSwapBack(behaviour.SceneIndex);
        }
        var lights = CollectionsMarshal.AsSpan(go.GetComponents<LightSource>());
        for (var i = 0; i < lights.Length; i++)
        {
            var light = lights[i];
            _lights.RemoveAtSwapBack(light.SceneIndex);
        }
        var cameras = CollectionsMarshal.AsSpan(go.GetComponents<Camera>());
        for (var i = 0; i < cameras.Length; i++)
        {
            var camera = cameras[i];
            _cameras.RemoveAtSwapBack(camera.SceneIndex);
        }
        var uiWindows = CollectionsMarshal.AsSpan(go.GetComponents<GuiWindow>());
        for (var i = 0; i < uiWindows.Length; i++)
        {
            var uiWindow = uiWindows[i];
            _uiWindows.RemoveAtSwapBack(uiWindow.SceneIndex);
        }
        var colliders = CollectionsMarshal.AsSpan(go.GetComponents<Collider>());
        for (var i = 0; i <  colliders.Length; i++)
        {
            var collider = colliders[i];
            _colliders.RemoveAtSwapBack(collider.SceneIndex);
        }
    }

    public bool ObjectExistsWithName(string name)
    {
        return _gameObjects.Exists(g => g.Name == name);
    }
}