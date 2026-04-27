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
    
    protected override string Title => "Hierarchy" + (_isDirty ? " *" : string.Empty) + "###EditorHierarchy";

    private bool _isDirty;
    private bool _createGameObject;
    
    private string _gameObjectName = string.Empty;

    private Scene? Scene => SceneManager.CurrentScene;

    public GameObject? SelectedGameObject { get; private set; }

    public EditorHierarchy()
    {
        Instance = this;
        EditorAction.OnSceneModified += () => _isDirty = true;
    }
    
    protected override void BeforeBegin()
    {
        ImGuiNet.SetNextWindowDockID(EditorGui.LeftDockId);
    }

    protected internal override async void OnUpdate(double deltaTime)
    {
        try
        {
            if (!Input.GetKey(Key.S) || !Input.GetKey(Key.ControlLeft) || !_isDirty) return;
            await Editor.TaskQueue.Enqueue(TrySaveScene, "Saving scene");
            _isDirty = false;
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
        if (ImGuiNet.CollapsingHeader(Scene.Name))
        {
            if (ImGuiNet.BeginChild("GameObjects"))
            {
                CreateGameObjectContextWindow();
                foreach (var gameObject in Scene.GameObjects)
                {
                    var isSelected = SelectedGameObject == gameObject;
                    if (ImGuiNet.Selectable(gameObject.Name, isSelected))
                        SelectedGameObject = gameObject;
                    GameObjectContextWindow(gameObject);
                }

                if (_createGameObject)
                {
                    ImGuiNet.SetKeyboardFocusHere();
                
                    if (ImGuiNet.InputText("New Game Object", ref _gameObjectName, 100, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        ExecuteGameObjectCreation(_gameObjectName);
                        StopCreation();
                    }
                    else if (ImGuiNet.IsItemDeactivated() && !ImGuiNet.IsKeyPressed(ImGuiKey.Enter) &&
                             !ImGuiNet.IsKeyPressed(ImGuiKey.KeypadEnter))
                    {
                        if (_gameObjectName.Length > 0)
                            ExecuteGameObjectCreation(_gameObjectName);
                        StopCreation();
                    }
                }
            }
            ImGuiNet.EndChild();
        }
    }

    private void GameObjectContextWindow(GameObject gameObject)
    {
        if (ImGuiNet.BeginPopupContextItem($"GameObjectContext_{gameObject.GetHashCode()}"))
        {
            if (ImGuiNet.MenuItem("Delete Game Object"))
                DeleteSelectedGameObject(gameObject);
            ImGuiNet.EndPopup();
        }
    }

    private void ExecuteGameObjectCreation(string name)
    {
        GameObject.Create(name);
        _isDirty = true;
    }
    
    private void StopCreation()
    {
        _createGameObject = false;
        _gameObjectName = string.Empty;
    }
    
    private void DeleteSelectedGameObject(GameObject gameObject)
    {
        gameObject.Destroy();
        if (gameObject == SelectedGameObject)
            SelectedGameObject = null;
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