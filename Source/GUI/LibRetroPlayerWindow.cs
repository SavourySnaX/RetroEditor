using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;

public class LibRetroPlayerWindow : IWindow
{
    Texture2D bitmap;

    LibRetroPlugin plugin;
    LibRetroPlugin.RetroSystemAVInfo aVInfo;
    float scale = 2.0f;
    string name;

    uint frameWidth, frameHeight;

    public LibRetroPlayerWindow(LibRetroPlugin plugin, string uniqueName)
    {
        this.plugin = plugin;
        this.name = uniqueName;
    }

    public bool Initialise()
    {
        if (plugin.Version() != 1)
        {
            return false;
        }
        plugin.Init();

        return true;
    }

    public bool OtherStuff()
    {
        /*
        else if (name == "FCEU")
        {
            var name = "C:\\work\\editor\\nes\\metroid.nes";
            var game = File.ReadAllBytes(name);
            // since a nes cartridge is rom, we patch here 

            game[16 + 0x0A] = 0x80;    // Press Start - advance to new game screen
            game[16 + 0x10DD] = 0x80;   // Press new game

            //game[16 + 0x1325] = 0x34;   // Y POS
            //game[16 + 0x1328] = 0x78;   // X POS


            // Brinstar
            game[16 + 0x55D7] = 3;      // 0x4000 + (0x95D7-0x8000)     MAP X Position
            game[16 + 0x55D8] = 2;      // 0x4000 + (0x95D8-0x8000)     MAP Y Position
            game[16 + 0x55D9] = 0x50;   // 0x4000 + (0x95D9-0x8000)     Start Y


            //15 Y
            //X 2

            //game[16 + 0x253E + (14 * 32) + 3] = 0x17;   // Modify map room number....
            plugin.LoadGame(name,game);
        }
*/
        // We should save snapshot, so we don't need to load from tape again...
        var saveSize = plugin.GetSaveStateSize();
        var state = new byte[saveSize];
        plugin.SaveState(state);

        return true;
    }

    public void InitWindow()
    {
        aVInfo = plugin.GetSystemAVInfo();
        frameHeight= aVInfo.geometry.maxHeight;
        frameWidth = aVInfo.geometry.maxWidth;
        var image = Raylib.GenImageColor((int)aVInfo.geometry.maxWidth, (int)aVInfo.geometry.maxHeight, Color.BLACK);
        image = new Image
        {
            Width = (int)aVInfo.geometry.maxWidth,
            Height = (int)aVInfo.geometry.maxHeight,
            Mipmaps = 1,
            Format = PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8
        };

        bitmap = Raylib.LoadTextureFromImage(image);
    }

    public void Update(float seconds)
    {
        plugin.Run();
        Raylib.UpdateTexture(bitmap, plugin.GetFrameBuffer(out frameWidth, out frameHeight));
    }

    public float UpdateInterval => (float)(1.0 / aVInfo.timing.fps);

    public bool Draw()
    {
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

        ImGui.End();

        return false;
    }

    public void Close()
    {
    }

}