using System.IO;
using System.Security.Cryptography;
using System.Linq;

using RetroEditor.Plugins;
using System;

// To do - move to editor library
interface AddressTranslation
{
    uint ToImage(uint address);
    uint FromImage(uint address);
}

public class LoRom : AddressTranslation
{
    public uint ToImage(uint address)
    {
        uint bank = (address >> 16) & 0xFF;
        uint rom = address&0xFFFF;

        if (rom<0x8000)
        {
            throw new System.Exception("Invalid address");
        }
        rom-=0x8000;
        return bank*0x8000+rom;
    }

    public uint FromImage(uint address)
    {
        uint bank = address / 0x8000;
        uint rom = address % 0x8000;
        return (bank << 16) | rom;
    }
}


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
                    editorInterface.OpenUserWindow($"Image View", GetImage(editorInterface, rom));
                });
    }

    public SuperMarioWorldTestImage GetImage(IEditor editorInterface, IMemoryAccess rom)
    {
        return new SuperMarioWorldTestImage(editorInterface, rom);
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
    private AddressTranslation _addressTranslation;
    private IWidgetRanged temp_levelSelect;
    private IEditor _editorInterface;

    public SuperMarioWorldTestImage(IEditor editorInterface, IMemoryAccess rom)
    {
        _rom = rom;
        _addressTranslation = new LoRom();
        _editorInterface = editorInterface;
    }

    public void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls)
    {
        widget.AddImageView(this);
        temp_levelSelect = widget.AddSlider("Level", 6, 0, 512, () => { });
    }

    private bool runOnce = false;

    public ReadOnlySpan<Pixel> GetImageData(float seconds)
    {
        if (!runOnce)
        {
            runOnce = true;

            // Get the level select value, index into the layer 1 data etc
            var levelSelect = (uint)temp_levelSelect.Value;
            var layer1Data = _rom.ReadBytes(ReadKind.Rom, _addressTranslation.ToImage(0x05E000 + 3 * levelSelect), 3);
            var layer2Data = _rom.ReadBytes(ReadKind.Rom, _addressTranslation.ToImage(0x05E600 + 3 * levelSelect), 3);
            var spriteData = _rom.ReadBytes(ReadKind.Rom, _addressTranslation.ToImage(0x05EC00 + 2 * levelSelect), 2);

            uint layer0Address = _addressTranslation.ToImage((uint)((layer1Data[2] << 16) | (layer1Data[1] << 8) | layer1Data[0]));

            var headerData = _rom.ReadBytes(ReadKind.Rom, layer0Address, 5);

            var byte0 = headerData[0];  // BBBLLLLL  B-BG Palette, L-Number of screens -1
            var byte1 = headerData[1];  // CCC00000  C-Back area colour
            var byte2 = headerData[2];  // 3MMMSSSS  3-layer 3 priority, M-Music, S-Sprite GFX Setting
            var byte3 = headerData[3];  // TTPPPFFF  T - timer setting, P - Sprite Palette, F - FG Palette
            var byte4 = headerData[4];  // IIVVZZZZ  I - item memory setting, V - vertical scroll setting, Z - FG/BG GFX Setting

            bool layerDone = false;
            uint offset = 0;
            while (!layerDone)
            {
                var triple = _rom.ReadBytes(ReadKind.Rom, layer0Address + 5 + offset, 3);
                if (triple[0] == 0xFF)
                {
                    layerDone = true;
                    break;
                }
                // Check if Standard Object / Extended Object

                var t0 = triple[0];  // NBBYYYYY
                var t1 = triple[1];  // bbbbXXXX
                var t2 = triple[2];  // SSSSSSSS
                var t3 = 0;

                offset += 3;
                bool extended = false;
                if ((t0 & 0x60) == 0 && (t1 & 0xF0) == 0)
                {
                    extended = true;
                    // Extended or screen exit
                    if (t2 == 0)
                    {
                        // Screen Exit
                        t3 = _rom.ReadBytes(ReadKind.Rom, layer0Address + 5 + offset, 1)[0];
                        offset += 1;
                    }
                }

                var objectNumber = ((t0 & 0x60) >> 1) | (t1 >> 4);
                if (objectNumber == 0)
                {
                    objectNumber = t2;
                }

                var yPos = t0 & 0x1F;
                var xPos = t1 & 0x0F;
                var p0 = (t2 & 0xF0) >> 4;
                var p1 = t2 & 0x0F;

                if (extended)
                {
                    switch (objectNumber)
                    {
                        default:
                            _editorInterface.Log(LogType.Info, $"Unknown Extended Object {objectNumber:X2} @{xPos:X2},{yPos:X2}");
                            break;
                    }
                }
                else
                {
                    switch (objectNumber)
                    {
                        default:
                            _editorInterface.Log(LogType.Info, $"Unknown Object {objectNumber:X2} @{xPos:X2},{yPos:X2}");
                            break;
                        case 0x13:      // Ground ledge
                            _editorInterface.Log(LogType.Info, $"Ledge edge  @{xPos:X2},{yPos:X2} - Height {p0:X2} - Type {p1:X2}");
                            break;
                        case 0x21:      // Long ground ledge
                            _editorInterface.Log(LogType.Info, $"Long ground ledge  @{xPos:X2},{yPos:X2} - Length {t2:X2}");
                            break;
                    }
                }


            }


            var graphicData000_072 = _rom.ReadBytes(ReadKind.Rom, _addressTranslation.ToImage(0x0D8000), 920);   // TTTTTTTT YXPCCCTT  T-Tile, Y-Y flip, X-X flip, P-Palette, C-Colour, T-Top priority
        }

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
