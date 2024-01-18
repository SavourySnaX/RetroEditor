using Raylib_cs;
using rlImGui_cs;
using ImGuiNET;
using System.Diagnostics;
using System.Security.Cryptography;

class Editor : IEditor
{
    private Dictionary<string, Type> romPlugins;
    private IRetroPlugin[] plugins;
    private List<IRetroPlugin> activePlugins;

    private List<IWindow> activeWindows;


    public Editor(IRetroPlugin[] plugins, IRomPlugin[] romPlugins)
    {
        this.plugins = plugins;

        this.activePlugins = new List<IRetroPlugin>();
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
            OpenFile(arg);
        }

        totalTime=0.0f;
        
        // Testing
        var pluginWindow = new JSWTest(LibRetroPluginFactory.Create("fuse_libretro","C:\\work\\editor\\RetroEditor\\data\\1.dll"), "Flibble");
        pluginWindow.Initialise();
        pluginWindow.OtherStuff();
        pluginWindow.InitWindow();
        AddWindow(pluginWindow);
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
        foreach (var active in activePlugins)
        {
            active.Close();
        }
    }

    private void AddWindow(IWindow window)
    {
        activeWindows.Add(window);
        var newTime = totalTime + window.UpdateInterval;
        priorityQueue.Enqueue((window, newTime),newTime);
    }

    private void OpenFile(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var md5 = MD5.Create().ComputeHash(bytes);
        foreach (var plugin in plugins)
        {
            if (plugin.CanHandle(md5, bytes, path))
            {
                if (plugin.Init(this, md5, bytes, path, out var retroPlugin))
                {
                    var pluginWindow = new JSWTest(retroPlugin, "Fuse");
                    pluginWindow.InitWindow();
                    AddWindow(pluginWindow);
                    activePlugins.Add(plugin);
                }
                break;
            }
        }
    }

    private void DrawUI()
    {
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("Open"))
                {
                    var result = NativeFileDialogSharp.Dialog.FileOpen();

                    if (result.IsOk)
                    {
                        OpenFile(result.Path);
                    }
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
                foreach (var active in activePlugins)
                {
                    if (ImGui.BeginMenu(active.Name))
                    {
                        var imageInterface = active.GetImageInterface();
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
                                        var window = new ImageWindow(active, map);
                                        window.Initialise();
                                        AddWindow(window);
                                    }
                                }
                                ImGui.EndMenu();
                            }
                        }
                        var tileMapInterface = active.GetTileMapInterface();
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
                                        var window = new TileMapEditorWindow(active, map);
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
}