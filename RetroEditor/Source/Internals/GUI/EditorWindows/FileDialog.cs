using ImGuiNET;
using RetroEditor.Source.Internals.GUI;

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
    private string restorePath;
    private string[] filters;
    private Func<string,bool> onSelected;

    private string[] currentFiles = Array.Empty<string>();
    private string[] currentDirectories = Array.Empty<string>();

    public FileDialog(FileDialogType dialogType, string title, string defaultPath, string[] filters, Func<string,bool> onSelected)
    {
        this.dialogType = dialogType;
        this.title = title;
        this.filters = filters;
        this.onSelected = onSelected;

        if (defaultPath == "")
        {
            defaultPath = Directory.GetCurrentDirectory();
        }
        this.defaultPath = defaultPath;
        this.restorePath = defaultPath;

        currentFiles = Directory.GetFiles(defaultPath);
        currentDirectories = Directory.GetDirectories(defaultPath);
    }

    public void Close()
    {
    }

    public void UpdatePath(string path)
    {
        defaultPath = path;
        restorePath = path;
        currentFiles = Directory.GetFiles(defaultPath);
        currentDirectories = Directory.GetDirectories(defaultPath);
    }

    public bool Draw()
    {
        bool shouldClose = false;
        // NativeFileDialog does not have arm64 builds, so we are 
        //just going to remove it, and use a custom ImGui file dialog instead.
        if (ImGui.InputText("Path", ref defaultPath, 1024, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            if (Directory.Exists(defaultPath))
            {
                UpdatePath(defaultPath);
            }
            else
            {
                UpdatePath(restorePath);
            }
        }

        var numEntries = currentDirectories.Length + currentFiles.Length + 1;
        if (dialogType== FileDialogType.FolderPicker)
        {
            numEntries = currentDirectories.Length + 1;
            ImGui.SameLine();
            if (AbiSafe_ImGuiWrapper.Button("Select Folder"))
            {
                shouldClose = onSelected.Invoke(defaultPath);
            }
        }

        if (AbiSafe_ImGuiWrapper.BeginChild("Scrolling", new System.Numerics.Vector2(0, 0), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar))
        {
            unsafe
            {
                var clipper = ImGuiNative.ImGuiListClipper_ImGuiListClipper();
                ImGuiNative.ImGuiListClipper_Begin(clipper, numEntries, -1.0f);
                bool changed = false;
                while (ImGuiNative.ImGuiListClipper_Step(clipper) != 0)
                {
                    if (changed) break;
                    for (int i = clipper->DisplayStart; i < clipper->DisplayEnd; i++)
                    {
                        if (i == 0)
                        {
                            AbiSafe_ImGuiWrapper.Selectable("[DIR] ..");
                            if (ImGui.IsItemClicked())
                            {
                                if (Path.GetPathRoot(defaultPath) != defaultPath)
                                {
                                    UpdatePath(Path.GetDirectoryName(defaultPath) ?? "");
                                    changed = true;
                                    break;
                                }
                            }
                        }
                        else if (i-1 < currentDirectories.Length)
                        {
                            var dir = currentDirectories[i - 1];
                            AbiSafe_ImGuiWrapper.Selectable($"[DIR] {Path.GetFileName(dir)}");
                            if (ImGui.IsItemClicked())
                            {
                                UpdatePath(dir);
                                changed = true;
                                break;
                            }
                        }
                        else
                        {
                            var file = currentFiles[i - 1 - currentDirectories.Length];
                            AbiSafe_ImGuiWrapper.Selectable(Path.GetFileName(file));
                            if (ImGui.IsItemClicked())
                            {
                                shouldClose = onSelected.Invoke(file);
                            }
                        }
                    }
                }
                ImGuiNative.ImGuiListClipper_End(clipper);
                ImGuiNative.ImGuiListClipper_destroy(clipper);
            }
            if (ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
            {
                ImGui.SetScrollHereY(1.0f);
            }
        }
        ImGui.EndChild();
        return shouldClose;
    }

    public bool Initialise()
    {
        return true;
    }

    public void Update(float seconds)
    {

    }
}