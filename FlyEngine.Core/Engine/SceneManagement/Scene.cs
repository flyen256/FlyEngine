using System.Runtime.InteropServices;
using FlyEngine.Core.Assets;
using FlyEngine.Core.Components.Colliders;
using FlyEngine.Core.Components.Common;
using FlyEngine.Core.Components.Renderer;
using FlyEngine.Core.Components.Renderer._3D.Meshes;
using FlyEngine.Core.Components.Renderer.Lighting;
using FlyEngine.Core.Extensions;
using FlyEngine.Core.Gui;
using FlyEngine.Core.Gui.ImGui;
using FlyEngine.Core.Renderer.Lighting;
using MemoryPack;

namespace FlyEngine.Core.SceneManagement;

[MemoryPackable]
public partial class Scene(Guid guid) : Asset(guid)
{
    [MemoryPackInclude]
    private List<GameObject> _gameObjects = [];
    [MemoryPackIgnore]
    private readonly List<Behaviour> _behaviours = [];
    [MemoryPackIgnore]
    private readonly List<LightSource> _lights = [];
    [MemoryPackIgnore]
    private readonly List<Camera> _cameras = [];
    [MemoryPackIgnore]
    private readonly List<GuiWindow> _guiWindows = [];
    [MemoryPackIgnore]
    private readonly List<Collider> _colliders = [];
    [MemoryPackIgnore]
    private readonly List<MeshRenderer> _meshRenderers = [];

    [MemoryPackIgnore]
    public IReadOnlyList<GameObject> GameObjects => _gameObjects;
    [MemoryPackIgnore]
    public IReadOnlyList<Behaviour> Behaviours => _behaviours;
    [MemoryPackIgnore]
    public IReadOnlyList<LightSource> Lights => _lights;
    [MemoryPackIgnore]
    public IReadOnlyList<Camera> Cameras => _cameras;
    [MemoryPackIgnore]
    public IReadOnlyList<GuiWindow> GuiWindows => _guiWindows;
    [MemoryPackIgnore]
    public IReadOnlyList<Collider> Colliders => _colliders;
    [MemoryPackIgnore]
    public IReadOnlyList<MeshRenderer> MeshRenderers => _meshRenderers;

    [MemoryPackOnDeserialized]
    private void OnDeserialized()
    {
        var gameObjects = CollectionsMarshal.AsSpan(_gameObjects);
        for (var i = 0; i < gameObjects.Length; i++)
        {
            var gameObject = gameObjects[i];
            var components = CollectionsMarshal.AsSpan(gameObjects[i].ComponentStore.List.ToList());
            for (var o = 0; o < components.Length; o++)
            {
                var component = components[o];
                RegisterComponent(component, gameObject);
            }
        }
    }
    
    public DeferredEnvironment Environment { get; private set; } = DeferredEnvironment.Default;

    protected internal void OnLoad()
    {
        if (!Application.IsRunning) return;
        foreach (var gameObject in _gameObjects)
            gameObject.ComponentStore.InitializeComponents();
        foreach (var behaviour in Behaviours.Where(behaviour => behaviour.IsActive()))
            behaviour.OnLoad();

        if (!ImGui.Initialized) return;
        foreach (var uiWindow in GuiWindows)
            uiWindow.OnLoadUi();
    }

    public void PreLoad()
    {
        
    }

    public override void Unload()
    {
        _gameObjects.Clear();
        _behaviours.Clear();
        _lights.Clear();
        _cameras.Clear();
        _guiWindows.Clear();
        _colliders.Clear();
        _meshRenderers.Clear();
        base.Unload();
    }

    public void Update(double deltaTime)
    {
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
        RegisterGameObjectComponents(go);
    }

    public void RegisterComponent(Component component, GameObject gameObject)
    {
        component.GameObject = gameObject;
        switch (component)
        {
            case MeshRenderer meshRenderer:
                meshRenderer.SceneIndex = _meshRenderers.Count;
                _meshRenderers.Add(meshRenderer);
                break;
            case Behaviour behaviour:
                behaviour.SceneIndex = _behaviours.Count;
                _behaviours.Add(behaviour);
                break;
            case LightSource lightSource:
                lightSource.SceneIndex = _lights.Count;
                _lights.Add(lightSource);
                break;
            case Camera camera:
                camera.SceneIndex = _cameras.Count;
                _cameras.Add(camera);
                break;
            case GuiWindow guiWindow:
                guiWindow.SceneIndex = _guiWindows.Count;
                _guiWindows.Add(guiWindow);
                break;
            case Collider collider:
                collider.SceneIndex = _colliders.Count;
                _colliders.Add(collider);
                break;
        }
    }

    internal void RemoveComponent(Component component)
    {
        switch (component)
        {
            case MeshRenderer meshRenderer:
                _meshRenderers.RemoveAtSwapBack(meshRenderer.SceneIndex);
                break;
            case Behaviour behaviour:
                _behaviours.RemoveAtSwapBack(behaviour.SceneIndex);
                break;
            case LightSource lightSource:
                _lights.RemoveAtSwapBack(lightSource.SceneIndex);
                break;
            case Camera camera:
                _cameras.RemoveAtSwapBack(camera.SceneIndex);
                break;
            case GuiWindow guiWindow:
                _guiWindows.RemoveAtSwapBack(guiWindow.SceneIndex);
                break;
            case Collider collider:
                _colliders.RemoveAtSwapBack(collider.SceneIndex);
                break;
        }
    }

    private void RegisterGameObjectComponents(GameObject go)
    {
        var components = go.ComponentStore.List;
        foreach (var component in components)
            RegisterComponent(component, go);
    }

    private void RemoveGameObjectComponents(GameObject go)
    {
        var components = go.ComponentStore.List;
        foreach (var component in components)
            RemoveComponent(component);
    }

    private void RemoveGameObject(int index)
    {
        var go = _gameObjects[index];
        _gameObjects.RemoveAtSwapBack(index);

        RemoveGameObjectComponents(go);
    }

    public bool ObjectExistsWithName(string name)
    {
        return _gameObjects.Exists(g => g.Name == name);
    }
}