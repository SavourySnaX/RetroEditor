using System.Numerics;
using System.Security.Cryptography;
using ImGuiNET;
using Veldrid;

public class JSWTest : IWindow
{
    Texture? bitmap;
    nint bitmapId;

    LibRetroPlugin plugin;
    LibRetroPlugin.RetroSystemAVInfo aVInfo;
    float scale = 2.0f;

    public JSWTest(LibRetroPlugin plugin)
    {
        this.plugin = plugin; 
    }

    public bool Initialise(ImGuiController controller,GraphicsDevice graphicsDevice)
    {
        if (plugin.Version() != 1)
        {
            return false;
        }
        plugin.Init();
        plugin.LoadGame("c:\\work\\editor\\jsw\\Jet Set Willy (1984)(Software Projects).tzx");

        // Load to a defined point (because we are loading from tape)
        plugin.AutoLoad(() =>
        {
            var memory = plugin.GetMemory(0x4000, 0x1800);  // Load until screen memory contains the pattern...
            var hash = MD5.Create().ComputeHash(memory);
            return (hash.SequenceEqual(screenHash));
        });

        // We should save snapshot, so we don't need to load from tape again...
        var saveSize = plugin.GetSaveStateSize();
        var state = new byte[saveSize];
        plugin.SaveState(state);

        aVInfo = plugin.GetSystemAVInfo();
        bitmap = graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(aVInfo.geometry.baseWidth, aVInfo.geometry.baseHeight, 1, 1, PixelFormat.B8_G8_R8_A8_UNorm, TextureUsage.Sampled));
        bitmap.Name = $"JSWTest";
        bitmapId = controller.GetOrCreateImGuiBinding(graphicsDevice.ResourceFactory, bitmap);

// FROM HERE
        plugin.RestoreState(state);

        // Kill copy protection code, and jump to screen to test...
        plugin.SetMemory(0x8785, new byte[] { 0xC9 });          // Store return to force out of cheat code key wait
        plugin.SetMemory(0x872C, new byte[] { 0xCA, 0x87 });    // Jump to game start
        plugin.SetMemory(0x88AC, new byte[] { 0xFC, 0x88 });    // start game

        byte yPos = 13*8;
        byte xPos = 1*8;
        byte roomNumber = 0x22;

        ushort attributeAddress = (ushort)(0x5C00 + ((yPos / 8) * 32) + (xPos / 8));

        plugin.SetMemory(0x87E6, new byte[] { (byte)(yPos*2) });          // willys y cordinate
        plugin.SetMemory(0x87F0, new byte[] { (byte)(attributeAddress & 0xFF), (byte)(attributeAddress >> 8) });    // willys cordinate
        plugin.SetMemory(0x87EB, new byte[] { (byte)(roomNumber) });
// TO HERE
        return true;
    }

    public readonly byte[] screenHash = { 17, 6, 168, 144, 11, 145, 236, 80, 76, 26, 162, 160, 98, 1, 0, 211 };

    public void Update(ImGuiController controller,GraphicsDevice graphicsDevice, float seconds)
    {
        plugin.Run();
        graphicsDevice.UpdateTexture(bitmap, plugin.GetFrameBuffer(), 0, 0, 0, aVInfo.geometry.baseWidth, aVInfo.geometry.baseHeight, 1, 0, 0);
    }

    public bool Draw()
    {
        bool open = true;
        ImGui.Begin($"JSW Player",ref open);

        ImGui.Image(bitmapId, new Vector2(aVInfo.geometry.maxWidth * scale, aVInfo.geometry.maxHeight * scale));

        ImGui.End();

        return open;
    }

    public void Close(ImGuiController controller,GraphicsDevice graphicsDevice)
    {
    }
}
