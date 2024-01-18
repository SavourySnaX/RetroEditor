using Raylib_cs;
using rlImGui_cs;
using ImGuiNET;
using System.Security.Cryptography;
using System.Text.Json;
using System.Runtime.InteropServices;

internal class Editor : IEditor
{
    private Dictionary<string, Type> romPlugins;
    private IRetroPlugin[] plugins;
    private List<ActiveProject> activeProjects;   // Needs to become active projects, since we now allow multiple of the same plugin

    private List<IWindow> activeWindows;

    internal struct ActiveProject
    {
        public IRetroPlugin Plugin;
        public ProjectSettings Settings;

        public string Name { get; internal set; }
    }

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

    internal IEnumerable<IRetroPlugin> Plugins => plugins;

    public Editor(IRetroPlugin[] plugins, IRomPlugin[] romPlugins)
    {
        this.plugins = plugins;

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
        activeWindows = new List<IWindow>();

        settings = new EditorSettings();

        if (File.Exists("settings.json"))
        {
            var json = File.ReadAllText("settings.json");
            settings = JsonSerializer.Deserialize<EditorSettings>(json);
        }
    }


    public void RenderRun()
    {
        Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
        Raylib.SetConfigFlags(ConfigFlags.FLAG_VSYNC_HINT);
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

        // Main application loop

        var args = Environment.GetCommandLineArgs();

        foreach (var arg in args)
        {
            OpenProject(arg);
        }

        totalTime=0.0f;
        
        // Testing
        /*
        var pluginWindow = new JSWTest(LibRetroPluginFactory.Create("fuse_libretro","C:\\work\\editor\\RetroEditor\\data\\1.dll"), "Flibble");
        pluginWindow.Initialise();
        pluginWindow.OtherStuff();
        pluginWindow.InitWindow();
        AddWindow(pluginWindow);*/
        /*
        pluginWindow = new JSWTest(LibRetroPluginFactory.Create("C:\\zidoo_flash\\retroarch\\cores\\fceumm_libretro.dll"), "FCEU");
        //var pluginWindow = new JSWTest(LibRetroPluginFactory.Create("C:\\work\\editor\\nes\\libretro-fceumm\\fceumm_libretro.dll"), "FCEU");
        pluginWindow.Initialise();
        pluginWindow.OtherStuff();
        AddWindow(pluginWindow);
        pluginWindow = new JSWTest(LibRetroPluginFactory.Create("C:\\zidoo_flash\\retroarch\\cores\\genesis_plus_gx_libretro.dll"), "Genesis");
        pluginWindow.Initialise();
        pluginWindow.OtherStuff();
        AddWindow(pluginWindow);
*/
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

            DrawUI();

            rlImGui.End();

            Raylib.EndDrawing();
            var deltaTime = Raylib.GetFrameTime();
            totalTime += deltaTime;
        }

        foreach (var mWindow in activeWindows)
        {
            mWindow.Close();
        }

        activeWindows.Clear();
        foreach (var active in activeProjects)
        {
            active.Plugin.Close();
        }

        var json = JsonSerializer.Serialize<EditorSettings>(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText("settings.json", json);
    }

    private void AddWindow(IWindow window)
    {
        activeWindows.Add(window);
        var newTime = totalTime + window.UpdateInterval;
        priorityQueue.Enqueue((window, newTime),newTime);
    }

    private void DrawUI()
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
                    AddWindow(window);
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
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Edit"))
            {
                if (ImGui.MenuItem("Undo"))
                {
                }
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Remote"))
            {
                if (ImGui.MenuItem("Mame Remote"))
                {
                    var mame = new MameRemoteCommandWindow();
                    mame.Initialise();
                    AddWindow(mame);
                }
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Window"))
            {
                foreach (var active in activeProjects)
                {
                    if (ImGui.BeginMenu(active.Name))
                    {
                        var imageInterface = active.Plugin.GetImageInterface();
                        if (imageInterface != null)
                        {
                            if (ImGui.BeginMenu("Image Viewer"))
                            {
                                for (int a = 0; a < imageInterface.GetImageCount(); a++)
                                {
                                    var map = imageInterface.GetImage(a);
                                    var mapName = map.Name;
                                    if (ImGui.MenuItem(mapName))
                                    {
                                        var window = new ImageWindow(active.Plugin, map);
                                        window.Initialise();
                                        AddWindow(window);
                                    }
                                }
                                ImGui.EndMenu();
                            }
                        }
                        var tileMapInterface = active.Plugin.GetTileMapInterface();
                        if (tileMapInterface != null)
                        {
                            if (ImGui.BeginMenu("Tile Map Editor"))
                            {
                                for (int a = 0; a < tileMapInterface.GetMapCount(); a++)
                                {
                                    var map = tileMapInterface.GetMap(a);
                                    var mapName = map.Name;
                                    if (ImGui.MenuItem(mapName))
                                    {
                                        var window = new TileMapEditorWindow(active.Plugin, map);
                                        window.Initialise();
                                        AddWindow(window);
                                    }
                                }
                                ImGui.EndMenu();
                            }
                        }
                        ImGui.EndMenu();
                    }
                }
                ImGui.EndMenu();
            }
            ImGui.EndMainMenuBar();
        }

        if (priorityQueue.Count > 0)
        {
            while (priorityQueue.Peek().Item2 <= totalTime)
            {
                var lastTick = priorityQueue.Dequeue();
                var diff = Math.Min(lastTick.Item2, totalTime - lastTick.Item2);
                lastTick.Item1.Update(totalTime);
                var newTime = totalTime + lastTick.Item1.UpdateInterval - diff;
                priorityQueue.Enqueue((lastTick.Item1, newTime),newTime);
            }
        }

        foreach (var window in activeWindows)
        {
            if (!window.Draw())
            {
                window.Close();
                activeWindows.Remove(window);
                break;
            }
        }
    }

    private PriorityQueue<(IWindow, float), float> priorityQueue = new PriorityQueue<(IWindow, float), float>();
    private float totalTime;

    public IRomPlugin? GetRomInstance(string romKind)
    {
        if (romPlugins.TryGetValue(romKind, out var Type))
        {
            return Activator.CreateInstance(Type) as IRomPlugin;
        }
        return null;
    }

    private void OpenProject(string projectPath)
    {
        // Todo Progress Dialog
        var editorPath = Path.Combine(projectPath, "Editor");
        if (!Directory.Exists(editorPath))
        {
            return;
        }
        foreach (var activeProject in activeProjects)
        {
            if (activeProject.Settings.projectPath == projectPath)
            {
                return;
            }
        }

        var projectName = projectPath.Split(Path.DirectorySeparatorChar).Last();
        var jsonPath = Path.Combine(editorPath, $"{projectName}.json");
        var projectSettings = new ProjectSettings(projectPath,"","");
        projectSettings.Load(jsonPath);

        // Get Project File Settings
        foreach (var plugin in plugins)
        {
            if (plugin.Name==projectSettings.RetroPluginName)
            {
                // Next we need to perform the initial import and save state, then we can begin editing (the player window will open)
                if (!plugin.Open(this, projectSettings, out var retroPluginInstance))
                {
                    return;
                }
                if (retroPluginInstance == null)
                {
                    return;
                }
                var pluginWindow = new JSWTest(retroPluginInstance, projectName);
                pluginWindow.InitWindow();
                AddWindow(pluginWindow);
                activeProjects.Add(new ActiveProject { Plugin = plugin, Settings = projectSettings, Name = projectName });
                if (!settings.RecentProjects.Contains(projectPath))
                {
                    if (settings.RecentProjects.Count>20)
                    {
                        settings.RecentProjects.RemoveAt(0);
                    }
                    settings.RecentProjects.Add(projectPath);
                }
            }
        }
    }


    internal bool CreateNewProject(string projectName, string projectLocation, string importFile, IRetroPlugin retroPlugin)
    {
        // Todo Progress Dialog
        var projectPath = Path.Combine(projectLocation, projectName);
        Directory.CreateDirectory(projectPath);
        Directory.CreateDirectory(Path.Combine(projectPath, "LibRetro"));
        Directory.CreateDirectory(Path.Combine(projectPath, "Editor"));
        var projectFile = Path.Combine(projectPath, "Editor", projectName + ".json");

        // Generate hash for the plugin
        var hash = MD5.Create().ComputeHash(File.ReadAllBytes(importFile));
        var retroCoreName = string.Concat(hash.Select(x => x.ToString("X2")));
        var retroPluginName = retroPlugin.Name;

        var projectSettings = new ProjectSettings(projectPath, retroCoreName, retroPluginName);
        projectSettings.Save(projectFile);
        File.Copy(importFile, GetRomPath(projectSettings), true);

        // Next we need to perform the initial import and save state, then we can begin editing (the player window will open)
        if (!retroPlugin.Init(this, projectSettings, out var retroPluginInstance))
        {
            return false;
        }
        if (retroPluginInstance == null)
        {
            return false;
        }
        var pluginWindow = new JSWTest(retroPluginInstance, projectName);
        pluginWindow.InitWindow();
        AddWindow(pluginWindow);
        activeProjects.Add(new ActiveProject { Plugin = retroPlugin, Settings = projectSettings, Name = projectName });
        if (!settings.RecentProjects.Contains(projectPath))
        {
            if (settings.RecentProjects.Count > 20)
            {
                settings.RecentProjects.RemoveAt(0);
            }
            settings.RecentProjects.Add(projectPath);
        }
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

    public LibRetroPlugin? GetLibRetroInstance(string pluginName, ProjectSettings projectSettings)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return null;
        }
        if (!RuntimeInformation.OSArchitecture.Equals(Architecture.X64))
        {
            return null;
        }

        var sourcePlugin = Path.Combine(settings.RetroCoreFolder, "win", "x64", $"{pluginName}.dll");
        var destinationPlugin = Path.Combine(projectSettings.projectPath, "LibRetro", $"{projectSettings.RetroCoreName}_win_x64.dll");

        if (!File.Exists(destinationPlugin))
        {
            if (!File.Exists(sourcePlugin))
            {
                // TODO DOWNLOAD
            }

            File.Copy(sourcePlugin, destinationPlugin, true);
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

}
