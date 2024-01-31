using Raylib_cs;
using rlImGui_cs;
using ImGuiNET;
using System.Security.Cryptography;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.IO.Compression;

internal struct ActiveProject : IPlayerControls
{
    public ActiveProject(string name, IRetroPlugin retroPlugin, LibRetroPlugin libRetroPlugin, IRomPlugin romPlugin, PlayableRom playableRom, ProjectSettings settings)
    {
        this.name = name;
        this.retroPlugin = retroPlugin;
        this.libRetroPlugin = libRetroPlugin;
        this.romPlugin = romPlugin;
        this.playableRom = playableRom;
        this.settings = settings;
    }

    private string name;
    private IRetroPlugin retroPlugin;
    private LibRetroPlugin libRetroPlugin;
    private IRomPlugin romPlugin;
    private PlayableRom playableRom;
    private ProjectSettings settings;

    public readonly IRetroPlugin RetroPlugin => retroPlugin;
    public readonly LibRetroPlugin LibRetroPlugin => libRetroPlugin;
    public readonly IRomPlugin RomPlugin => romPlugin;
    public readonly ProjectSettings Settings => settings;
    public readonly PlayableRom PlayableRomPlugin => playableRom;

    public readonly string Name => name;

    public void Reset()
    {
        playableRom.ClearTemporaryMemory();
        retroPlugin.SetupGameTemporaryPatches(playableRom);
        playableRom.Reset(true);
    }
}


internal class Editor : IEditor
{
    private Dictionary<string, Type> romPlugins;
    private Dictionary<string, Type> plugins;
    private List<ActiveProject> activeProjects;   // Needs to become active projects, since we now allow multiple of the same plugin
    private WindowManager windowManager;

    internal class EditorSettings
    {
        public string ProjectLocation { get; set;}
        public string LastImportedLocation { get; set;}
        public string RetroCoreFolder { get; set;}
        public List<string> RecentProjects { get; set;}

        public EditorSettings()
        {
            ProjectLocation = Path.Combine(Directory.GetCurrentDirectory(), "Projects");
            RetroCoreFolder = Path.Combine(Directory.GetCurrentDirectory(), "Data");
            LastImportedLocation = "";
            RecentProjects = new List<string>();
        }
    }

    private EditorSettings settings;

    internal EditorSettings Settings => settings;

    internal IEnumerable<string> Plugins => plugins.Keys;

    private ActiveProject? currentActiveProject;
    public Editor(IRetroPlugin[] plugins, IRomPlugin[] romPlugins)
    {
        this.activeProjects = new List<ActiveProject>();
        this.romPlugins = new Dictionary<string, Type>();
        foreach (var plugin in romPlugins)
        {
            var type = plugin.GetType();
            var name = type.InvokeMember("Name", System.Reflection.BindingFlags.GetProperty, null, null, null) as string;
            if (name != null)
            {
                this.romPlugins.Add(name, type);
            }
        }
        this.plugins = new Dictionary<string, Type>();
        foreach (var plugin in plugins)
        {
            var type = plugin.GetType();
            var name = type.InvokeMember("Name", System.Reflection.BindingFlags.GetProperty, null, null, null) as string;
            if (name != null)
            {
                this.plugins.Add(name, type);
            }
        }
        windowManager = new WindowManager();

        settings = new EditorSettings();

        if (File.Exists("settings.json"))
        {
            var json = File.ReadAllText("settings.json");
            settings = JsonSerializer.Deserialize<EditorSettings>(json);
        }

        if (!Directory.Exists(settings.ProjectLocation))
        {
            Directory.CreateDirectory(settings.ProjectLocation);
        }

        if (!Directory.Exists(settings.RetroCoreFolder))
        {
            Directory.CreateDirectory(settings.RetroCoreFolder);
        }

        currentActiveProject=null;
    }


    public void RenderRun()
    {
        Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
        //Raylib.SetConfigFlags(ConfigFlags.FLAG_VSYNC_HINT);   // Don't wait for VSYNC, we do all synchronisation ourselves
        Raylib.InitWindow(800, 600, "Retro Editor");
        if (Raylib.IsWindowFullscreen())
        {
            Raylib.ToggleFullscreen();
        }
        if (!Raylib.IsWindowMaximized())
        {
            Raylib.MaximizeWindow();
        }
        rlImGui.Setup(darkTheme: true, enableDocking: true);

        Raylib.InitAudioDevice();

        var args = Environment.GetCommandLineArgs();

        foreach (var arg in args)
        {
            OpenProject(arg);
        }

        var deltaTime = 0.0f;
        while (!Raylib.WindowShouldClose())
        {
            if (Raylib.IsWindowResized())
            {
                rlImGui.Shutdown();
                rlImGui.Setup(darkTheme: true, enableDocking: true);
            }
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.BLANK);

            rlImGui.Begin();

            var quit= DrawUI(deltaTime);

            rlImGui.End();

            Raylib.EndDrawing();
            deltaTime = Raylib.GetFrameTime();

            if (quit)
            {
                Raylib.CloseWindow(); 
            }
        }

        windowManager.CloseAll();

        foreach (var active in activeProjects)
        {
            active.PlayableRomPlugin.Serialise(active.Settings);
            active.RetroPlugin.Close();
        }

        var json = JsonSerializer.Serialize<EditorSettings>(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText("settings.json", json);
    }

    private bool DrawUI(float deltaTime)
    {
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("Create New Project"))
                {
                    var window = new NewProjectDialog();
                    window.SetEditor(this);
                    window.Initialise();
                    windowManager.AddBlockingPopup(window, "Create New Project");
                }
                ImGui.Separator();
                if (ImGui.MenuItem("Open Existing Project"))
                {
                    var result = NativeFileDialogSharp.Dialog.FolderPicker();

                    if (result.IsOk)
                    {
                        OpenProject(result.Path);
                    }
                }
                if (settings.RecentProjects.Count == 0)
                {
                    ImGui.BeginDisabled();
                }
                if (ImGui.BeginMenu("Open Recent Project"))
                {
                    string toOpen = "";
                    foreach (var recent in settings.RecentProjects)
                    {
                        if (ImGui.MenuItem(recent))
                        {
                            toOpen=recent;
                            break;
                        }
                    }
                    if (toOpen != "")
                    {
                        OpenProject(toOpen);
                    }
                    ImGui.EndMenu();
                }
                if (settings.RecentProjects.Count == 0)
                {
                    ImGui.EndDisabled();
                }
                ImGui.Separator();
                if (activeProjects.Count == 0)
                {
                    ImGui.BeginDisabled();
                }
                if (ImGui.BeginMenu("Export Project"))
                {
                    foreach (var active in activeProjects)
                    {
                        if (ImGui.MenuItem(active.Name))
                        {
                            var result = NativeFileDialogSharp.Dialog.FileSave();

                            if (result.IsOk)
                            {
                                // Before export, we need to restore the original state, and only apply the serialised part
                                active.PlayableRomPlugin.Reset(false);

                                active.RetroPlugin.Export(active.PlayableRomPlugin).Save(result.Path);

                                active.PlayableRomPlugin.Reset(true);
                            }
                        }
                    }
                    ImGui.EndMenu();
                }
                if (activeProjects.Count == 0)
                {
                    ImGui.EndDisabled();
                }
                ImGui.Separator();
                if (ImGui.MenuItem("Exit"))
                {
                    return true;
                }
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Edit"))
            {
                ImGui.BeginDisabled();
                if (ImGui.MenuItem("Undo"))
                {
                }
                ImGui.EndDisabled();
                ImGui.EndMenu();
            }
#if DEBUG
            if (ImGui.BeginMenu("Developer"))
            {
                if (ImGui.BeginMenu("Plugin Player"))
                {
                    foreach (var plugin in romPlugins)
                    {
                        if (ImGui.MenuItem(plugin.Key))
                        {
                            var result = NativeFileDialogSharp.Dialog.FileOpen();
                            if (result.IsOk)
                            {
                                var instance = GetRomInstance(plugin.Key);
                                if (instance != null)
                                {
                                    var retro = GetLibRetroInstance(instance.LibRetroPluginName, null);
                                    if (retro != null)
                                    {
                                        //var game = new Fairlight();
                                        var pluginWindow = new LibRetroPlayerWindow(retro, null, null);
                                        var playableRom = new PlayableRom(this, retro, instance.Endian, instance.RequiresReload, instance.ChecksumCalculation);
                                        pluginWindow.Initialise();
                                        retro.LoadGame(result.Path);
                                        //retro.AutoLoad(playableRom, game.AutoLoadCondition);
                                        pluginWindow.OtherStuff();
                                        pluginWindow.InitWindow();
                                        windowManager.AddWindow(pluginWindow, plugin.Key);
                                    }
                                }
                            }

                        }
                    }

                    ImGui.EndMenu();
                }
                if (ImGui.MenuItem("Mame Remote"))
                {
                    var mame = new MameRemoteCommandWindow();
                    mame.Initialise();
                    windowManager.AddWindow(mame, "Mame Remote");
                }
                ImGui.EndMenu();
            }
#endif
            if (ImGui.BeginMenu("Window"))
            {
                if (activeProjects.Count==0)
                {
                    ImGui.BeginDisabled();
                    ImGui.MenuItem("No Projects Open");
                    ImGui.EndDisabled();
                }
                else
                {
                    foreach (var active in activeProjects)
                    {
                        if (ImGui.BeginMenu(active.Name))
                        {
                            currentActiveProject=active;
                            active.RetroPlugin.Menu(active.PlayableRomPlugin, this);
                            currentActiveProject=null;
                            bool playerOpen = windowManager.IsOpen($"LibRetro Player ({active.Name})");
                            if (playerOpen)
                            {
                                ImGui.BeginDisabled();
                            }
                            if (ImGui.MenuItem("Open Player"))
                            {
                                OpenPlayerWindow(active);
                            }
                            if (playerOpen)
                            {
                                ImGui.EndDisabled();
                            }

                            ImGui.EndMenu();
                        }
                    }
                }
                ImGui.EndMenu();
            }
            ImGui.EndMainMenuBar();
        }

        windowManager.Update(deltaTime);

        windowManager.Draw();

        return false;
    }


    internal IRomPlugin? GetRomInstance(string romKind)
    {
        if (romPlugins.TryGetValue(romKind, out var Type))
        {
            return Activator.CreateInstance(Type) as IRomPlugin;
        }
        return null;
    }

    internal IRetroPlugin? GetPluginInstance(string pluginName)
    {
        if (plugins.TryGetValue(pluginName, out var Type))
        {
            return Activator.CreateInstance(Type) as IRetroPlugin;
        }
        return null;
    }

    internal bool IsPluginSuitable(string pluginName, string fileName)
    {
        var tPlugin = GetPluginInstance(pluginName);
        if (tPlugin!=null)
        {
            return tPlugin.CanHandle(fileName);
        }
        return false;
    }

    private void OpenProject(string projectPath)
    {
        var editorPath = Path.Combine(projectPath, "Editor");
        // Bail if for some reason the editor directory is missing
        if (!Directory.Exists(editorPath))
        {
            return;
        }
        // Bail if the project is already open
        foreach (var activeProject in activeProjects)
        {
            if (activeProject.Settings.projectPath == projectPath)
            {
                return;
            }
        }

        // Load the project settings
        var projectName = projectPath.Split(Path.DirectorySeparatorChar).Last();
        var jsonPath = Path.Combine(editorPath, $"{projectName}.json");
        var projectSettings = new ProjectSettings(projectName, projectPath, "", "", "");
        projectSettings.Load(jsonPath);

        // Locate the plugin this project needs
        var plugin = GetPluginInstance(projectSettings.RetroPluginName);
        if (plugin!=null)
        {
            // Next we need to perform the initial import and save state, then we can begin editing (the player window will open)
            InternalInitialisePlugin(plugin, projectSettings, false);
        }
    }

    private void OpenPlayerWindow(ActiveProject activeProject)
    {
        var pluginWindow = new LibRetroPlayerWindow(activeProject.LibRetroPlugin, activeProject, activeProject.RetroPlugin.GetPlayerExtension());
        OpenWindow(pluginWindow, $"LibRetro Player ({activeProject.Name})");
        pluginWindow.InitWindow();
    }

    private void InternalAddDefaultWindowAndProject(ProjectSettings projectSettings, IRetroPlugin plugin, LibRetroPlugin retroPluginInstance, IRomPlugin romPlugin, PlayableRom playableRom)
    {
        var activeProjectName = projectSettings.projectName + $" [{activeProjects.Count + 1}]";
        var project = new ActiveProject(activeProjectName, plugin, retroPluginInstance, romPlugin, playableRom, projectSettings);

        OpenPlayerWindow(project);
        activeProjects.Add(project);
        if (!settings.RecentProjects.Contains(projectSettings.projectPath))
        {
            if (settings.RecentProjects.Count > 20)
            {
                settings.RecentProjects.RemoveAt(0);
            }
            settings.RecentProjects.Add(projectSettings.projectPath);
        }
    }

    internal bool CreateNewProject(string projectName, string projectLocation, string importFile, string retroPluginName)
    {
        // Todo Progress Dialog
        var projectPath = Path.Combine(projectLocation, projectName);
        Directory.CreateDirectory(projectPath);
        Directory.CreateDirectory(Path.Combine(projectPath, "LibRetro"));
        Directory.CreateDirectory(Path.Combine(projectPath, "Editor"));
        var projectFile = Path.Combine(projectPath, "Editor", projectName + ".json");

        // Generate hash for the plugin
        var hashA = MD5.Create().ComputeHash(File.ReadAllBytes(importFile));
        var hashB = MD5.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(projectName));
        var hashC = MD5.Create().ComputeHash(BitConverter.GetBytes(DateTime.Now.ToBinary()));
        var hash = MD5.Create().ComputeHash(hashA.Concat(hashB).Concat(hashC).ToArray());
        var retroCoreName = string.Concat(hash.Select(x => x.ToString("X2")));

        var projectSettings = new ProjectSettings(projectName, projectPath, retroCoreName, retroPluginName, Path.GetFileName(importFile));
        projectSettings.Save(projectFile);
        File.Copy(importFile, GetRomPath(projectSettings), true);

        var retroPlugin = GetPluginInstance(retroPluginName);
        if (retroPlugin==null)
        {
            return false;
        }
        return InternalInitialisePlugin(retroPlugin, projectSettings, true);
    }

    private bool InternalInitialisePlugin(IRetroPlugin plugin, ProjectSettings projectSettings, bool firstTime)
    {
        // Initialise Rom
        var romInterface = GetRomInstance(plugin.RomPluginName);
        if (romInterface==null)
        {
            return false;
        }
        var emuPlugin = GetLibRetroInstance(romInterface.LibRetroPluginName, projectSettings); 
        if (emuPlugin == null)
        {
            return false;
        }
        if (emuPlugin.Version() != 1)
        {
            return false;
        }
        emuPlugin.Init();

        var playableRom = new PlayableRom(this, emuPlugin, romInterface.Endian, romInterface.RequiresReload, romInterface.ChecksumCalculation);

        if (firstTime)
        {
            playableRom.Setup(projectSettings, GetRomPath(projectSettings), plugin.RequiresAutoLoad ? plugin.AutoLoadCondition : null);
        }
        else
        {
            playableRom.Reload(projectSettings);
        }

        plugin.SetupGameTemporaryPatches(playableRom);

        playableRom.Reset(true);
        
        InternalAddDefaultWindowAndProject(projectSettings, plugin, emuPlugin, romInterface, playableRom);
        return true;
    }

    public void SaveState(byte[] state, ProjectSettings projectSettings)
    {
        var stateFile = Path.Combine(projectSettings.projectPath, "Editor", $"{projectSettings.RetroCoreName}_state.bin");
        File.WriteAllBytes(stateFile, state);
    }

    public byte[] LoadState(ProjectSettings projectSettings)
    {
        var stateFile = Path.Combine(projectSettings.projectPath, "Editor", $"{projectSettings.RetroCoreName}_state.bin");
        if (!File.Exists(stateFile))
        {
            return Array.Empty<byte>();
        }
        return File.ReadAllBytes(stateFile);
    }

    public string GetRomPath(ProjectSettings projectSettings)
    {
        return Path.Combine(projectSettings.projectPath, "Editor", $"{projectSettings.RetroCoreName}_rom.bin");
    }

    public string GetEditorDataPath(ProjectSettings projectSettings, string serialiseName)
    {
        return Path.Combine(projectSettings.projectPath, "Editor", $"{projectSettings.RetroCoreName}_{serialiseName}.json");
    }

    public LibRetroPlugin? GetLibRetroInstance(string pluginName, ProjectSettings? projectSettings)
    {
        var OS=RuntimeInformation.OSDescription;
        var platform = "";
        var extension = "";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            platform = "windows";
            extension = ".dll";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            platform = "linux";
            extension = ".so";
        }
        else
        {
            Console.WriteLine($"Unsupported OS: {OS}");
            return null;
        }
        var architecture = "";
        switch (RuntimeInformation.OSArchitecture)
        {
            case Architecture.X64:
                architecture = "x86_64";
                break;
            default:
                Console.WriteLine($"Unsupported Architecture: {RuntimeInformation.OSArchitecture}");
                return null;
        }


        var sourcePlugin = Path.Combine(settings.RetroCoreFolder, platform, architecture, $"{pluginName}{extension}");
        string? destinationPlugin;
        if (projectSettings == null)
        {
            // Developer mode, we don't have a project folder
            destinationPlugin = sourcePlugin;
        }
        else
        {
            destinationPlugin = Path.Combine(projectSettings.projectPath, "LibRetro", $"{projectSettings.RetroCoreName}_{platform}_{architecture}{extension}");
        }
        if (!File.Exists(destinationPlugin))
        {
            if (!File.Exists(sourcePlugin))
            {
                var task = Download(platform, architecture, extension, pluginName);
                task.Wait();
                if (task.Result!=true)
                {
                    return null;
                }
            }

            if (destinationPlugin != sourcePlugin)
            {
                File.Copy(sourcePlugin, destinationPlugin, true);
            }
        }

        try 
        {
            return new LibRetroPlugin(destinationPlugin);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to load plugin {pluginName} from {destinationPlugin}: {e.Message}");
            return null;
        }
    }

    async Task<bool> Download(string platform, string architecture, string extension, string pluginName)
    {
        var url = $"http://buildbot.libretro.com/nightly/{platform}/{architecture}/latest/{pluginName}{extension}.zip";
        var destination = Path.Combine(settings.RetroCoreFolder, platform, architecture, $"{pluginName}{extension}");

        using (var client = new HttpClient())
        {
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);
                        ZipArchive archive = new ZipArchive(memoryStream);
                        foreach (var entry in archive.Entries)
                        {
                            if (entry.Name == $"{pluginName}{extension}")
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(destination));  // Ensure destination folder exists
                                using (var fileStream = File.Create(destination))
                                {
                                    entry.Open().CopyTo(fileStream);
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
        }
        return false;
    }

    public void OpenWindow(IWindow window, string name)
    {
        if (windowManager.IsOpen(name))
        {
            return;
        }
        window.Initialise();

        if (currentActiveProject != null)
        {
            windowManager.AddWindow(window, $"{name} ({currentActiveProject?.Name})");
        }
        else
        {
            windowManager.AddWindow(window, name);
        }
    }

    public void CloseWindow(string name)
    {
        windowManager.Close(name);
    }

}
