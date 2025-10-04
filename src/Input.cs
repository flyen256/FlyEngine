using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Flyeng.Events;
using Flyeng.UI;

namespace Flyeng;

public static class Input
{
    private struct ClickedElement
    {
        public IMouseClickEvents Events;
        public MouseButton Button;

        public ClickedElement(IMouseClickEvents events, MouseButton button)
        {
            Events = events;
            Button = button;
        }
    }

    private static IInputContext? _inputContext;
    public static IInputContext? InputContext => _inputContext;
    private static List<Key> _pressedKeys = new();

    private static List<IMouseEvents> _hoveredObjects = new();
    private static List<ClickedElement> _clickedObjects = new();

    public static void Initialize(IWindow window)
    {
        _inputContext = window.CreateInput();
        foreach (var keyboard in _inputContext.Keyboards)
        {
            keyboard.KeyDown += OnKeyDown;
            keyboard.KeyUp += OnKeyUp;
        }
        foreach (var mouse in _inputContext.Mice)
        {
            mouse.MouseMove += OnMouseMove;
            mouse.MouseDown += OnMouseDown;
            mouse.MouseUp += OnMouseUp;
        }
    }

    private static void OnMouseMove(IMouse mouse, Vector2 mousePosition)
    {
        foreach (var behaviour in Application.Behaviours)
        {
            Element? element = behaviour.GetComponent<Element>();
            if (!(element is IMouseEvents elementEvents)) return;
            elementEvents.OnMouseMove(mouse, mousePosition);
            if (element != null && element.Transform != null)
            {
                Vector3D<float>? position = element.Position;
                Vector3D<float>? size = element.Size;
                if (position != null && size != null)
                {
                    var elementRect = new Rectangle<float>(position.Value.X, position.Value.Y, size.Value.X, size.Value.Y);
                    if (
                        !elementRect.Contains(new Vector2D<float>(mouse.Position.X, mouse.Position.Y)) &&
                        _hoveredObjects.Contains(elementEvents)
                    )
                    {
                        elementEvents.OnMouseExit(mouse, mousePosition);
                        _hoveredObjects.Remove(elementEvents);
                    }
                    else if (
                        elementRect.Contains(new Vector2D<float>(mouse.Position.X, mouse.Position.Y)) &&
                        !_hoveredObjects.Contains(elementEvents)
                    )
                    {
                        elementEvents.OnMouseEnter(mouse, mousePosition);
                        _hoveredObjects.Add(elementEvents);
                    }
                }
            }
        }
    }

    private static void OnMouseDown(IMouse mouse, MouseButton button)
    {
        foreach (var behaviour in Application.Behaviours)
        {
            Element? element = behaviour.GetComponent<Element>();
            if (!(element is IMouseClickEvents elementEvents)) return;
            ClickedElement clickedElement = new ClickedElement(elementEvents, button);
            if (_clickedObjects.Contains(clickedElement)) return;
            if (element != null && element.Transform != null)
            {
                Vector3D<float>? position = element.Position;
                Vector3D<float>? size = element.Size;
                if (position != null && size != null)
                {
                    var elementRect = new Rectangle<float>(position.Value.X, position.Value.Y, size.Value.X, size.Value.Y);
                    if (elementRect.Contains(new Vector2D<float>(mouse.Position.X, mouse.Position.Y)))
                    {
                        elementEvents.OnMouseDown(mouse, button);
                        if (!_clickedObjects.Contains(clickedElement))
                            _clickedObjects.Add(clickedElement);
                    }
                }
            }
        }
    }

    private static void OnMouseUp(IMouse mouse, MouseButton button)
    {
        foreach (var behaviour in Application.Behaviours)
        {
            Element? element = behaviour.GetComponent<Element>();
            if (!(element is IMouseClickEvents elementEvents)) return;
            ClickedElement clickedElement = new ClickedElement(elementEvents, button);
            if (!_clickedObjects.Contains(clickedElement)) return;
            if (element != null && element.Transform != null)
            {
                Vector3D<float>? position = element.Position;
                Vector3D<float>? size = element.Size;
                if (position != null && size != null)
                {
                    var elementRect = new Rectangle<float>(position.Value.X, position.Value.Y, size.Value.X, size.Value.Y);
                    if (elementRect.Contains(new Vector2D<float>(mouse.Position.X, mouse.Position.Y)))
                    {
                        elementEvents.OnMouseUp(mouse, button);
                        if (_clickedObjects.Contains(clickedElement))
                            _clickedObjects.Remove(clickedElement);
                    }
                }
            }
        }
    }

    private static void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        if (!_pressedKeys.Contains(key))
            _pressedKeys.Add(key);
    }

    private static void OnKeyUp(IKeyboard keyboard, Key key, int keyCode)
    {
        if (_pressedKeys.Contains(key))
            _pressedKeys.Remove(key);
    }

    private static bool GetKey(Key key)
    {
        return _pressedKeys.Contains(key);
    }

    public static Vector2D<float> GetMoveInput()
    {
        Vector2D<float> vector = new(0f, 0f);
        bool leftKey = GetKey(Key.A);
        bool rightKey = GetKey(Key.D);
        bool upKey = GetKey(Key.W);
        bool downKey = GetKey(Key.S);
        if (leftKey && !rightKey)
            vector.X = -1;
        if (!leftKey && rightKey)
            vector.X = 1;
        if (leftKey && rightKey)
            vector.X = 0;
        if (!leftKey && !rightKey)
            vector.X = 0;
        if (upKey && !downKey)
            vector.Y = 1;
        if (!upKey && downKey)
            vector.Y = -1;
        if (upKey && downKey)
            vector.Y = 0;
        if (!upKey && !downKey)
            vector.Y = 0;
        if (upKey && leftKey)
        {
            vector.Y = 0.75f;
            vector.X = -0.75f;
        }
        if (upKey && rightKey)
        {
            vector.Y = 0.75f;
            vector.X = 0.75f;
        }
        if (downKey && rightKey)
        {
            vector.Y = -0.75f;
            vector.X = 0.75f;
        }
        if (downKey && leftKey)
        {
            vector.Y = -0.75f;
            vector.X = -0.75f;
        }
        if (!leftKey && !rightKey && !upKey && !downKey)
            vector = Vector2D<float>.Zero;
        return vector;
    }
}
