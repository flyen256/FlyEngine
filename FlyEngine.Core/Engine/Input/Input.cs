using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace FlyEngine.Core;

public static class Input
{
    public static IInputContext? InputContext { get; private set; }

    private static readonly List<Key> PressedKeys = [];

    private static Vector2 _mouseDeltaAccumulated;

    public static Vector2 MouseInput { get; private set; } = Vector2.Zero;
    public static Vector2 MousePosition { get; private set; } = Vector2.Zero;

    private static bool _cursorVisible = true;
    private static bool? _previousStateVisible;
    private static bool? _previousStateLocked;

    public static bool CursorVisible
    {
        get => _cursorVisible;
        set
        {
            if (_cursorVisible.Equals(value)) return;
            _cursorVisible = value;
            if (InputContext == null) return;
            foreach (var mouse in InputContext.Mice)
                mouse.Cursor.CursorMode = _cursorVisible ? CursorMode.Normal : CursorMode.Hidden;
        }
    }

    private static bool _cursorLocked;
    public static bool CursorLocked
    {
        get => _cursorLocked;
        set
        {
            if (_cursorLocked.Equals(value)) return;
            if (InputContext == null) return;
            if (_lockPositions.Length < InputContext.Mice.Count)
                _lockPositions = new Vector2[InputContext.Mice.Count];
            if (value)
            {
                for (var i = 0; i < InputContext.Mice.Count; i++)
                {
                    var mouse = InputContext.Mice[i];
                    _lockPositions[i] = mouse.Position;
                }
                MoveMouseToLockPosition();
            }
            else
            {
                for (var i = 0; i < InputContext.Mice.Count; i++)
                {
                    var mouse = InputContext.Mice[i];
                    mouse.Position = _lockPositions[i];
                }
                _lockPositions = [];
            }
            _cursorLocked = value;
        }
    }

    public static void LockAndHideCursor()
    {
        CursorLocked = true;
        CursorVisible = false;
    }

    public static void UnlockAndShowCursor()
    {
        CursorLocked = false;
        CursorVisible = true;
    }
    
    private static Vector2[] _lockPositions = [];

    public static void Initialize(IWindow window)
    {
        InputContext = window.CreateInput();
        foreach (var mouse in InputContext.Mice)
            mouse.Cursor.CursorMode = CursorVisible ? CursorMode.Normal : CursorMode.Disabled;
        foreach (var keyboard in InputContext.Keyboards)
        {
            keyboard.KeyDown += OnKeyDown;
            keyboard.KeyUp += OnKeyUp;
        }
        foreach (var mouse in InputContext.Mice)
            mouse.MouseMove += OnMouseMove;
    }
    
    private static void MoveMouseToLockPosition()
    {
        if (InputContext == null || Application.Window == null) return;

        var centerX = Application.Window.Handle.Size.X / 2;
        var centerY = Application.Window.Handle.Size.Y / 2;

        for (var i = 0; i < InputContext.Mice.Count; i++)
        {
            var mouse = InputContext.Mice[i];
            mouse.Position =
                // i < _lockPositions.Length ?
                //     new Vector2(_lockPositions[i].X, _lockPositions[i].Y) :
                    new Vector2(centerX, centerY);
            MousePosition = mouse.Position;
        }
    }

    public static void Update(double deltaTime)
    {
        var raw = _mouseDeltaAccumulated;
        _mouseDeltaAccumulated = Vector2.Zero;

        MouseInput = raw;

        if (CursorLocked)
            MoveMouseToLockPosition();
    }

    private static void OnMouseMove(IMouse mouse, Vector2 mousePosition)
    {
        var deltaX = mousePosition.X - MousePosition.X;
        var deltaY = MousePosition.Y - mousePosition.Y;

        MousePosition = mousePosition;

        _mouseDeltaAccumulated += new Vector2(deltaX, deltaY);
    }

    private static void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        if (PressedKeys.Contains(key)) return;
        PressedKeys.Add(key);
        if (!Application.IsRunning) return;
        if (key == Key.Escape &&
            Application.Window is { IsEditor: true })
        {
            if (CursorLocked)
            {
                _previousStateLocked = CursorLocked;
                CursorLocked = false;
            }
            else if (_previousStateLocked != null)
            {
                CursorLocked = (bool)_previousStateLocked;
                _previousStateLocked = null;
            }
            if (!CursorVisible)
            {
                _previousStateVisible = CursorVisible;
                CursorVisible = true;
            }
            else if (_previousStateVisible != null)
            {
                CursorVisible = (bool)_previousStateVisible;
                _previousStateVisible = null;
            }
        }
        if (Application.Scene == null) return;
        foreach (var behaviour in Application.Scene.Behaviours)
        {
            var onKeyDownMethod = behaviour.GetType().GetMethod("OnKeyDown");
            if (onKeyDownMethod == null) continue;
            onKeyDownMethod.Invoke(behaviour, [key, keyCode]);
        }
    }

    private static void OnKeyUp(IKeyboard keyboard, Key key, int keyCode)
    {
        PressedKeys.Remove(key);
        if (!Application.IsRunning) return;
    }

    public static bool GetKey(Key key) => PressedKeys.Contains(key);

    public static Vector2D<float> GetMoveInput()
    {
        var vector = Vector2.Zero;
        if (GetKey(Key.W)) vector.Y += 1;
        if (GetKey(Key.S)) vector.Y -= 1;
        if (GetKey(Key.D)) vector.X += 1;
        if (GetKey(Key.A)) vector.X -= 1;

        return vector != Vector2.Zero ? Vector2.Normalize(vector).ToGeneric() : vector.ToGeneric();
    }
}