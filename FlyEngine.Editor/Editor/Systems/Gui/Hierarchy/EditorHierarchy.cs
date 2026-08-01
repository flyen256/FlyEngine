using System.Runtime.InteropServices;
using FlyEngine.Core;
using FlyEngine.Core.Components;
using FlyEngine.Core.SceneManagement;
using ImGuiNET;
using MemoryPack;
using Microsoft.Extensions.Logging;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Editor.Systems;

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
                    Selection.SelectedObject = null;
                CreateGameObjectContextWindow();

                if (ImGuiNet.IsWindowFocused() && ImGuiNet.IsKeyPressed(ImGuiKey.F2) &&
                    Selection.SelectedObject != null)
                    StartRename(Selection.SelectedObject);

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

        var validChildrenCount = gameObject.Transform.ChildrenGameObjects.Count(child => !child.IsDestroyed);

        var hasChildren = validChildrenCount > 0;

        var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick |
                    ImGuiTreeNodeFlags.SpanAvailWidth;

        if (Selection.SelectedObject == gameObject)
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
        var isNodeOpen = ImGuiNet.TreeNodeEx($"{gameObject.Name} (Entity id: {gameObject.EntityId})###GO_{gameObject.GetHashCode()}", flags);
        if (ImGuiNet.IsItemHovered() && ImGuiNet.IsMouseReleased(ImGuiMouseButton.Left))
        {
            if (!ImGuiNet.IsMouseDragging(ImGuiMouseButton.Left))
            {
                if (!ImGuiNet.IsItemToggledOpen())
                    Selection.SelectedObject = gameObject;
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
            ImGuiNet.Text($"(Entity id: {gameObject.EntityId})");
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
                        var tr = draggedOuter.Transform;
                        tr.SetParent(gameObject);
                        draggedOuter.Transform = tr;
                        EditorAction.MarkDirty();
                    }
                }

                ImGuiNet.EndDragDropTarget();
            }
        }

        if (isNodeOpen)
        {
            var children = gameObject.Transform.ChildrenGameObjects;
            foreach (var childTransform in children)
            {
                if (!childTransform.IsDestroyed)
                    RenderGameObjectNode(childTransform);
            }

            ImGuiNet.TreePop();
        }
    }

    private static bool IsChildOf(TransformComponent potentialParent, TransformComponent potentialChild)
    {
        var current = potentialChild.Parent;

        while (current.HasValue)
        {
            if (current.Value.GameObject.EntityId == potentialParent.GameObject.EntityId) 
                return true;
            
            current = current.Value.Parent;
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
                var transform = child.Transform;
                transform.SetParent(gameObject);
                child.Transform = transform;
                EditorAction.MarkDirty();
            }

            if (gameObject.Transform.Parent != null)
            {
                if (ImGuiNet.MenuItem("Detach (Make Root)"))
                {
                    var transform = gameObject.Transform;
                    transform.SetParent(null);
                    gameObject.Transform = transform;
                    EditorAction.MarkDirty();
                }
            }

            ImGuiNet.Separator();

            if (ImGuiNet.MenuItem("Delete Game Object"))
                DeleteSelectedGameObject(gameObject);

            ImGuiNet.EndPopup();
        }
    }

    private static void StartRename(Core.Components.Object obj)
    {
        if (obj is not GameObject gameObject) return;
        _renamingGameObject = gameObject;
        _renameBuffer = gameObject.Name;
    }

    private static void ApplyRename(GameObject gameObject)
    {
        if (!string.IsNullOrWhiteSpace(_renameBuffer) && !_renameBuffer.Equals(gameObject.Name))
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

    private static void DeleteSelectedGameObject(Core.Components.Object obj)
    {
        obj.Destroy();
        if (obj == Selection.SelectedObject) Selection.SelectedObject = null;
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
}