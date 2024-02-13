using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;

public class LibRetroDebuggerWindow : IWindow
{
    Texture2D bitmap;

    LibRetroPlugin plugin;
    LibRetroPlugin.RetroSystemAVInfo aVInfo;
    float scale = 1.0f;

    uint frameWidth, frameHeight;
    IPlayerWindowExtension extension;
    IPlayerControls controls;
    Thread mameThread;
    bool notDone;

    public LibRetroDebuggerWindow(LibRetroPlugin plugin, IPlayerControls controls, IPlayerWindowExtension extension)
    {
        this.plugin = plugin;
        this.extension = extension;
        this.controls = controls;
        this.notDone = true;
    }

    public bool Initialise()
    {
        if (plugin.Version() != 1)
        {
            return false;
        }
        plugin.Init();

        // Spwan a thread to handle mame, since in mame the debugger blocks retro_run when stopped on a breakpoint etc
        // This is a workaround to keep the UI responsive
        mameThread = new Thread(() =>
        {
            while (notDone)
            {
                Thread.Sleep((int)Math.Floor(UpdateInterval*1000));
                plugin.Run();
            }
        });
        mameThread.Name = "MameRunnerThread";

        return true;
    }

    public void InitWindow()
    {
        aVInfo = plugin.GetSystemAVInfo();
        frameHeight= aVInfo.geometry.maxHeight;
        frameWidth = aVInfo.geometry.maxWidth;
        var image = new Image
        {
            Width = (int)aVInfo.geometry.maxWidth,
            Height = (int)aVInfo.geometry.maxHeight,
            Mipmaps = 1,
            Format = PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8
        };

        bitmap = Raylib.LoadTextureFromImage(image);
        mameThread.Start();
    }

    public void Update(float seconds)
    {
        extension?.Update(seconds);
        Raylib.UpdateTexture(bitmap, plugin.GetFrameBuffer(out frameWidth, out frameHeight));
    }

    public float UpdateInterval => (float)(1.0 / aVInfo.timing.fps);

    private bool audioEnabled = false;
    string inputBuffer;
    string log;

    public bool Draw()
    {
        if (ImGui.Checkbox("Audio", ref audioEnabled))
        {
            plugin.SwitchAudio(audioEnabled);
        }

        rlImGui.ImageRect(bitmap, (int)(aVInfo.geometry.baseWidth * scale), (int)(aVInfo.geometry.baseHeight * scale), new Rectangle(0,0,frameWidth,frameHeight));

        if (ImGui.IsWindowFocused())
        {
            // JSW keys
            plugin.UpdateKey(KeyboardKey.KEY_SPACE, ImGui.IsKeyDown(ImGuiKey.Space));
            plugin.UpdateKey(KeyboardKey.KEY_O, ImGui.IsKeyDown(ImGuiKey.O));
            plugin.UpdateKey(KeyboardKey.KEY_P, ImGui.IsKeyDown(ImGuiKey.P));

            // Rollercoaster keys
            plugin.UpdateKey(KeyboardKey.KEY_ENTER, ImGui.IsKeyDown(ImGuiKey.Enter));
            plugin.UpdateKey(KeyboardKey.KEY_M, ImGui.IsKeyDown(ImGuiKey.M));
            plugin.UpdateKey(KeyboardKey.KEY_LEFT_SHIFT, ImGui.IsKeyDown(ImGuiKey.LeftShift));
            plugin.UpdateKey(KeyboardKey.KEY_RIGHT_SHIFT, ImGui.IsKeyDown(ImGuiKey.RightShift));

            // JOYPAD emulation 
            plugin.UpdateKey(KeyboardKey.KEY_UP, ImGui.IsKeyDown(ImGuiKey.UpArrow));
            plugin.UpdateKey(KeyboardKey.KEY_DOWN, ImGui.IsKeyDown(ImGuiKey.DownArrow));
            plugin.UpdateKey(KeyboardKey.KEY_LEFT, ImGui.IsKeyDown(ImGuiKey.LeftArrow));
            plugin.UpdateKey(KeyboardKey.KEY_RIGHT, ImGui.IsKeyDown(ImGuiKey.RightArrow));
            plugin.UpdateKey(KeyboardKey.KEY_Z, ImGui.IsKeyDown(ImGuiKey.Z));
            plugin.UpdateKey(KeyboardKey.KEY_X, ImGui.IsKeyDown(ImGuiKey.X));
            plugin.UpdateKey(KeyboardKey.KEY_A, ImGui.IsKeyDown(ImGuiKey.A));
            plugin.UpdateKey(KeyboardKey.KEY_S, ImGui.IsKeyDown(ImGuiKey.S));
            plugin.UpdateKey(KeyboardKey.KEY_M, ImGui.IsKeyDown(ImGuiKey.M));
            plugin.UpdateKey(KeyboardKey.KEY_N, ImGui.IsKeyDown(ImGuiKey.N));
        }

        extension?.Render(controls);
        return false;
    }

    public void Close()
    {
        // probably need to send something to unblock the mame thread though
        notDone = false;
    }
}