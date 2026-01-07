using Raylib_cs;
using Raylib_cs.BleedingEdge;
using rlImGui_cs;
using ImGuiNET;
using System.Security.Cryptography;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.IO.Compression;
using RetroEditor.Plugins;
using RetroEditor.Source.Internals.GUI;

internal class MenuData : IMenuItem
{
    bool _enabled;
    string _name;
    MenuEventHandler? _handler;
    List<IMenuItem> _children;

    public MenuData(string name, MenuEventHandler? handler)
    {
        _name = name;
        _handler = handler;
        _enabled = true;
        _children = new List<IMenuItem>();
    }

    public bool Enabled { get => _enabled; set => _enabled = value; }
    public string Name { get => _name; set => _name = value; }
    public MenuEventHandler? Handler { get => _handler; set => _handler = value; }

    public List<IMenuItem> Children => _children;

}

internal class UIData
{
    public UIData()
    {
        Menus = new List<MenuData>();
    }

    public List<MenuData> Menus;
}

internal struct ActiveProject : IPlayerControls, IMenu
{
    public ActiveProject(string name, PlayableRom playableRom, ProjectSettings settings, UIData ui)
    {
        this.name = name;
        this.retroPlugin = playableRom.retroPlugin;
        this.libRetroPlugin = playableRom.systemPlugin;
        this.romPlugin = playableRom.romInterface;
        this.playableRom = playableRom;
        this.settings = settings;
        this.ui = ui;
    }

    private string name;
    private IRetroPlugin retroPlugin;
    private LibRetroPlugin libRetroPlugin;
    private ISystemPlugin romPlugin;
    private PlayableRom playableRom;
    private ProjectSettings settings;
    private UIData ui;

    public readonly IRetroPlugin RetroPlugin => retroPlugin;
    public readonly LibRetroPlugin LibRetroPlugin => libRetroPlugin;
    public readonly ISystemPlugin RomPlugin => romPlugin;
    public readonly ProjectSettings Settings => settings;
    public readonly PlayableRom PlayableRomPlugin => playableRom;
    public readonly UIData UI => ui;

    public readonly string Name => name;

    public void Reset()
    {
        playableRom.ClearTemporaryMemory();
        retroPlugin.SetupGameTemporaryPatches(playableRom);
        playableRom.Reset(true);
    }

    internal void UnloadRetroPlugin()
    {
        ui.Menus.Clear();
        PlayableRomPlugin.Close();
        libRetroPlugin.Dispose();
    }

    public readonly IMenuItem AddItem(string name)
    {
        var item = new MenuData(name, null);
        ui.Menus.Add(item);
        return item;
    }

    public IMenuItem AddItem(string name, MenuEventHandler handler)
    {
        var item = new MenuData(name, handler);
        ui.Menus.Add(item);
        return item;
    }

    public IMenuItem AddItem(IMenuItem parent, string name)
    {
        var item = new MenuData(name, null);
        ((MenuData)parent).Children.Add(item);
        return item;
    }

    public IMenuItem AddItem(IMenuItem parent, string name, MenuEventHandler handler)
    {
        var item = new MenuData(name, handler);
        ((MenuData)parent).Children.Add(item);
        return item;
    }
}


internal class Editor : IEditor, IEditorInternal
{
    private Dictionary<string, Type> romPlugins;
    private Dictionary<string, Type> plugins;
    private List<ActiveProject> activeProjects;   // Needs to become active projects, since we now allow multiple of the same plugin
    internal WindowManager windowManager;

    internal class EditorSettings
    {
        public const int MajorVersion = 0;
        public const int MinorVersion = 1;
        public const int PatchVersion = 0;
        public string ProjectLocation { get; set;}
        public string LastImportedLocation { get; set;}
        public string RetroCoreFolder { get; set;}
        public string MameDebuggerDataFolder { get; set; }
        public string TempFolder { get; set; }
        public string LogFolder { get; set; }
        public List<string> RecentProjects { get; set;}
        public string Version { get; set; }
        public bool DeveloperMode { get; set; }
        public bool EnableMSAA { get; set; }
        public bool EnableHighDPI { get; set; }

        public static string CurrentVersion => $"{MajorVersion}.{MinorVersion}.{PatchVersion}";

        public EditorSettings()
        {
            Version = $"{MajorVersion}.{MinorVersion}.{PatchVersion}";
            ProjectLocation = Path.Combine(Directory.GetCurrentDirectory(), "Projects");
            RetroCoreFolder = Path.Combine(Directory.GetCurrentDirectory(), "Data");
            LogFolder = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            MameDebuggerDataFolder = Path.Combine(Directory.GetCurrentDirectory(), "Mame");
            TempFolder = Path.Combine(RetroCoreFolder, "Temp");
            LastImportedLocation = "";
            RecentProjects = new List<string>();
            DeveloperMode = false;
            EnableHighDPI = false;
            EnableMSAA = false;
        }
    }

    private EditorSettings settings;

    public EditorSettings Settings => settings;

    internal IEnumerable<string> Plugins => plugins.Keys;

    internal LibMameDebugger? mameInstance;

    private ActiveProject? currentActiveProject;

    private Dictionary<Type, IGamePluginLoader> pluginToLoader = new Dictionary<Type, IGamePluginLoader>();

    private ILogger _log;


    internal Editor()
    {
        this.activeProjects = new List<ActiveProject>();

        windowManager = new WindowManager(this);

        if (settings == null)
        {
            settings = new EditorSettings();

            if (File.Exists("settings.json"))
            {
                var json = File.ReadAllText("settings.json");
                var tempSettings = JsonSerializer.Deserialize<EditorSettings>(json);
                if (tempSettings != null)
                {
                    settings = tempSettings;
                }
            }
        }

        if (!Directory.Exists(settings.ProjectLocation))
        {
            Directory.CreateDirectory(settings.ProjectLocation);
        }

        if (!Directory.Exists(settings.RetroCoreFolder))
        {
            Directory.CreateDirectory(settings.RetroCoreFolder);
        }

        if (!Directory.Exists(settings.LogFolder))
        {
            Directory.CreateDirectory(settings.LogFolder);
        }

        if (!Directory.Exists(settings.MameDebuggerDataFolder))
        {
            Directory.CreateDirectory(settings.MameDebuggerDataFolder);
        }

        // Clear out temp folder on startup
        if (Directory.Exists(settings.TempFolder))
        {
            Directory.Delete(settings.TempFolder, true);
        }
        Directory.CreateDirectory(settings.TempFolder);

        if (_log == null)
        {
            _log = new Log(Path.Combine(settings.LogFolder, "editor.log"));
        }

        this.romPlugins = new Dictionary<string, Type>();

        this.plugins = new Dictionary<string, Type>();
        currentActiveProject=null;
        mameInstance = null;
    }

    internal Editor(EditorSettings editorSettings, ILogger logger) : this()
    {
        settings = editorSettings;
        _log = logger;
    }

    internal void InitialisePlugins(IEnumerable<IGamePluginLoader> plugins, Type[] romPlugins)
    {
        foreach (var plugin in romPlugins)
        {
            var type = plugin;
            var name = type.InvokeMember("Name", System.Reflection.BindingFlags.GetProperty, null, null, null) as string;
            if (name != null)
            {
                this.romPlugins.Add(name, type);
            }
        }

        foreach (var pluginLoader in plugins)
        {
            InitialisePlugin(pluginLoader);
        }

    }

    private void InitialisePlugin(IGamePluginLoader pluginLoader)
    {
        var pluginTypes = pluginLoader.LoadPlugin();
        if (pluginTypes == null)
        {
            return;
        }
        foreach (var t in pluginTypes)
        {
            pluginToLoader.Add(t, pluginLoader);
            InitialiseIRetroType(t);
        }
    }

    private void ClosePlugin(IGamePluginLoader pluginLoader)
    {
        pluginLoader.UnloadPlugin();
    }

    public Log AccessLog => _log as Log ?? throw new Exception("AccessLog is intended for logview only!");

    public void Log(LogType type, string logSource, string message)
    {
        _log.Add(type, logSource, message);
    }

    private void InitialiseIRetroType(Type plugin)
    {
        var type = plugin;
        var name = type.InvokeMember("Name", System.Reflection.BindingFlags.GetProperty, null, null, null) as string;
        if (name != null)
        {
            this.plugins.Add(name, type);
        }
    }

    private void RemoveIRetroType(string name)
    {
        this.plugins.Remove(name);
    }

    private unsafe delegate* unmanaged[Cdecl]<void*, int, sbyte*, sbyte*, void> RayLibLogInstanceDelegate;
    private unsafe delegate* unmanaged[Cdecl]<TraceLogLevel, byte*, nint, void> LogWithInstance;

    internal void RenderRun()
    {
        var pin = GCHandle.Alloc(this);
        unsafe
        {
            // Wrap the log function so we can pass the instance of editor to it
            RayLibLogInstanceDelegate = &RayLibLoggingWrapper.Log;
            LogWithInstance = (delegate* unmanaged[Cdecl]<TraceLogLevel, byte*, nint, void>)InstanceTrampoline.InterfaceTrampoline.AllocateTrampoline(GCHandle.ToIntPtr(pin), 3, (nint)RayLibLogInstanceDelegate);
            Raylib.SetTraceLogCallback(LogWithInstance);
        }

        var config = ConfigFlags.WindowResizable;
        if (settings.EnableHighDPI)
        {
            config|=ConfigFlags.WindowHighDpi;
        }
        if (settings.EnableMSAA)
        {
            config|=ConfigFlags.Msaa4XHint;
        }

        Raylib.SetConfigFlags(config);
        //Raylib.SetConfigFlags(ConfigFlags.FLAG_VSYNC_HINT);   // Don't wait for VSYNC, we do all synchronisation ourselves
        Raylib.InitWindow(800, 600, $"Retro Editor - レトロゲームの変更の道具 - Version {EditorSettings.CurrentVersion}");
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

        Raylib.SetExitKey(KeyboardKey.Null); // Disable exit key to allow proper shutdown

        var args = Environment.GetCommandLineArgs().Skip(1);

        // SPEED STUFF UP FOR NOW
/*
        var retro = GetDeveloperMame();
        if (retro != null)
        {
            mameInstance = new LibMameDebugger(retro);

            var pluginWindow = new LibRetroDebuggerWindow(retro,mameInstance);
            pluginWindow.Initialise();
            retro.LoadGame(Path.Combine(Directory.GetCurrentDirectory(), "..", "snes", "mariow.bin"));
            pluginWindow.InitWindow();
            windowManager.AddWindow(pluginWindow, "MAME RETRO", null);
            while (!mameInstance.DebuggerViewReady)
            {
                Thread.Sleep(100);
            }
            OpenWindow(new Resourcer(mameInstance), "Resourcer");
            OpenWindow(new DebuggerView(mameInstance, LibRetroPlugin.debug_view_type.State, 20, 25, ""), $"CPU State 0");
            OpenWindow(new DebuggerView(mameInstance, LibRetroPlugin.debug_view_type.Disassembly, 100, 25, "curpc"), $"Disassembly 0");
            OpenWindow(new DebuggerView(mameInstance, LibRetroPlugin.debug_view_type.Memory, 80, 25, "0"), $"Memory 0");
        }
*/
        //


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
            Raylib.ClearBackground(Color.Blank);

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
        }

        var json = JsonSerializer.Serialize<EditorSettings>(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText("settings.json", json);

        unsafe
        {
            Raylib.SetTraceLogCallback(null);
        }
        pin.Free();
    }

    private bool DrawUI(float deltaTime)
    {
        bool shouldClose = false;
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("Create New Project"))
                {
                    var window = new NewProjectDialog(this);
                    window.Initialise();
                    windowManager.AddWindow(window, "Create New Project", null);
                }
                ImGui.Separator();
                if (ImGui.MenuItem("Open Existing Project"))
                {
                    OpenFileDialog(FileDialog.FileDialogType.FolderPicker, "Select Project Folder", settings.ProjectLocation, new string[] { "*.*" }, (string path) =>
                    {
                        if (path != null)
                        {
                            OpenProject(path);
                        }
                        return true;
                    });
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
                            OpenFileDialog(FileDialog.FileDialogType.Save, "Export File", "", new string[] { "*.*" }, (string path) =>
                            {
                                if (path != null)
                                {
                                    // Before export, we need to restore the original state, and only apply the serialised part
                                    active.PlayableRomPlugin.Reset(false);

                                    ISave? save = null;
                                    try
                                    {
                                        save = active.RetroPlugin.Export(active.PlayableRomPlugin);
                                    }
                                    catch (Exception e)
                                    {
                                        Log(LogType.Error, $"Exporting {active.Name}", e.Message);
                                    }
                                    if (save != null)
                                    {
                                        save.Save(path);
                                    }

                                    active.PlayableRomPlugin.Reset(true);
                                }
                                return true;
                            });
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
                    shouldClose = true;
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
            if (settings.DeveloperMode)
            {
                if (ImGui.BeginMenu("Developer"))
                {
                    if (ImGui.BeginMenu("Plugin Player"))
                    {
                        foreach (var plugin in romPlugins)
                        {
                            if (ImGui.MenuItem(plugin.Key))
                            {
                                OpenFileDialog(FileDialog.FileDialogType.Open, "Select ROM to Load", "", new string[] { "*.*" }, (string path) =>
                                {
                                    if (path != null)
                                    {
                                        var instance = GetRomInstance(plugin.Key);
                                        if (instance != null)
                                        {
                                            var retro = GetLibRetroInstance(instance.LibRetroPluginName, null, out _);
                                            if (retro != null)
                                            {
                                                var pluginWindow = new LibRetroPlayerWindow(retro);
                                                var playableRom = new PlayableRom(this, retro, instance, new DummyRetroPlugin());
                                                pluginWindow.Initialise();
                                                retro.LoadGame(path);
                                                pluginWindow.OtherStuff();
                                                pluginWindow.InitWindow();
                                                windowManager.AddWindow(pluginWindow, plugin.Key, null);
                                            }
                                        }
                                    }
                                    return true;
                                });
                            }
                        }

                        ImGui.EndMenu();
                    }
                    if (ImGui.BeginMenu("LibMame Debugger"))
                    {
                        var initialMameInstance = mameInstance;
                        if (initialMameInstance != null)
                        {
                            ImGui.BeginDisabled();
                        }
                        if (ImGui.MenuItem("Launch"))
                        {
                            // We use Mame for debugging mostly because it means I only needed to modify one of the lib retro plugins
                            OpenFileDialog(FileDialog.FileDialogType.Open, "Select ROM to Load", "", new string[] { "*.*" }, (string path) =>
                            {
                                if (path != null)
                                {
                                    var retro = GetDeveloperMame();
                                    if (retro != null)
                                    {
                                        mameInstance = new LibMameDebugger(retro);

                                        var pluginWindow = new LibRetroDebuggerWindow(retro,mameInstance);
                                        pluginWindow.Initialise();
                                        retro.LoadGame(path);
                                        pluginWindow.InitWindow();
                                        windowManager.AddWindow(pluginWindow, "MAME RETRO", null);

                                    }
                                }
                                return true;
                            });
                        }
                        if (initialMameInstance != null)
                        {
                            ImGui.EndDisabled();
                        }
                        if (mameInstance == null)
                        {
                            ImGui.BeginDisabled();
                        }
                        mameInstance?.Menus(this);
                        if (ImGui.MenuItem("Open Player Window"))
                        {
                            if (!windowManager.IsOpen("MAME RETRO"))
                            {
                                // TODO
                            }
                        }
                        if (mameInstance == null)
                        {
                            ImGui.EndDisabled();
                        }
                        ImGui.EndMenu();
                    }
                    ImGui.EndMenu();
                }
            }
            if (ImGui.BeginMenu("Window"))
            {
                bool logOpen = windowManager.IsOpen("Log");
                if (logOpen)
                {
                    ImGui.BeginDisabled();
                }
                if (ImGui.MenuItem("Show Log"))
                {
                    var logWindow = new LogView(this);
                    logWindow.Initialise();
                    OpenWindow(logWindow, "Log");
                }
                if (logOpen)
                {
                    ImGui.EndDisabled();
                }

                if (activeProjects.Count==0)
                {
                    ImGui.BeginDisabled();
                    ImGui.MenuItem("No Projects Open");
                    ImGui.EndDisabled();
                }
                else
                {
                    ActiveProject? toClose = null;
                    foreach (var active in activeProjects)
                    {
                        if (ImGui.BeginMenu(active.Name))
                        {
                            currentActiveProject=active;
                            foreach (var menu in active.UI.Menus)
                            {
                                RenderMenu(menu);
                            }


                            //active.RetroPlugin.Menu(active.PlayableRomPlugin, this);
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
                            if (ImGui.MenuItem("Reload Plugin"))
                            {
                                toClose = active;
                            }

                            currentActiveProject=null;
                            ImGui.EndMenu();
                        }
                    }
                    if (toClose != null)
                    {
                        var projectLocation = toClose.Value.Settings.projectPath;
                        var pluginName = toClose.Value.Settings.RetroPluginName;
                        var loader = pluginToLoader[toClose.Value.RetroPlugin.GetType()];
                        CloseProject(toClose.Value);
                        toClose = null;

                        RemoveIRetroType(pluginName);
                        ClosePlugin(loader);

                        InitialisePlugin(loader);

                        // Reload the project
                        OpenProject(projectLocation);
                    }
                }
                ImGui.EndMenu();
            }
            ImGui.EndMainMenuBar();
        }

        windowManager.Update(deltaTime);

        windowManager.Draw();

        return shouldClose;
    }

    private void RenderMenu(MenuData menu)
    {
        var disabled = menu.Enabled == false;
        if (disabled)
        {
            ImGui.BeginDisabled();
        }
        if (menu.Children.Count == 0)
        {
            if (ImGui.MenuItem(menu.Name))
            {
                menu.Handler?.Invoke(this, menu);
            }
        }
        else
        {
            if (ImGui.BeginMenu(menu.Name))
            {
                foreach (var child in menu.Children)
                {
                    RenderMenu((MenuData)child);
                }
                ImGui.EndMenu();
            }
        }
        if (disabled)
        {
            ImGui.EndDisabled();
        }
    }

    internal ISystemPlugin? GetRomInstance(string romKind)
    {
        if (romPlugins.TryGetValue(romKind, out var Type))
        {
            return Activator.CreateInstance(Type) as ISystemPlugin;
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
                Log(LogType.Info, "Open Project", $"Project at {projectPath} is already open.");
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
            var playable = InternalInitialisePlugin(plugin, projectSettings, out var isNew);
            if (playable == null)
            {
                Log(LogType.Error, "Open Project", $"Failed to initialise plugin {projectSettings.RetroPluginName} for project at {projectPath}.");
                return;
            }
            if (!InternalInitialisePlayableRom(playable, projectSettings, isNew))
            {
                Log(LogType.Error, "Open Project", $"Failed to initialise playable ROM for project at {projectPath}.");
                return;
            }
        }
    }

    private void CloseProject(ActiveProject project)
    {
        activeProjects.Remove(project);
        windowManager.CloseAll(project.Settings);
        project.UnloadRetroPlugin();
    }

    private void OpenPlayerWindow(ActiveProject activeProject)
    {
        var pluginWindow = new LibRetroPlayerWindow(activeProject.LibRetroPlugin);
        OpenWindow(pluginWindow, $"LibRetro Player");
        pluginWindow.InitWindow();
    }

    private ActiveProject InternalAddDefaultWindowAndProject(ProjectSettings projectSettings, PlayableRom playableRom)
    {
        var activeProjectName = projectSettings.projectName + $" [{activeProjects.Count + 1}]";
        var project = new ActiveProject(activeProjectName, playableRom, projectSettings, new UIData());

        currentActiveProject=project;
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
        currentActiveProject = null;
        return project;
    }

    internal PlayableRom? CreateNewProject(string projectName, string projectLocation, string importFile, string retroPluginName, out ProjectSettings projectSettings)
    {
        // Todo Progress Dialog
        var projectPath = Path.Combine(projectLocation, projectName);
        Directory.CreateDirectory(projectPath);
        Directory.CreateDirectory(Path.Combine(projectPath, "Editor"));
        var projectFile = Path.Combine(projectPath, "Editor", projectName + ".json");

        // Generate hash for the plugin
        var hashA = MD5.Create().ComputeHash(File.ReadAllBytes(importFile));
        var hashB = MD5.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(projectName));
        var hashC = MD5.Create().ComputeHash(BitConverter.GetBytes(DateTime.Now.ToBinary()));
        var hash = MD5.Create().ComputeHash(hashA.Concat(hashB).Concat(hashC).ToArray());
        var retroCoreName = string.Concat(hash.Select(x => x.ToString("X2")));

        projectSettings = new ProjectSettings(projectName, projectPath, retroCoreName, retroPluginName, Path.GetFileName(importFile));
        projectSettings.Save(projectFile);
        File.Copy(importFile, GetRomPath(projectSettings), true);

        Directory.CreateDirectory(projectSettings.LibRetroPath);
        var retroPlugin = GetPluginInstance(retroPluginName);
        if (retroPlugin==null)
        {
            return null;
        }
        return InternalInitialisePlugin(retroPlugin, projectSettings, out _);
    }

    private PlayableRom? InternalInitialisePlugin(IRetroPlugin plugin, ProjectSettings projectSettings, out bool isNew)
    {
        isNew=false;
        // Initialise Rom
        var romInterface = GetRomInstance(plugin.RomPluginName);
        if (romInterface==null)
        {
            return null;
        }
        var emuPlugin = GetLibRetroInstance(romInterface.LibRetroPluginName, projectSettings, out isNew); 
        if (emuPlugin == null)
        {
            return null;
        }
        if (emuPlugin.Version() != 1)
        {
            return null;
        }
        emuPlugin.Init();
        return new PlayableRom(this, emuPlugin, romInterface, plugin);
    }

    internal bool InternalInitialisePlayableRom(PlayableRom playableRom, ProjectSettings projectSettings, bool firstTime)
    {
        if (playableRom.retroPlugin==null)
        {
            return false;
        }
        if (firstTime)
        {
            playableRom.Setup(projectSettings, GetRomPath(projectSettings), playableRom.retroPlugin.RequiresAutoLoad ? playableRom.retroPlugin.AutoLoadCondition : null);
        }
        else
        {
            playableRom.Reload(projectSettings);
        }

        var project = InternalAddDefaultWindowAndProject(projectSettings, playableRom);
        
        playableRom.retroPlugin.SetupGameTemporaryPatches(playableRom);

        playableRom.Reset(true);

        // Initialise menus only after rom is ready
        if (project.RetroPlugin is IMenuProvider menuProvider)
        {
            menuProvider.ConfigureMenu(project.PlayableRomPlugin, project);
        }

        return true;
    }

    public void SaveState(byte[] state, ProjectSettings projectSettings)
    {
        var stateFile = Path.Combine(projectSettings.LibRetroPath, $"{projectSettings.RetroCoreName}_state.bin");
        File.WriteAllBytes(stateFile, state);
    }

    public byte[] LoadState(ProjectSettings projectSettings)
    {
        var stateFile = Path.Combine(projectSettings.LibRetroPath, $"{projectSettings.RetroCoreName}_state.bin");
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

    internal enum OSSupportedResult
    {
        Ok,
        ArchitectureNotSupported,
        OSArchitectureNotSupported,
    }

    internal static OSSupportedResult GetOSStrings(out string platform, out string extra, out string extension, out string architecture)
    {
        bool supportsWindows = false;
        bool supportsLinux = false;
        bool supportsMacos = false;
        architecture = "";
        platform = "";
        extension = "";
        extra = "";
        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X64:
                architecture = "x86_64";
                supportsLinux = true;
                supportsWindows = true;
                break;
            case Architecture.Arm64:
                architecture = "arm64";
                supportsMacos = true;
                supportsWindows = true;
                break;
            default:
                return OSSupportedResult.ArchitectureNotSupported;
        }
        if (supportsWindows && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            platform = "windows";
            extension = ".dll";
        }
        else if (supportsLinux && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            platform = "linux";
            extension = ".so";
        }
        else if (supportsMacos && RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            extra = "apple";
            platform = "osx";
            extension = ".dylib";
        }
        else
        {
            return OSSupportedResult.OSArchitectureNotSupported;
        }
        return OSSupportedResult.Ok;
    }

    internal LibRetroPlugin? GetLibRetroInstance(string pluginName, ProjectSettings? projectSettings, out bool isNew)
    {
        isNew = false;
        var OS=RuntimeInformation.OSDescription;
        var supported = GetOSStrings(out var platform, out var extra, out var extension, out var architecture);
        switch (supported)
        {
            case OSSupportedResult.Ok:
                break;
            case OSSupportedResult.ArchitectureNotSupported:
                Log(LogType.Error, "Editor", $"Unsupported Architecture: {RuntimeInformation.ProcessArchitecture} - Cannot load lib retro plugin");
                return null;
            case OSSupportedResult.OSArchitectureNotSupported:
                Log(LogType.Error, "Editor", $"Unsupported OS + {RuntimeInformation.ProcessArchitecture}: {OS} - Cannot load lib retro plugin");
                return null;
        }

        var sourcePluginPath = Path.Combine(settings.RetroCoreFolder, platform, architecture);
        var sourcePlugin = Path.Combine(sourcePluginPath, $"{pluginName}{extension}");
        string? destinationPlugin;
        string? destinationPluginPath;
        if (projectSettings == null)
        {
            // Developer mode, we don't have a project folder
            destinationPluginPath = sourcePluginPath;
            destinationPlugin = sourcePlugin;
        }
        else
        {
            destinationPluginPath = projectSettings.LibRetroPath;
            if (!Directory.Exists(destinationPluginPath))
            {
                Directory.CreateDirectory(destinationPluginPath);   // in case we are upgrading from an old project that didn't have the subfolders
            }
            destinationPlugin = Path.Combine(destinationPluginPath, $"{projectSettings.RetroCoreName}{extension}");
        }
        if (!File.Exists(destinationPlugin))
        {
            isNew = true;
            if (!File.Exists(sourcePlugin))
            {
                var task = DownloadLibRetro(platform, extra, architecture, extension, pluginName);
                task.Wait();
                if (task.Result!=true)
                {
                    return null;
                }
            }

            if (destinationPlugin != sourcePlugin)
            {
                File.Copy(sourcePlugin, destinationPlugin, true);
                // We also need to copy any extra files (dependencies for dlls)
                if (File.Exists(Path.Combine(sourcePluginPath, "extras.lst")))
                {
                    var lines = File.ReadAllLines(Path.Combine(sourcePluginPath, "extras.lst"));
                    foreach (var line in lines)
                    {
                        var sourceExtra = Path.Combine(sourcePluginPath, line);
                        var destinationExtra = Path.Combine(destinationPluginPath, line);
                        File.Copy(sourceExtra, destinationExtra, true);
                    }
                }
            }
        }

        try 
        {
            return new LibRetroPlugin(this, destinationPlugin);
        }
        catch (Exception e)
        {
            Log(LogType.Error, "Editor", $"Failed to load plugin {pluginName} from {destinationPlugin}: {e.Message}");
            return null;
        }
    }

    internal LibRetroPlugin? GetDeveloperMame()
    {
        var OS=RuntimeInformation.OSDescription;
        var supported = GetOSStrings(out var platform, out _, out var extension, out var architecture);
        switch (supported)
        {
            case OSSupportedResult.Ok:
                break;
            case OSSupportedResult.ArchitectureNotSupported:
                Log(LogType.Error, "Editor", $"Unsupported Architecture: {RuntimeInformation.ProcessArchitecture} - Cannot load lib mame debugger plugin");
                return null;
            case OSSupportedResult.OSArchitectureNotSupported:
                Log(LogType.Error, "Editor", $"Unsupported OS + {RuntimeInformation.ProcessArchitecture}: {OS} - Cannot load lib mame debugger plugin");
                return null;
        }

        var destinationPlugin = Path.Combine(settings.RetroCoreFolder, "developer", platform, architecture, $"mame_libretro{extension}");
        if (!File.Exists(destinationPlugin))
        {
            var task = DownloadDeveloperMame(platform, architecture, extension);
            task.Wait();
            if (task.Result!=true)
            {
                return null;
            }
        }

        try 
        {
            return new LibRetroPlugin(this, destinationPlugin);
        }
        catch (Exception e)
        {
            Log(LogType.Error, "Editor", $"Failed to load developer mame plugin from {destinationPlugin}: {e.Message}");
            return null;
        }
    }

    async Task<bool> DownloadDeveloperMame(string platform, string architecture, string extension)
    {
        var api_revision = "v1.261.1"; // TODO - link this to the extension api
        var extra = $"_{architecture}";
        var url = $"https://github.com/SavourySnaX/lib_mame_retro_custom_fork/releases/download/{api_revision}/build_{platform}{extra}.zip";
        var destinationFolder = Path.Combine(settings.RetroCoreFolder, "developer", platform, architecture);
        var itemToGrab = $"mame_libretro{extension}";
        Log(LogType.Info, $"Downloading {url} as local copy not present");
        return await Download(url, destinationFolder, itemToGrab);
    }


    async Task<bool> DownloadLibRetro(string platform, string extra, string architecture, string extension, string pluginName)
    {
        if (!string.IsNullOrEmpty(extra))
        {
            extra = $"{extra}/";
        }
        var destinationFolder = Path.Combine(settings.RetroCoreFolder, platform, architecture);
        var itemToGrab = $"{pluginName}{extension}";
        var url = $"http://buildbot.libretro.com/nightly/{extra}{platform}/{architecture}/latest/{pluginName}{extension}.zip";
        if (platform=="windows" && architecture=="arm64")
        {
            // At present buildbot does not have arm64 builds for windows
            // as a workaround, I've published the ones built at https://github.com/talynone/RetroArch/releases/
            // individually to github location so we can download them from there
            // annoyingly these will also need copying to project folder too.. which means the project folder structure
            // should probably be standardised to include platform/architecture folders

            // In addition, there are some extra dependencies needed so we need to validate/download those too.
            var extraCheck = Path.Combine(destinationFolder, "extras.txt");
            Task<bool> result= null!;
            if (!File.Exists(extraCheck))
            {
                var extrasUrl = $"http://github.com/SavourySnaX/prebuilt-windows-dependencies/releases/latest/download/extras.zip";

                Log(LogType.Info, $"Downloading {extrasUrl} as local copy not present");
                result = Download(extrasUrl, destinationFolder);
                await result;
                if (result.Result!=true)    // its not the end of the world to do this concurrently
                {
                    return false;
                }
            }
            url = $"http://github.com/SavourySnaX/prebuilt-windows-dependencies/releases/latest/download/{pluginName}{extension}.zip";
        }
        Log(LogType.Info, $"Downloading {url} as local copy not present");
        return await Download(url, destinationFolder, itemToGrab);
    }

    async Task<bool> Download(string url, string destinationFolder, params string[] itemsToGrab)
    {
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
                            if (itemsToGrab.Length==0 || itemsToGrab.Contains(entry.Name))
                            {
                                if (destinationFolder == null)
                                {
                                    return false;
                                }
                                Directory.CreateDirectory(destinationFolder);  // Ensure destination folder exists
                                using (var fileStream = File.Create(Path.Combine(destinationFolder, entry.Name)))
                                {
                                    entry.Open().CopyTo(fileStream);
                                }
                            }
                        }
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public void OpenWindow(IWindow window, string name)
    {
        var windowName=name;
        if (currentActiveProject != null)
        {
            var active = currentActiveProject.Value;
            windowName = $"{name} ({active.Name})";
        }

        if (windowManager.IsOpen(windowName))
        {
            return;
        }
        window.Initialise();

        if (currentActiveProject != null)
        {
            var active = currentActiveProject.Value;
            windowManager.AddWindow(window, windowName, active);
        }
        else
        {
            windowManager.AddWindow(window, windowName, null);
        }
    }

    public void CloseWindow(string name)
    {
        windowManager.Close(name);
    }

    public void OpenUserWindow(string name, IUserWindow window)
    {
        if (currentActiveProject != null)
        {
            var userWindow = new UserWindow(window);
            OpenWindow(userWindow, name);
        }
    }

    public void Log(LogType type, string message)
    {
        if (currentActiveProject!=null)
        {
            var active = currentActiveProject.Value;
            Log(type, active.Settings.RetroPluginName, message);
        }
        else
        {
            Log(type, "Unknown", message);
        }
    }

    public void OpenFileDialog(FileDialog.FileDialogType type, string title, string defaultPath, string[] filters, Func<string, bool> onSelected)
    {
        var fileDialog = new FileDialog(type, title, defaultPath, filters, onSelected);
        fileDialog.Initialise();
        windowManager.AddBlockingPopup(fileDialog, "File Dialog");
    }

}
