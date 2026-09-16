using System.Reflection;
using FlyEngine.Core.SceneManagement;
using FlyEngine.Editor.SceneManagement;
using ImGuiNET;
using MemoryPack;
using Microsoft.Extensions.Logging;
using ImGuiNet = ImGuiNET.ImGui;

namespace FlyEngine.Editor.Systems;

public class EditorFileBrowser : EditorGuiWindow
{
    private readonly ILogger _logger = new Logger<EditorFileBrowser>(LoggerFactory.Create(builder => builder.AddConsole()));
    
    private string? _currentDirectory = Editor.CurrentProjectPath;
    private string _selectedFile = string.Empty;
    
    protected override string Title => "File Browser";

    private bool _createFile;
    private bool _createFolder;

    private Type? _currentCreateType;

    private string _fileName = string.Empty;
    private string _folderName = string.Empty;

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
            CreateContextWindow();
            foreach (var dir in Directory.GetDirectories(_currentDirectory))
            {
                if (ImGuiNet.Selectable($"[Folder] {Path.GetFileName(dir)}", false))
                    _currentDirectory = dir;
                FolderContextWindow(dir);
            }

            foreach (var file in Directory.GetFiles(_currentDirectory))
            {
                var isSelected = (_selectedFile == file);
                if (ImGuiNet.Selectable(Path.GetFileName(file), isSelected))
                    _selectedFile = file;
                FileContextWindow(file);

                if (ImGuiNet.IsItemHovered() && ImGuiNet.IsMouseDoubleClicked(0))
                    _ = HandleFileOpen(file);
            }

            if (_createFile && _currentCreateType != null)
            {
                ImGuiNet.SetKeyboardFocusHere();

                if (ImGuiNet.InputText("New " + _currentCreateType.Name, ref _fileName, 100, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    ExecuteCreation(_fileName);
                    StopCreation();
                }
                else if (ImGuiNet.IsItemDeactivated() && !ImGuiNet.IsKeyPressed(ImGuiKey.Enter) && !ImGuiNet.IsKeyPressed(ImGuiKey.KeypadEnter))
                    StopCreation();
            }
            else if (_createFolder)
            {
                ImGuiNet.SetKeyboardFocusHere();

                if (ImGuiNet.InputText("New Folder", ref _folderName, 100, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    ExecuteCreation(_folderName);
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
        _createFolder = false;
        _fileName = string.Empty;
        _folderName = string.Empty;
        _currentCreateType = null;
    }
    
    private void ExecuteCreation(string name)
    {
        string fullPath;
        if (!string.IsNullOrWhiteSpace(name) && _createFolder && _currentDirectory != null)
        {
            fullPath = Path.Combine(_currentDirectory, name);
            Directory.CreateDirectory(fullPath);
            return;
        }
        if (string.IsNullOrWhiteSpace(name) || _currentDirectory == null || _currentCreateType == null) return;

        var extension = ".asset";
        
        var fieldInfo = _currentCreateType.GetField("Extension", BindingFlags.Static | BindingFlags.Public);
        if (fieldInfo != null)
            extension = (string)fieldInfo.GetValue(null)!;
        
        fullPath = Path.Combine(_currentDirectory, name + extension);

        if (File.Exists(fullPath))
        {
            _logger.LogWarning("File already exists!");
            return;
        }

        try
        {
            var instance = Activator.CreateInstance(_currentCreateType);

            var bin = MemoryPackSerializer.Serialize(instance);
            File.WriteAllBytes(fullPath, bin);
        
            _logger.LogInformation("Successfully created: {FullPath}", fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to create file: {ExMessage}", ex.Message);
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
                    _logger.LogInformation("Deleted: {File}", file);
                }
                catch (Exception ex) 
                {
                    _logger.LogError("Delete failed: {ExMessage}", ex.Message);
                }
            }
            if (ImGuiNet.MenuItem("Rename")) { }
            ImGuiNet.EndPopup();
        }
    }

    private void FolderContextWindow(string path)
    {
        if (ImGuiNet.BeginPopupContextItem("FolderContextMenu" + path))
        {
            if (ImGuiNet.MenuItem("Delete Folder"))
            {
                try 
                {
                    Directory.Delete(path);
                    _logger.LogInformation("Deleted: {Path}", path);
                }
                catch (Exception ex) 
                {
                    _logger.LogError("Delete failed: {ExMessage}", ex.Message);
                }
            }
            if (ImGuiNet.MenuItem("Rename")) { }
            ImGuiNet.EndPopup();
        }
    }

    private void CreateContextWindow()
    {
        if (ImGuiNet.BeginPopupContextWindow("CreateFileContextMenu"))
        {
            if (ImGuiNet.MenuItem("New Folder"))
                _createFolder = true;
            ImGuiNet.Separator();
            foreach (var type in EditorGui.CreateFileMenuTypes)
            {
                if (ImGuiNet.MenuItem("New " + type.Name))
                {
                    _currentCreateType = type;
                    _createFile = true;
                }
            }
            
            ImGuiNet.EndPopup();
        }
    }
    
    private async Task HandleFileOpen(string path)
    {
        if (path.EndsWith(".scene"))
        {
            try
            {
                await Editor.TaskQueue.Enqueue(SceneManager.LoadScene, path);
                SceneSnapshot.DeleteSnapshot();
        
                _logger.LogInformation("Successfully loaded scene: {Path}", path);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to load scene: {Exception}", ex);
                await SceneSnapshot.RestoreSnapshotAsync();
            }
        }
    }
}