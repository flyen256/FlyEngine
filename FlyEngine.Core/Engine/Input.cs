using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace FlyEngine.Core;

public static class Input
{
    public static IInputContext? InputContext { get; private set; }

    private static readonly List<Key> PressedKeys = [];

    public static float MouseSmoothing { get; set; } = 0.35f;

    private static Vector2 _mouseDeltaAccumulated;
    private static Vector2 _mouseDeltaPreviousFrame;

    public static Vector2 MouseInput { get; private set; } = new();
    public static Vector2 MousePosition { get; private set; } = new();

    private static bool _cursorLocked;
    private static bool _cursorVisible = true;

    public static bool CursorLocked
    {
        get => _cursorLocked;
        set
        {
            _cursorLocked = value;
            CenterMouse();
        }
    }

    public static bool CursorVisible
    {
        get => _cursorVisible;
        set
        {
            _cursorVisible = value;
            if (InputContext == null) return;
            foreach (var mouse in InputContext.Mice)
                mouse.Cursor.CursorMode = value ? CursorMode.Normal : CursorMode.Hidden;
        }
    }

    public static void Initialize(IWindow window)
    {
        InputContext = window.CreateInput();
        foreach (var mouse in InputContext.Mice)
            mouse.Cursor.CursorMode = _cursorVisible ? CursorMode.Disabled : CursorMode.Normal;
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

        if (MouseSmoothing <= 0f)
        {
            MouseInput = raw;
            _mouseDeltaPreviousFrame = raw;
            return;
        }

        var t = System.Math.Clamp(MouseSmoothing, 0f, 0.95f);
        MouseInput = raw * (1f - t) + _mouseDeltaPreviousFrame * t;
        _mouseDeltaPreviousFrame = raw;
    }

    private static void CenterMouse()
    {
        if (InputContext == null) return;

        var centerX = Application.Instance.Window.Size.X / 2;
        var centerY = Application.Instance.Window.Size.Y / 2;

        foreach (var mouse in InputContext.Mice)
        {
            mouse.Position = new Vector2(centerX, centerY);
            MousePosition = mouse.Position;
        }
    }

    private static void OnMouseMove(IMouse mouse, Vector2 mousePosition)
    {
        var deltaX = mousePosition.X - MousePosition.X;
        var deltaY = MousePosition.Y - mousePosition.Y;

        MousePosition = mousePosition;

        _mouseDeltaAccumulated += new Vector2(deltaX, deltaY);

        CenterMouse();
    }

    private static void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        if (!PressedKeys.Contains(key))
        {
            PressedKeys.Add(key);
            foreach (var behaviour in Application.Instance.Behaviours)
            {
                var onKeyDownMethod = behaviour.GetType().GetMethod("OnKeyDown");
                if (onKeyDownMethod == null) continue;
                onKeyDownMethod.Invoke(behaviour, [key, keyCode]);
            }
        }
    }

    private static void OnKeyUp(IKeyboard keyboard, Key key, int keyCode)
    {
        PressedKeys.Remove(key);
    }

    public static bool GetKey(Key key)
    {
        return PressedKeys.Contains(key);
    }

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
