using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using RetroEditor.Plugins;

public class LinkToThePast : IRetroPlugin, IMenuProvider
{
    public static string Name => "The Legend of Zelda: A Link to the Past";

    public string RomPluginName => "SNES";

    public bool RequiresAutoLoad => false;

    //    byte[] link_to_the_past_jp_headerless = [0x03, 0xa6, 0x39, 0x45, 0x39, 0x81, 0x91, 0x33, 0x7e, 0x89, 0x6e, 0x57, 0x71, 0xf7, 0x71, 0x73];
    byte[] link_to_the_past_us_headerless = [0x60, 0x8c, 0x22, 0xb8, 0xff, 0x93, 0x0c, 0x62, 0xdc, 0x2d, 0xe5, 0x4b, 0xcd, 0x6e, 0xba, 0x72];

    public bool AutoLoadCondition(IMemoryAccess romAccess)
    {
        throw new System.NotImplementedException("AutoLoadCondition not required");
    }

    public bool CanHandle(string path)
    {
        if (!File.Exists(path))
            return false;
        var bytes = File.ReadAllBytes(path);
        var hash = MD5.Create().ComputeHash(bytes);
        return hash.SequenceEqual(link_to_the_past_us_headerless);
    }

    public ISave Export(IMemoryAccess romAccess)
    {
        throw new System.NotImplementedException();
    }

    public void SetupGameTemporaryPatches(IMemoryAccess romAccess)
    {
        // Implement any necessary patches for the game
    }

    public LinkToThePastTesting GetTesting(IEditor editorInterface, IMemoryAccess rom)
    {
        return new LinkToThePastTesting(editorInterface, rom);
    }

    public void ConfigureMenu(IMemoryAccess rom, IMenu menu)
    {
        menu.AddItem("Testing",
            (editorInterface, menuItem) =>
            {
                editorInterface.OpenUserWindow($"Testing", GetTesting(editorInterface, rom));
            });
    }
}

public class LinkToThePastTesting : IUserWindow, IImage
{
    private readonly IEditor _editorInterface;
    private readonly IMemoryAccess _rom;

    public uint Width => 512;

    public uint Height => 512;

    public float ScaleX => 1.0f;

    public float ScaleY => 1.0f;

    public float UpdateInterval => 1 / 30.0f;

    public LinkToThePastTesting(IEditor editorInterface, IMemoryAccess rom)
    {
        _editorInterface = editorInterface;
        _rom = rom;
    }

    public ReadOnlySpan<Pixel> GetImageData(float seconds)
    {
        return new Pixel[Width * Height];
    }

    public void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls)
    {
    }

    public void OnClose()
    {
    }
}

