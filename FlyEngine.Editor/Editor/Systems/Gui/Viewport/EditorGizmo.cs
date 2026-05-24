using System.Numerics;
using FlyEngine.Core;
using FlyEngine.Core.Components.Common;
using FlyEngine.Core.Extensions;
using ImGuiNET;
using Silk.NET.Maths;

namespace FlyEngine.Editor.Systems.Gui;

public static class EditorGizmo
{
    public enum Operation
    {
        Translate = 0,
        Rotate = 1,
        Scale = 2
    }

    private static bool _pressedX;
    private static bool _pressedY;
    private static bool _pressedZ;

    public static void DrawGizmo(
        Operation operation,
        ImDrawListPtr drawListPtr,
        Transform transform,
        Vector2 screenPos,
        Vector2 windowPos)
    {
        switch (operation)
        {
            case Operation.Translate:
                DrawTranslate(drawListPtr, transform, screenPos, windowPos);
                break;
            case Operation.Rotate:
                DrawRotate(drawListPtr, transform, windowPos);
                break;
            default:
                break;
        }
    }

    private static void DrawTranslate(
        ImDrawListPtr drawListPtr,
        Transform transform,
        Vector2 screenPos,
        Vector2 windowPos)
    {
        var mousePosition = ImGui.GetMousePos();
        var xColor = ImGui.GetColorU32(new Vector4(0.75f, 0.0f, 0.0f, 1.0f));
        var yColor = ImGui.GetColorU32(new Vector4(0.0f, 0.75f, 0.0f, 1.0f));
        var zColor = ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.75f, 1.0f));
        var scale = GetGizmoScale(transform.Position);
        var endX = WorldToScreen(transform.Position + transform.Right * scale) + windowPos;
        var endY = WorldToScreen(transform.Position + transform.Up * scale) + windowPos;
        var endZ = WorldToScreen(transform.Position + transform.Forward * scale) + windowPos;
        var isOverX = IsMouseOverLine(mousePosition, screenPos, endX) || _pressedX;
        var isOverY = IsMouseOverLine(mousePosition, screenPos, endY) || _pressedY;
        var isOverZ = IsMouseOverLine(mousePosition, screenPos, endZ) || _pressedZ;
        if (isOverX)
        {
            xColor = ImGui.GetColorU32(new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
            isOverY = false;
            isOverZ = false;
        }
        else if (isOverY)
        {
            yColor = ImGui.GetColorU32(new Vector4(0.0f, 1.0f, 0.0f, 1.0f));
            isOverZ = false;
        }
        else if (isOverZ)
        {
            zColor = ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 1.0f, 1.0f));
        }
        IsMouseOverLine(mousePosition, screenPos, endX);
        if (ValidatePosition(endX, windowPos))
            drawListPtr.AddLine(screenPos, endX, xColor, 5);
        if (ValidatePosition(endY, windowPos))
            drawListPtr.AddLine(screenPos, endY, yColor, 5);
        if (ValidatePosition(endZ, windowPos))
            drawListPtr.AddLine(screenPos, endZ, zColor, 5);
        if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            if (isOverX && !_pressedY && !_pressedZ)
            {
                Input.CursorVisible = false;
                _pressedX = true;
            }
            else if (isOverY && !_pressedX && !_pressedZ)
            {
                Input.CursorVisible = false;
                _pressedY = true;
            }
            else if (isOverZ && !_pressedY && !_pressedX)
            {
                Input.CursorVisible = false;
                _pressedZ = true;
            }
        }
        if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
        {
            if (_pressedX)
            {
                Input.CursorVisible = true;
                _pressedX = false;
            }
            if (_pressedY)
            {
                Input.CursorVisible = true;
                _pressedY = false;
            }
            if (_pressedZ)
            {
                Input.CursorVisible = true;
                _pressedZ = false;
            }
        }

        const float moveSpeed = 0.01f;
        var deltaMove = Vector3.Zero;
        if (_pressedX)
            deltaMove = transform.Right * (Input.MouseInput.X * moveSpeed);
        if (_pressedY)
            deltaMove = transform.Up * (Input.MouseInput.X * moveSpeed);
        if (_pressedZ)
            deltaMove = transform.Forward * (Input.MouseInput.X * moveSpeed);
        if (deltaMove == Vector3.Zero) return;
        EditorAction.MarkDirty();
        transform.Position += deltaMove;
    }

    private static void DrawRotate(
        ImDrawListPtr drawListPtr,
        Transform transform,
        Vector2 windowPos)
    {
        var scale = GetGizmoScale(transform.Position);

        var anyOtherHovered = false;
        anyOtherHovered = DrawRotationCircle(
            drawListPtr,
            transform,
            transform.Right,
            new Vector4(1, 0, 0, 1),
            scale,
            windowPos,
            ref _pressedX, 
            "X",
            anyOtherHovered);
        anyOtherHovered = DrawRotationCircle(
            drawListPtr,
            transform,
            transform.Up,
            new Vector4(0, 1, 0, 1),
            scale,
            windowPos,
            ref _pressedY,
            "Y",
            anyOtherHovered);
        anyOtherHovered = DrawRotationCircle(
            drawListPtr,
            transform,
            transform.Forward,
            new Vector4(0, 0, 1, 1),
            scale,
            windowPos,
            ref _pressedZ,
            "Z",
            anyOtherHovered);
    }

    private static bool DrawRotationCircle(
        ImDrawListPtr drawListPtr,
        Transform transform,
        Vector3 axis,
        Vector4 color,
        float radius,
        Vector2 windowPos,
        ref bool pressed,
        string axisName,
        bool anyOtherHovered)
    {
        const int segments = 32;
        var points = new Vector2[segments + 1];
        var isHovered = false;
        var mousePos = ImGui.GetMousePos();

        var perp1 = Vector3.Normalize(Vector3.Cross(axis, Math.Abs(axis.Y) > 0.9f ? Vector3.UnitX : Vector3.UnitY));
        var perp2 = Vector3.Normalize(Vector3.Cross(axis, perp1));

        for (var i = 0; i <= segments; i++)
        {
            var angle = (i / (float)segments) * MathF.PI * 2.0f;
            var pointWorld = transform.Position + (perp1 * MathF.Cos(angle) + perp2 * MathF.Sin(angle)) * radius;
            points[i] = WorldToScreen(pointWorld) + windowPos;

            if (i > 0)
            {
                if (IsMouseOverLine(mousePos, points[i - 1], points[i], 10.0f) && !anyOtherHovered)
                    isHovered = true;
            }
        }

        var col = ImGui.GetColorU32((isHovered || pressed) ? color : color * 0.6f);

        for (var i = 0; i < segments; i++)
        {
            if (ValidatePosition(points[i], windowPos) && ValidatePosition(points[i+1], windowPos))
                drawListPtr.AddLine(points[i], points[i+1], col, (isHovered || pressed) ? 4.0f : 2.5f);
        }

        if (isHovered && ImGui.IsMouseDown(ImGuiMouseButton.Left) && !AnyOtherPressed(axisName))
        {
            pressed = true;
            Input.CursorVisible = false;
        }

        if (pressed)
        {
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                pressed = false;
                Input.CursorVisible = true;
            }
            else
            {
                const float rotSpeed = 0.02f; 
                var delta = (Input.MouseInput.X + -Input.MouseInput.Y) * rotSpeed;

                var deltaRotation = Quaternion.CreateFromAxisAngle(axis, delta);
                
                var targetRotation = Quaternion.Normalize(deltaRotation * transform.Rotation);
                if (transform.Rotation != targetRotation)
                    EditorAction.MarkDirty();
                transform.Rotation = targetRotation;
            }
        }
        return isHovered;
    }

    private static bool AnyOtherPressed(string current)
    {
        if (current != "X" && _pressedX) return true;
        if (current != "Y" && _pressedY) return true;
        if (current != "Z" && _pressedZ) return true;
        return false;
    }

    private static bool IsMouseOverLine(Vector2 mouse, Vector2 start, Vector2 end, float threshold = 8.0f)
    {
        var line = end - start;
        var lenSq = line.LengthSquared();
    
        if (lenSq == 0) return Vector2.Distance(mouse, start) <= threshold;

        var t = Vector2.Dot(mouse - start, line) / lenSq;
        t = Math.Clamp(t, 0.0f, 1.0f);

        var projection = start + t * line;

        return Vector2.Distance(mouse, projection) <= threshold;
    }
    
    private static float GetGizmoScale(Vector3 worldPos)
    {
        if (Editor.Window == null) return 1.0f;
    
        var cameraPos = Editor.Window.EditorCameraPosition;
        var distance = Vector3.Distance(worldPos, cameraPos);
    
        const float baseScale = 0.15f; 
    
        return distance * baseScale;
    }

    private static bool ValidatePosition(Vector2 position, Vector2 windowPos)
    {
        return Math.Abs(position.X - (windowPos.X - 1)) > 0.001f || Math.Abs(position.Y - (windowPos.Y - 1)) > 0.001f;
    }
    
    public static Vector2 WorldToScreen(Vector3 worldPos)
    {
        if (Editor.Window == null) return Vector2.Zero;
        var view = Editor.Window.EditorCameraViewMatrix;
        var projection = Editor.Window.EditorCameraProjectionMatrix;
        var width = Editor.Window.EditorViewport.X;
        var height = Editor.Window.EditorViewport.Y;
        var clipSpace = Vector4.Transform(worldPos, view * projection);
    
        if (clipSpace.W <= 0) return new Vector2(-1, -1);

        var ndc = new Vector3(clipSpace.X, clipSpace.Y, clipSpace.Z) / clipSpace.W;

        return new Vector2(
            (ndc.X + 1.0f) * 0.5f * width,
            (ndc.Y + 1.0f) * 0.5f * height
        );
    }
}