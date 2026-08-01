using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace FlyEngine.Core.Input;

public static class Input
{
    public static IInputContext? InputContext { get; private set; }

    private static readonly List<Key> PressedKeys = [];
    
    public delegate void OnKeyDownDelegate(IKeyboard keyboard, Key key, int keyCode);
    public static event OnKeyDownDelegate? OnKeyDownEvent;

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
            {
                if (CursorLocked)
                {
                    mouse.Cursor.CursorMode = CursorMode.Raw;
                    MousePosition = mouse.Position;
                }
                else
                    mouse.Cursor.CursorMode = _cursorVisible ? CursorMode.Normal : CursorMode.Hidden;
            }
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
            if (value)
            {
                for (var i = 0; i < InputContext.Mice.Count; i++)
                {
                    var mouse = InputContext.Mice[i];
                    mouse.Cursor.CursorMode = CursorMode.Raw;
                    MousePosition = mouse.Position;
                }
            }
            else
            {
                for (var i = 0; i < InputContext.Mice.Count; i++)
                {
                    var mouse = InputContext.Mice[i];
                    mouse.Cursor.CursorMode = CursorVisible ? CursorMode.Normal : CursorMode.Hidden;
                    MousePosition = mouse.Position;
                }
            }
            _cursorLocked = value;
        }
    }

    public static bool BlockInput { get; set; } = false;

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

    public static void Update(double deltaTime)
    {
        var raw = _mouseDeltaAccumulated;
        _mouseDeltaAccumulated = Vector2.Zero;

        if (BlockInput) raw = Vector2.Zero;
        MouseInput = raw;
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
        OnKeyDownEvent?.Invoke(keyboard, key, keyCode);
        if (!Application.IsRunning) return;
        if (key == Key.Escape &&
            Application.IsEditor)
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
        if (Application.Scene == null || BlockInput) return;
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
        if (!Application.IsRunning || BlockInput) return;
    }

    public static bool GetKey(Key key) => PressedKeys.Contains(key) && !BlockInput;

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