
using System.Diagnostics;
using Veldrid;
using Veldrid.StartupUtilities;
using ImGuiNET;
using System.Security.Cryptography;

class Editor : IEditor
{
    private Dictionary<string, Type> romPlugins;
    private IRetroPlugin[] plugins;
    private List<IRetroPlugin> activePlugins;

    private List<IWindow> activeWindows;
    private bool[] keystate = new bool[65536];


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
        ImGuiController controller;

        var windowInfo = new WindowCreateInfo();
        windowInfo.WindowTitle = "Editor";
        windowInfo.WindowWidth=1024+512;
        windowInfo.WindowHeight=800;
        windowInfo.X = 50;
        windowInfo.Y = 50;
        //windowInfo.WindowInitialState = WindowState.Maximized;

        VeldridStartup.CreateWindowAndGraphicsDevice(
            windowInfo,
            new GraphicsDeviceOptions(true, null, true, ResourceBindingModel.Improved, true, true),
            out var window,
            out var graphicsDevice
        );
        
        var commandList = graphicsDevice.ResourceFactory.CreateCommandList();
        controller = new ImGuiController(graphicsDevice, graphicsDevice.MainSwapchain.Framebuffer.OutputDescription, window.Width, window.Height);

        window.Resized += () =>
        {
            graphicsDevice.MainSwapchain.Resize((uint)window.Width, (uint)window.Height);
            controller.WindowResized(window.Width, window.Height);
        };

        var stopwatch = Stopwatch.StartNew();
        float deltaTime = 0f;
        // Main application loop

        var timer = new Stopwatch();
        timer.Start();

        var args = Environment.GetCommandLineArgs();
        foreach (var arg in args)
        {
            OpenFile(arg);
        }

        float totalTime=0.0f;
        while (window.Exists)
        {
            deltaTime = stopwatch.ElapsedTicks / (float)Stopwatch.Frequency;
            stopwatch.Restart();
            InputSnapshot snapshot = window.PumpEvents();
            if (!window.Exists) { break; }
            controller.Update(deltaTime, snapshot);

            if (timer.ElapsedMilliseconds>1500)
            {
                timer.Restart();
            }

            DrawUI(controller,graphicsDevice,totalTime);
            totalTime += deltaTime;

            commandList.Begin();
            commandList.SetFramebuffer(graphicsDevice.MainSwapchain.Framebuffer);
            commandList.ClearColorTarget(0, new RgbaFloat(0.0f, 0.0f, 0.0f, 1f));
            controller.Render(graphicsDevice, commandList);
            commandList.End();
            graphicsDevice.SubmitCommands(commandList);
            graphicsDevice.SwapBuffers(graphicsDevice.MainSwapchain);
        }

        foreach (var mWindow in activeWindows)
        {
            mWindow.Close(controller, graphicsDevice);
        }
        // Clean up Veldrid resources
        graphicsDevice.WaitForIdle();
        controller.Dispose();
        commandList.Dispose();
        graphicsDevice.Dispose();

        activeWindows.Clear();
        foreach (var active in activePlugins)
        {
            active.Close();
        }
    }

    private void OpenFile(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var md5 = MD5.Create().ComputeHash(bytes);
        foreach (var plugin in plugins)
        {
            if (plugin.CanHandle(md5, bytes, path))
            {
                if (plugin.Init(this, md5, bytes, path))
                {
                    activePlugins.Add(plugin);
                }
                break;
            }
        }
    }

    private void DrawUI(ImGuiController controller, GraphicsDevice graphicsDevice, float totalTime)
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
                    mame.Initialise(controller, graphicsDevice);
                    activeWindows.Add(mame);
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
                                        window.Initialise(controller, graphicsDevice);
                                        activeWindows.Add(window);
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
                                        window.Initialise(controller, graphicsDevice);
                                        activeWindows.Add(window);
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

        foreach (var window in activeWindows)
        {
            window.Update(controller, graphicsDevice, totalTime);
            if (!window.Draw())
            {
                window.Close(controller, graphicsDevice);
                activeWindows.Remove(window);
                break;
            }
        }
    }

    public IRomPlugin? GetRomInstance(string romKind)
    {
        if (romPlugins.TryGetValue(romKind, out var Type))
        {
            return Activator.CreateInstance(Type) as IRomPlugin;
        }
        return null;
    }
}
