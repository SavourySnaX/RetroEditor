using System.IO;
using System.Security.Cryptography;
using System.Linq;

using RetroEditor.Plugins;
using System;

public class SuperMarioWorld : IRetroPlugin , IMenuProvider
{
	public static string Name => "Super Mario World";
	public string RomPluginName => "SNES";
	public bool RequiresAutoLoad => false;

    byte[] smw_japan = new byte[] { 0x4e, 0x4f, 0x8f, 0x4c, 0xfd, 0xaa, 0xbf, 0xfd, 0xde, 0x20, 0xc8, 0x05, 0x32, 0x02, 0xd4, 0xf0 };

    public bool AutoLoadCondition(IMemoryAccess romAccess)
    {
        throw new System.NotImplementedException("AutoLoadCondition not required");
    }

    public bool CanHandle(string filename)
    {
        if (!File.Exists(filename))
            return false;
        var bytes = File.ReadAllBytes(filename);
        var hash = MD5.Create().ComputeHash(bytes);
        return hash.SequenceEqual(smw_japan);
    }

	public void SetupGameTemporaryPatches(IMemoryAccess memoryAccess) 
    { 

    }

	public ISave Export(IMemoryAccess memoryAccess)
    {
        throw new System.NotImplementedException("Export not implemented");
    }

    public void ConfigureMenu(IMemoryAccess rom, IMenu menu)
    {
            menu.AddItem("Testing", 
                (editorInterface,menuItem) => {
                    editorInterface.OpenUserWindow($"Image View", GetImage(rom));
                });
    }

    public SuperMarioWorldTestImage GetImage(IMemoryAccess rom)
    {
        return new SuperMarioWorldTestImage(rom);
    }
}

public class SuperMarioWorldTestImage : IImage, IUserWindow
{
    public uint Width => 512;

    public uint Height => 512;

    public float ScaleX => 1.0f;

    public float ScaleY => 1.0f;

    public float UpdateInterval => 1/60.0f;

    private IMemoryAccess _rom;
    private IWidgetRanged temp_levelSelect;

    public SuperMarioWorldTestImage(IMemoryAccess rom)
    {
        _rom = rom;
    }

    public void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls)
    {
        widget.AddImageView(this);
        temp_levelSelect = widget.AddSlider("Level", 6, 0, 512, () => { });
    }

    uint SNESAddressToCartAddress(uint address)
    {
        uint bank = (address >> 16) & 0xFF;
        uint rom = address&0xFFFF;

        if (rom<0x8000)
        {
            throw new Exception("Invalid address");
        }
        rom-=0x8000;
        return bank*0x8000+rom;
    }

    public ReadOnlySpan<Pixel> GetImageData(float seconds)
    {

        // Get the level select value, index into the layer 1 data etc
        var levelSelect = (uint)temp_levelSelect.Value;
        var layer1Data = _rom.ReadBytes(ReadKind.Rom, SNESAddressToCartAddress(0x05E000 + 3 * levelSelect), 3);
        var layer2Data = _rom.ReadBytes(ReadKind.Rom, SNESAddressToCartAddress(0x05E600 + 3 * levelSelect), 3);
        var spriteData = _rom.ReadBytes(ReadKind.Rom, SNESAddressToCartAddress(0x05EC00 + 2 * levelSelect), 2);

        uint layer0Address = SNESAddressToCartAddress((uint)((layer1Data[2] << 16) | (layer1Data[1] << 8) | layer1Data[0]));

        var headerData = _rom.ReadBytes(ReadKind.Rom, layer0Address, 5);

        var byte0 = headerData[0];  // BBBLLLLL  B-BG Palette, L-Number of screens -1
        var byte1 = headerData[1];  // CCC00000  C-Back area colour
        var byte2 = headerData[2];  // 3MMMSSSS  3-layer 3 priority, M-Music, S-Sprite GFX Setting
        var byte3 = headerData[3];  // TTPPPFFF  T - timer setting, P - Sprite Palette, F - FG Palette
        var byte4 = headerData[4];  // IIVVZZZZ  I - item memory setting, V - vertical scroll setting, Z - FG/BG GFX Setting


        var graphicData000_072 = _rom.ReadBytes(ReadKind.Rom, SNESAddressToCartAddress(0x0D8000), 920);   // TTTTTTTT YXPCCCTT  T-Tile, Y-Y flip, X-X flip, P-Palette, C-Colour, T-Top priority

        var pixels = new Pixel[Width * Height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Pixel(0, 0, 0, 255);
        }
        return pixels;
    }

    public void OnClose()
    {
    }
}
