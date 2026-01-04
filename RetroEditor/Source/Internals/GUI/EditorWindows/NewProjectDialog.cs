using ImGuiNET;
using RetroEditor.Source.Internals.GUI;

class NewProjectDialog : IWindow
{
    public float UpdateInterval => 1/30f;

    private string projectName="";
    private string projectLocation="";
    private string importFile="";
    private int selectedPlugin;
    public string[] availablePluginNames=Array.Empty<string>();

    private Editor editor;
    private bool requestFileDialog = false;
    private bool requestFolderDialog = false;
    public NewProjectDialog(Editor editor)
    {
        this.editor = editor;
    }

    public void Close()
    {
        // User cancelled
    }

    public bool Draw()
    {
        var disabledCounter = 0;

        if (AbiSafe_ImGuiWrapper.Button("Choose Folder"))
        {
            requestFolderDialog = true;
        }
        ImGui.SameLine();
        if (projectLocation == "")
        {
            ImGui.LabelText("No project folder Selected", "Choose a folder to create the project in, a folder named <ProjectName> will be created there.");
        }
        else
        {
            if (!Directory.Exists(projectLocation))
            {
                ImGui.LabelText("Invalid Location!", projectLocation);
            }
            else
            {
                ImGui.LabelText("", projectLocation);
            }
        }

        if (projectLocation == "" || !Directory.Exists(projectLocation))
        {
            disabledCounter++;
            if (disabledCounter == 1)
            {
                ImGui.BeginDisabled();
            }
        }

        ImGui.Text("Choose a game and plugin to import the game with, choosing a game will automatically select a compatable plugin if available.");
        if (ImGui.Combo("Choose Game To Modify", ref selectedPlugin, availablePluginNames, availablePluginNames.Length))
        {
        }

        if (!(requestFileDialog||requestFolderDialog) && AbiSafe_ImGuiWrapper.Button("Choose Game"))
        {
            requestFileDialog = true;
        }
        ImGui.SameLine();
        if (importFile == "")
        {
            ImGui.LabelText("No game selected", "Choose a game to import, the game needs to be supported by one of the editor plugins.");
        }
        else
        {
            ImGui.LabelText("", importFile);
        }

        if (importFile == "" || !File.Exists(importFile) || selectedPlugin == 0 || !editor.IsPluginSuitable(availablePluginNames[selectedPlugin], importFile))
        {
            disabledCounter++;
            if (disabledCounter == 1)
            {
                ImGui.BeginDisabled();
            }
        }
        if (ImGui.InputTextWithHint("Project Name", "Enter a name for the project", ref projectName, 256))
        {
            // Validate name?
        }
        if (projectName == "")
        {
            disabledCounter++;
            if (disabledCounter == 1)
            {
                ImGui.BeginDisabled();
            }
        }
        ImGui.Separator();

        if (AbiSafe_ImGuiWrapper.Button("Create Project"))
        {
            editor.Settings.ProjectLocation = projectLocation;
            ImGui.CloseCurrentPopup();
            editor.CreateNewProject(projectName, projectLocation, importFile, availablePluginNames[selectedPlugin]);
            return true;
        }
        if (disabledCounter > 0)
        {
            ImGui.EndDisabled();
        }
        return false;
    }

    public bool Initialise()
    {
        // TODO editor settings file, so we can remember things!
        projectName = "";
        projectLocation = editor.Settings.ProjectLocation;
        importFile = "";

        var plugins = editor.Plugins.ToList();
        plugins.Insert(0, "");
        availablePluginNames = plugins.ToArray();
        return true;
    }

    public void Update(float seconds)
    {
        if (requestFileDialog)
        {
            requestFileDialog = false;
            editor.OpenFileDialog(FileDialog.FileDialogType.Open, "Select Game File to Import", editor.Settings.LastImportedLocation, new string[] { "*.*" }, (string path) =>
            {
                if (path != null)
                {
                    importFile = path;
                    editor.Settings.LastImportedLocation = Path.GetDirectoryName(importFile) ?? "";
                    if (projectName == "")
                    {
                        projectName = Path.GetFileNameWithoutExtension(importFile);
                    }
                    selectedPlugin = 0;
                    foreach (var plugin in availablePluginNames)
                    {
                        if (editor.IsPluginSuitable(plugin, importFile))
                        {
                            selectedPlugin = Array.IndexOf(availablePluginNames, plugin);
                            break;
                        }
                    }
                }
                return true;
            });
        }
        if (requestFolderDialog)
        {
            requestFolderDialog = false;
            editor.OpenFileDialog(FileDialog.FileDialogType.FolderPicker, "Select Project Location", editor.Settings.ProjectLocation, new string[] { "*.*" }, (string path) =>
            {
                if (path != null)
                {
                    projectLocation = path;
                }
                return true;
            });
        }
    }

}