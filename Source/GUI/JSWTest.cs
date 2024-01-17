using System.Security.Cryptography;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;

public class JSWTest : IWindow
{
    Texture2D bitmap;

    LibRetroPlugin plugin;
    LibRetroPlugin.RetroSystemAVInfo aVInfo;
    float scale = 2.0f;
    string name;

    public JSWTest(LibRetroPlugin plugin, string name)
    {
        this.plugin = plugin;
        this.name = name;
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
        if (name == "Fuse")
        {
            plugin.LoadGame("c:\\work\\editor\\jsw\\Jet Set Willy (1984)(Software Projects).tzx");

            // Load to a defined point (because we are loading from tape)
            plugin.AutoLoad(() =>
            {
                var memory = plugin.GetMemory(0x4000, 0x1800);  // Load until screen memory contains the pattern...
                var hash = MD5.Create().ComputeHash(memory);
                return hash.SequenceEqual(screenHash);
            });
        }
        else
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

        // We should save snapshot, so we don't need to load from tape again...
        var saveSize = plugin.GetSaveStateSize();
        var state = new byte[saveSize];
        plugin.SaveState(state);

        aVInfo = plugin.GetSystemAVInfo();
        var image = Raylib.GenImageColor((int)aVInfo.geometry.baseWidth, (int)aVInfo.geometry.baseHeight, Color.BLACK);
        image = new Image
        {
            Width = (int)aVInfo.geometry.baseWidth,
            Height = (int)aVInfo.geometry.baseHeight,
            Mipmaps = 1,
            Format = PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8
        };

        bitmap = Raylib.LoadTextureFromImage(image);

        if (name == "Fuse")
        {
            // FROM HERE
            plugin.RestoreState(state);

            // Kill copy protection code, and jump to screen to test...
            plugin.SetMemory(0x8785, new byte[] { 0xC9 });          // Store return to force out of cheat code key wait
            plugin.SetMemory(0x872C, new byte[] { 0xCA, 0x87 });    // Jump to game start
            plugin.SetMemory(0x88AC, new byte[] { 0xFC, 0x88 });    // start game

            byte yPos = 13 * 8;
            byte xPos = 1 * 8;
            byte roomNumber = 0x22;

            ushort attributeAddress = (ushort)(0x5C00 + ((yPos / 8) * 32) + (xPos / 8));

            plugin.SetMemory(0x87E6, new byte[] { (byte)(yPos * 2) });          // willys y cordinate
            plugin.SetMemory(0x87F0, new byte[] { (byte)(attributeAddress & 0xFF), (byte)(attributeAddress >> 8) });    // willys cordinate
            plugin.SetMemory(0x87EB, new byte[] { (byte)(roomNumber) });
            // TO HERE
        }
        return true;
    }

    public readonly byte[] screenHash = { 17, 6, 168, 144, 11, 145, 236, 80, 76, 26, 162, 160, 98, 1, 0, 211 };

    public void Update(float seconds)
    {
        plugin.Run();
        Raylib.UpdateTexture(bitmap, plugin.GetFrameBuffer());
    }

    public float UpdateInterval => (float)(1.0 / aVInfo.timing.fps);

    public bool Draw()
    {
        bool open = true;
        ImGui.Begin($"Player {name}",ref open);

        rlImGui.ImageSize(bitmap, (int)(aVInfo.geometry.baseWidth * scale), (int)(aVInfo.geometry.baseHeight * scale));

        if (ImGui.IsWindowFocused())
        {
            // JSW keys
            plugin.UpdateKey(KeyboardKey.KEY_SPACE, ImGui.IsKeyDown(ImGuiKey.Space));
            plugin.UpdateKey(KeyboardKey.KEY_O, ImGui.IsKeyDown(ImGuiKey.O));
            plugin.UpdateKey(KeyboardKey.KEY_P, ImGui.IsKeyDown(ImGuiKey.P));

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

        return open;
    }

    public void Close()
    {
    }

}