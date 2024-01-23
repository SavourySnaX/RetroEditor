using ImGuiNET;

class NewProjectDialog : IEditorWindow
{
    public float UpdateInterval => 9999.0f;

    private string projectName;
    private string projectLocation;
    private string importFile;
    private int selectedPlugin;
    public string[] availablePluginNames;

    private Editor editor;
    public void Close()
    {
        // User cancelled
    }

    public bool Draw()
    {
        var disabledCounter = 0;

        if (ImGui.Button("Choose Folder"))
        {
            var dialogResult = NativeFileDialogSharp.Dialog.FolderPicker(projectLocation);
            if (dialogResult.IsOk)
            {
                projectLocation = dialogResult.Path;
                // Validate location empty
            }
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

        if (ImGui.Button("Choose Game"))
        {
            var dialogResult = NativeFileDialogSharp.Dialog.FileOpen(defaultPath: editor.Settings.LastImportedLocation);
            if (dialogResult.IsOk)
            {
                importFile = dialogResult.Path;
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

        if (ImGui.Button("Create Project"))
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
        // Nothing to do
    }

    public bool SetEditor(Editor editor)
    {
        this.editor = editor;
        return true;
    }
}