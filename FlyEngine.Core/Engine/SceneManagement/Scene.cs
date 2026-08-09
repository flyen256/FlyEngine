using System.Reflection;
using System.Runtime.InteropServices;
using FlyEngine.Core.Assets;
using FlyEngine.Core.Components;
using FlyEngine.Core.ECS;
using FlyEngine.Core.Extensions;
using FlyEngine.Core.Gui;
using MemoryPack;
using Component = FlyEngine.Core.Components.Component;
using DeferredEnvironment = FlyEngine.Core.Renderer.DeferredEnvironment;
using GameObject = FlyEngine.Core.Components.GameObject;

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

    [MemoryPackInclude]
    public World EcsWorld = new();
    
    public DeferredEnvironment Environment { get; private set; } = DeferredEnvironment.Default;

    [MemoryPackOnDeserialized]
    private void OnDeserialized()
    {
        var gameObjects = CollectionsMarshal.AsSpan(_gameObjects);

        for (var i = 0; i < gameObjects.Length; i++)
        {
            var gameObject = gameObjects[i];
            gameObject.Scene = this;

            var components = gameObject.ComponentStore.List;
            
            for (var o = 0; o < components.Count; o++)
            {
                var component = components[o];
                RegisterComponent(component, gameObject);

                var componentFields = GetComponentFields(component.GetType(), typeof(Component));
                foreach (var field in componentFields)
                {
                    var value = (Component?)field.GetValue(component);
                    if (value == null || value.LazyGuid == Guid.Empty) continue;
                    field.SetValue(GetComponentByGuid(value.LazyGuid), value);
                }

                var transformFields = GetComponentFields(component.GetType(), typeof(TransformComponent));
                foreach (var field in transformFields)
                {
                    var value = (TransformComponent?)field.GetValue(component);
                    if (value == null || value.Value.LazyGuid == Guid.Empty) continue;
                    
                    var go = GetTransformByGuid(value.Value.LazyGuid);
                    if (go == null) continue;
                    
                    ref var transform = ref go.Transform;
                    field.SetValue(transform, value);
                }
            }
        }

        for (var i = 0; i < gameObjects.Length; i++)
        {
            var gameObject = gameObjects[i];
            ref var transform = ref gameObject.Transform;
            transform.GameObject = gameObject;
            transform.ResolveReferences(gameObjects);
        }
        AssetsManager.AddAsset(this);
    }
    
    public async Task SaveAsync()
    {
        if (Path == null || Application.IsRunning) return;
        var fs = File.Open(Path, FileMode.Create);
        await MemoryPackSerializer.SerializeAsync(fs, this);
        fs.Close();
    }
    
    public static FieldInfo[] GetComponentFields(Type targetType, Type fieldType)
    {
        var fields = targetType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        var result = fields.Where(field => fieldType.IsAssignableFrom(field.FieldType)).ToArray();
        return result;
    }

    public Component? GetComponentByGuid(Guid guid)
    {
        var gameObjects = CollectionsMarshal.AsSpan(_gameObjects);
        for (var i = 0; i < gameObjects.Length; i++)
        {
            var components = gameObjects[i].ComponentStore.List;
            for (var o = 0; o < components.Count; o++)
            {
                var component = components[o];
                if (component.Guid == guid)
                    return component;
            }
        }
        return null;
    }
    
    public GameObject? GetTransformByGuid(Guid guid)
    {
        var gameObjects = CollectionsMarshal.AsSpan(_gameObjects);
        for (var i = 0; i < gameObjects.Length; i++)
        {
            var gameObject = gameObjects[i];
            if (gameObject.Transform.Guid == guid)
                return gameObject;
        }
        return null;
    }

    protected internal void OnLoad()
    {
        if (!Application.IsRunning) return;
        foreach (var gameObject in _gameObjects)
            gameObject.ComponentStore.InitializeComponents();

        if (!ImGui.Initialized) return;
        foreach (var uiWindow in GuiWindows)
            uiWindow.OnLoadUi();
    }

    public override void Unload()
    {
        base.Unload();
        _gameObjects.Clear();
        _behaviours.Clear();
        _lights.Clear();
        _cameras.Clear();
        _guiWindows.Clear();
        _colliders.Clear();
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

    public bool ObjectExistsWithName(string name) =>
        _gameObjects.Exists(g => g.Name == name);
}