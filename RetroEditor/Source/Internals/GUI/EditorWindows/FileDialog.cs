using ImGuiNET;

class FileDialog : IWindow
{
    public float UpdateInterval => 9999.0f;

    public enum FileDialogType
    {
        Open,
        Save,
        FolderPicker
    }

    private FileDialogType dialogType;
    private string title;
    private string defaultPath;
    private string[] filters;
    private Action<string> onSelected;

    private string[] currentFiles = Array.Empty<string>();
    private string[] currentDirectories = Array.Empty<string>();

    public FileDialog(FileDialogType dialogType, string title, string defaultPath, string[] filters, Action<string> onSelected)
    {
        this.dialogType = dialogType;
        this.title = title;
        this.defaultPath = defaultPath;
        this.filters = filters;
        this.onSelected = onSelected;

        currentFiles = Directory.GetFiles(defaultPath);
        currentDirectories = Directory.GetDirectories(defaultPath);
    }

    public void Close()
    {
    }

    public bool Draw()
    {
        // NativeFileDialog does not have arm64 builds
        ImGui.InputText("Path", ref defaultPath, 1024);
        
        ImGui.BeginChild("FileList", new System.Numerics.Vector2(0, -ImGui.GetFrameHeightWithSpacing()));
        foreach (var dir in currentDirectories)
        {
            if (ImGui.Selectable($"[DIR] {Path.GetFileName(dir)}"))
            {
                defaultPath = dir;
                currentFiles = Directory.GetFiles(defaultPath);
                currentDirectories = Directory.GetDirectories(defaultPath);
            }
        }
        foreach (var file in currentFiles)
        {
            if (ImGui.Selectable(Path.GetFileName(file)))
            {
                onSelected?.Invoke(file);
            }
        }
        ImGui.EndChild();
        if (ImGui.Button("Cancel"))
        {
            onSelected?.Invoke(null);
        }
        return false;
    }

    public bool Initialise()
    {
        return true;
    }

    public void Update(float seconds)
    {

    }
}