using Raylib_cs;
using rlImGui_cs;
using ImGuiNET;
using System.Security.Cryptography;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.IO.Compression;

internal class Editor : IEditor
{
    private Dictionary<string, Type> romPlugins;
    private IRetroPlugin[] plugins;
    private List<ActiveProject> activeProjects;   // Needs to become active projects, since we now allow multiple of the same plugin
    private WindowManager windowManager;

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

            DrawUI(deltaTime);

            rlImGui.End();

            Raylib.EndDrawing();
            deltaTime = Raylib.GetFrameTime();
        }

        windowManager.CloseAll();

        foreach (var active in activeProjects)
        {
            active.Plugin.Close();
        }

        var json = JsonSerializer.Serialize<EditorSettings>(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText("settings.json", json);
    }

    private void DrawUI(float deltaTime)
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
                    windowManager.AddWindow(mame, "Mame Remote");
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
                                        windowManager.AddWindow(window, $"Image ({active.Name})");
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
                                        windowManager.AddWindow(window, $"Tile ({active.Name})");
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

        windowManager.Update(deltaTime);

        windowManager.Draw();
    }


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
                var activeProjectName = projectName + $" [{activeProjects.Count+1}]";
                var pluginWindow = new LibRetroPlayerWindow(retroPluginInstance, activeProjectName);
                pluginWindow.InitWindow();
                windowManager.AddWindow(pluginWindow, $"LibRetro Player ({activeProjectName})");
                activeProjects.Add(new ActiveProject { Plugin = plugin, Settings = projectSettings, Name = activeProjectName });
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
        var hashA = MD5.Create().ComputeHash(File.ReadAllBytes(importFile));
        var hashB = MD5.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(projectName));
        var hashC = MD5.Create().ComputeHash(BitConverter.GetBytes(DateTime.Now.ToBinary()));
        var hash = MD5.Create().ComputeHash(hashA.Concat(hashB).Concat(hashC).ToArray());
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
        var activeProjectName = projectName + $" [{activeProjects.Count + 1}]";
        var pluginWindow = new LibRetroPlayerWindow(retroPluginInstance, activeProjectName);
        pluginWindow.InitWindow();
        windowManager.AddWindow(pluginWindow, $"LibRetro Player ({activeProjectName})");
        activeProjects.Add(new ActiveProject { Plugin = retroPlugin, Settings = projectSettings, Name = activeProjectName });
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
        var platform = "windows";
        var architecture = "x86_64";
        var extension = ".dll";

        var sourcePlugin = Path.Combine(settings.RetroCoreFolder, platform, architecture, $"{pluginName}{extension}");
        var destinationPlugin = Path.Combine(projectSettings.projectPath, "LibRetro", $"{projectSettings.RetroCoreName}_{platform}_{architecture}{extension}");

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

    async Task<bool> Download(string platform, string architecture, string extension, string pluginName)
    {
        var url = $"http://buildbot.libretro.com/nightly/{platform}/{architecture}/latest/{pluginName}{extension}.zip";
        var destination = Path.Combine(settings.RetroCoreFolder, platform, architecture, $"{pluginName}.dll");

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

}
