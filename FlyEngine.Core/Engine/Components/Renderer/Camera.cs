using System.Text.Json.Serialization;
using FlyEngine.Core.Engine.Components.Common;

namespace FlyEngine.Core.Engine.Components.Renderer;

public class Camera : Component
{
    [JsonIgnore]
    private static Camera? _currentCamera;
    [JsonIgnore]
    public static Camera? CurrentCamera
    {
        get => _currentCamera;
        set
        {
            if (_currentCamera == value) return;
            _currentCamera = value;
            OnCurrentCameraChanged?.Invoke();
        }
    }
    public static event Action? OnCurrentCameraChanged;
}