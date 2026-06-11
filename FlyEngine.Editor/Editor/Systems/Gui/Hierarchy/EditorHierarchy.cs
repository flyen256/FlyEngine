using System.Runtime.InteropServices;
using FlyEngine.Core;
using FlyEngine.Core.Components.Common;
using FlyEngine.Core.SceneManagement;
using ImGuiNET;
using MemoryPack;
using Microsoft.Extensions.Logging;
using Silk.NET.Input;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Editor.Systems.Gui;

public class EditorHierarchy : EditorGuiWindow
{
    private static readonly ILogger Logger = new Logger<EditorHierarchy>(LoggerFactory.Create(b => b.AddConsole()));

    public static EditorHierarchy? Instance { get; private set; }

    protected override string Title =>
        "Hierarchy" + (EditorAction.IsDirty ? " *" : string.Empty) + "###EditorHierarchy";

    private bool _createGameObject;
    private string _gameObjectName = string.Empty;

    private static GameObject? _renamingGameObject;
    private static string _renameBuffer = string.Empty;

    private static Scene? Scene => SceneManager.CurrentScene;

    public EditorHierarchy()
    {
        Instance = this;
    }

    protected override void BeforeBegin()
    {
        ImGuiNet.SetNextWindowDockID(EditorGui.LeftDockId);
    }

    protected internal override async void OnUpdate(double deltaTime)
    {
        try
        {
            if (!Input.GetKey(Key.S) || !Input.GetKey(Key.ControlLeft) || !EditorAction.IsDirty) return;
            await Editor.TaskQueue.Enqueue(TrySaveScene, "Saving scene");
            EditorAction.IsDirty = false;
        }
        catch (Exception e)
        {
            Logger.LogError("{error}", e);
        }
    }

    protected override void OnRender(double deltaTime)
    {
        if (Scene == null)
        {
            ImGuiNet.Text("No Scene Selected");
            return;
        }

        ImGuiNet.SetNextItemOpen(true, ImGuiCond.Once);
        if (ImGuiNet.CollapsingHeader(Scene.Name))
        {
            if (ImGuiNet.BeginChild("GameObjects"))
            {
                if (ImGuiNet.IsWindowHovered() && ImGuiNet.IsMouseReleased(ImGuiMouseButton.Left) && !ImGuiNet.IsMouseDragging(ImGuiMouseButton.Left))
                    Editor.SelectionManager.SelectedGameObject = null;
                CreateGameObjectContextWindow();

                if (ImGuiNet.IsWindowFocused() && ImGuiNet.IsKeyPressed(ImGuiKey.F2) &&
                    Editor.SelectionManager.SelectedGameObject != null)
                {
                    StartRename(Editor.SelectionManager.SelectedGameObject);
                }

                var gameObjectsSpan = CollectionsMarshal.AsSpan((List<GameObject>)Scene.GameObjects);
                for (var i = 0; i < gameObjectsSpan.Length; i++)
                {
                    var gameObject = gameObjectsSpan[i];
                    if (gameObject.IsDestroyed || gameObject.Transform.Parent != null) continue;
                    RenderGameObjectNode(gameObject);
                }

                if (_createGameObject)
                {
                    ImGuiNet.SetKeyboardFocusHere();
                    if (ImGuiNet.InputText("New Game Object", ref _gameObjectName, 100,
                            ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        ExecuteGameObjectCreation(_gameObjectName);
                        StopCreation();
                    }
                    else if (ImGuiNet.IsItemDeactivated() && !ImGuiNet.IsKeyPressed(ImGuiKey.Enter) &&
                             !ImGuiNet.IsKeyPressed(ImGuiKey.KeypadEnter))
                    {
                        if (_gameObjectName.Length > 0) ExecuteGameObjectCreation(_gameObjectName);
                        StopCreation();
                    }
                }
            }

            ImGuiNet.EndChild();
        }
    }

    private static void RenderGameObjectNode(GameObject gameObject)
    {
        if (gameObject.IsDestroyed) return;

        var transform = gameObject.Transform;

        var validChildrenCount = transform.Children.Count(child => !child.GameObject.IsDestroyed);

        var hasChildren = validChildrenCount > 0;

        var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick |
                    ImGuiTreeNodeFlags.SpanAvailWidth;

        if (Editor.SelectionManager.SelectedGameObject == gameObject)
            flags |= ImGuiTreeNodeFlags.Selected;

        if (!hasChildren)
            flags |= ImGuiTreeNodeFlags.Leaf;

        if (_renamingGameObject == gameObject)
        {
            ImGuiNet.SetKeyboardFocusHere();

            if (ImGuiNet.InputText("##RenameInput", ref _renameBuffer, 100, ImGuiInputTextFlags.EnterReturnsTrue))
                ApplyRename(gameObject);
            else if (ImGuiNet.IsItemDeactivated() && !ImGuiNet.IsKeyPressed(ImGuiKey.Enter) &&
                     !ImGuiNet.IsKeyPressed(ImGuiKey.KeypadEnter))
                ApplyRename(gameObject);
            return;
        }
        var isNodeOpen = ImGuiNet.TreeNodeEx($"{gameObject.Name}###GO_{gameObject.GetHashCode()}", flags);
        if (ImGuiNet.IsItemHovered() && ImGuiNet.IsMouseReleased(ImGuiMouseButton.Left))
        {
            if (!ImGuiNet.IsMouseDragging(ImGuiMouseButton.Left))
            {
                if (!ImGuiNet.IsItemToggledOpen())
                    Editor.SelectionManager.SelectedGameObject = gameObject;
            }
        }

        GameObjectContextWindow(gameObject);
        if (ImGuiNet.BeginDragDropSource())
        {
            unsafe
            {
                ImGuiNet.SetDragDropPayload("GAMEOBJECT_NODE", (IntPtr)(&gameObject), (uint)sizeof(IntPtr));
            }

            ImGuiNet.Text(gameObject.Name);
            ImGuiNet.EndDragDropSource();
        }

        if (ImGuiNet.BeginDragDropTarget())
        {
            unsafe
            {
                var payload = ImGuiNet.AcceptDragDropPayload("GAMEOBJECT_NODE");
                if (payload.NativePtr != null)
                {
                    var draggedOuter = *(GameObject*)payload.Data;

                    if (draggedOuter != gameObject && !IsChildOf(gameObject.Transform, draggedOuter.Transform))
                    {
                        draggedOuter.Transform.Parent = gameObject.Transform;
                        EditorAction.MarkDirty();
                    }
                }

                ImGuiNet.EndDragDropTarget();
            }
        }

        if (isNodeOpen)
        {
            var childrenCopy = transform.Children.ToList();
            foreach (var childTransform in childrenCopy)
            {
                if (!childTransform.GameObject.IsDestroyed)
                    RenderGameObjectNode(childTransform.GameObject);
            }

            ImGuiNet.TreePop();
        }
    }

    private static bool IsChildOf(Transform potentialParent, Transform potentialChild)
    {
        var current = potentialParent;
        while (current != null)
        {
            if (current == potentialChild) return true;
            current = current.Parent;
        }

        return false;
    }

    private static void GameObjectContextWindow(GameObject gameObject)
    {
        if (ImGuiNet.BeginPopupContextItem($"GameObjectContext_{gameObject.GetHashCode()}"))
        {
            if (ImGuiNet.MenuItem("Rename"))
                StartRename(gameObject);

            ImGuiNet.Separator();

            if (ImGuiNet.MenuItem("Create Child Game Object"))
            {
                var child = GameObject.Create("New Child");
                child.Transform.Parent = gameObject.Transform;
                EditorAction.MarkDirty();
            }

            if (gameObject.Transform.Parent != null)
            {
                if (ImGuiNet.MenuItem("Detach (Make Root)"))
                {
                    gameObject.Transform.Parent = null;
                    EditorAction.MarkDirty();
                }
            }

            ImGuiNet.Separator();

            if (ImGuiNet.MenuItem("Delete Game Object"))
                DeleteSelectedGameObject(gameObject);

            ImGuiNet.EndPopup();
        }
    }

    private static void StartRename(GameObject gameObject)
    {
        _renamingGameObject = gameObject;
        _renameBuffer = gameObject.Name;
    }

    private static void ApplyRename(GameObject gameObject)
    {
        if (!string.IsNullOrWhiteSpace(_renameBuffer))
        {
            gameObject.Name = _renameBuffer;
            EditorAction.MarkDirty();
        }

        _renamingGameObject = null;
        _renameBuffer = string.Empty;
    }

    private static void ExecuteGameObjectCreation(string name)
    {
        GameObject.Create(name);
        EditorAction.MarkDirty();
    }

    private void StopCreation()
    {
        _createGameObject = false;
        _gameObjectName = string.Empty;
    }

    private static void DeleteSelectedGameObject(GameObject gameObject)
    {
        var childrenCopy = gameObject.Transform.Children.ToList();
        foreach (var child in childrenCopy)
        {
            child.Parent = null;
        }

        gameObject.Transform.Parent = null;
        gameObject.Destroy();
        if (gameObject == Editor.SelectionManager.SelectedGameObject) Editor.SelectionManager.SelectedGameObject = null;
        EditorAction.MarkDirty();
    }

    private void CreateGameObjectContextWindow()
    {
        if (ImGuiNet.BeginPopupContextWindow("HierarchyContextWindow"))
        {
            if (ImGuiNet.MenuItem("New Game Object"))
                _createGameObject = true;
            ImGuiNet.EndPopup();
        }
    }

    private async Task TrySaveScene()
    {
        if (Scene?.Path == null || Application.IsRunning) return;
        var fs = File.Open(Scene.Path, FileMode.Create);
        await MemoryPackSerializer.SerializeAsync(fs, Scene);
        fs.Close();
    }
}