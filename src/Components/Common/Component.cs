using Silk.NET.Maths;

namespace Flyeng;

public class Component : Object
{
    private GameObject _gameObject;
    public GameObject GameObject
    {
        get => _gameObject;
        set
        {
            _gameObject = value;
            if(_gameObject != null)
                OnInitialize();
        }
    }
    public Transform Transform => GameObject.Transform;
    public Vector3D<float> Position => GameObject.Transform.Position;
    public Vector3D<float> Size => GameObject.Transform.Size;

    protected virtual void OnInitialize() { }

    public T? GetComponent<T>() where T : Component
    {
        return GameObject != null ? GameObject.Components.GetComponent<T>() : null;
    }

    public T? AddComponent<T>() where T : Component
    {
        return GameObject != null ? GameObject.Components.AddComponent<T>() : null;
    }

    public T? AddComponent<T>(T component) where T : Component
    {
        return GameObject != null ? GameObject.Components.AddComponent(component) : null;
    }
}
