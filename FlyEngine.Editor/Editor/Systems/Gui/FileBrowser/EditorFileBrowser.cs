using FlyEngine.Core.Engine.SceneManagement;
using FlyEngine.Editor.SceneManagement;
using ImGuiNET;
using MemoryPack;
using Microsoft.Extensions.Logging;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Editor.Systems.Gui;

public class EditorFileBrowser : EditorGuiWindow
{
    private readonly ILogger _logger = new Logger<EditorFileBrowser>(LoggerFactory.Create(builder => builder.AddConsole()));
    
    private string? _currentDirectory = Editor.CurrentProjectPath;
    private string _selectedFile = string.Empty;
    
    protected override string Title => "File Browser";

    private bool _createFile;

    private Type? _currentCreateType;

    private string _fileName = string.Empty;

    protected override void BeforeBegin()
    {
        ImGuiNet.SetNextWindowDockID(EditorGui.BottomDockId);
    }

    protected internal override void OnLoad()
    {
        Editor.OnCurrentProjectPathChanged += OnCurrentProjectPathChanged;
    }

    protected internal override void OnUnload()
    {
        Editor.OnCurrentProjectPathChanged -= OnCurrentProjectPathChanged;
    }

    private void OnCurrentProjectPathChanged(string? newPath)
    {
        _currentDirectory = newPath;
    }

    protected override void OnRender(double deltaTime)
    {
        if (_currentDirectory == null) return;
        if (ImGuiNet.Button("<"))
        {
            var parent = Directory.GetParent(_currentDirectory);
            if (parent != null) _currentDirectory = parent.FullName;
        }
        ImGuiNet.SameLine();
        ImGuiNet.Text($"Current: {_currentDirectory}");
        ImGuiNet.Separator();
        
        if (ImGuiNet.BeginChild("Files"))
        {
            foreach (var dir in Directory.GetDirectories(_currentDirectory))
            {
                if (ImGuiNet.Selectable($"[Folder] {Path.GetFileName(dir)}", false))
                    _currentDirectory = dir;
            }
            CreateFileContextWindow();

            foreach (var file in Directory.GetFiles(_currentDirectory))
            {
                var isSelected = (_selectedFile == file);
                if (ImGuiNet.Selectable(Path.GetFileName(file), isSelected))
                    _selectedFile = file;
                FileContextWindow(file);

                if (ImGuiNet.IsItemHovered() && ImGuiNet.IsMouseDoubleClicked(0))
                    HandleFileOpen(file);
            }

            if (_createFile && _currentCreateType != null)
            {
                ImGuiNet.SetKeyboardFocusHere();

                if (ImGuiNet.InputText("New " + _currentCreateType.Name, ref _fileName, 100, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    ExecuteFileCreation(_fileName);
                    StopCreation();
                }
                else if (ImGuiNet.IsItemDeactivated() && !ImGuiNet.IsKeyPressed(ImGuiKey.Enter) && !ImGuiNet.IsKeyPressed(ImGuiKey.KeypadEnter))
                    StopCreation();
            }
        }
        ImGuiNet.EndChild();
    }
    
    private void StopCreation()
    {
        _createFile = false;
        _fileName = string.Empty;
        _currentCreateType = null;
    }
    
    private void ExecuteFileCreation(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;

        var extension = _currentCreateType == typeof(Scene) ? ".scene" : ".data";
        var fullPath = Path.Combine(_currentDirectory, name + extension);

        if (File.Exists(fullPath))
        {
            _logger.LogWarning("File already exists!");
            return;
        }

        try
        {
            if (_currentCreateType == typeof(Scene))
            {
                var newScene = new Scene(Guid.NewGuid());

                var bin = MemoryPackSerializer.Serialize(newScene);
                File.WriteAllBytes(fullPath, bin);
            }
        
            _logger.LogInformation($"Successfully created: {fullPath}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to create file: {ex.Message}");
        }
    }

    private void FileContextWindow(string file)
    {
        if (ImGuiNet.BeginPopupContextItem("FileContextMenu" + file))
        {
            _selectedFile = file;
            if (ImGuiNet.MenuItem("Delete File"))
            {
                try 
                {
                    File.Delete(file);
                    _logger.LogInformation($"Deleted: {file}");
                }
                catch (Exception ex) 
                {
                    _logger.LogError($"Delete failed: {ex.Message}");
                }
            }
            if (ImGuiNet.MenuItem("Rename")) { }
            ImGuiNet.EndPopup();
        }
    }

    private void CreateFileContextWindow()
    {
        if (ImGuiNet.BeginPopupContextWindow("CreateFileContextMenu"))
        {
            if (ImGuiNet.MenuItem("New Scene"))
            {
                _currentCreateType = typeof(Scene);
                _createFile = true;
            }
            
            ImGuiNet.EndPopup();
        }
    }
    
    private async void HandleFileOpen(string path)
    {
        if (path.EndsWith(".scene")) 
        {
            try
            {
                if (SceneManager.CurrentScene != null)
                    await Editor.TaskQueue.Enqueue(SceneSnapshot.CreateSnapshotAsync, SceneManager.CurrentScene, "Creating scene snapshot");
                SceneManager.UnloadScene();

                await Editor.TaskQueue.Enqueue(LoadScene, path);
                
                SceneSnapshot.DeleteSnapshot();
        
                _logger.LogInformation($"Successfully loaded scene: {path}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load scene: {ex}");
                await Editor.TaskQueue.Enqueue(SceneSnapshot.RestoreSnapshotAsync, "Restoring scene");
            }
        }
    }

    private async Task LoadScene(string path)
    {
        var bytes = await File.ReadAllBytesAsync(path);
        var name = Path.GetFileName(path).Replace(".scene", "");
        var scene = MemoryPackSerializer.Deserialize<Scene>(bytes);
        if (scene == null)
            throw new Exception("Deserialize failed");
        scene.Path = path;
        scene.Name = name;
        await SceneManager.LoadSceneAsync(scene);
    }
}