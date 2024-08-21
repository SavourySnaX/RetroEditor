using System.IO;
using System.Security.Cryptography;
using System.Linq;

using RetroEditor.Plugins;
using System;
using System.Collections.Generic;

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

public static class LC_LZ2
{
    private static void DecompressCode(ref byte[] decompBuffer, ref ReadOnlySpan<byte> data, ref int offset, int l, int c)
    {
        var count=l;
        switch (c)
        {
            case 0: // Direct Copy
                {
                    for (int i = 0; i <= count; i++)
                    {
                        decompBuffer[offset++] = data[0];
                        data = data.Slice(1);
                    }
                }
                break;
            case 1: //Byte Fill
                {
                    var value = data[0];
                    data = data.Slice(1);
                    for (int i = 0; i <= count; i++)
                    {
                        decompBuffer[offset++] = value;
                    }
                }
                break;
            case 2: //Word Fill
                {
                    var v0 = data[0];
                    var v1 = data[1];
                    data = data.Slice(2);
                    bool oddeven=false;
                    for (int i=0;i<=count;i++)
                    {
                        if (!oddeven)
                            decompBuffer[offset++] = v0;
                        else
                            decompBuffer[offset++] = v1;
                        oddeven = !oddeven;
                    }
                }
                break;
            case 3: //Increasing Fill
                {
                    var value = data[0];
                    data = data.Slice(1);
                    for (int i = 0; i <= count; i++)
                    {
                        decompBuffer[offset++] = value++;
                    }
                }
                break;
            case 4: //Repeat
                {
                    var h = data[1];
                    var L = data[0];
                    var src = (h<<8) | L;
                    data = data.Slice(2);
                    for (int i = 0; i <= count; i++)
                    {
                        decompBuffer[offset++] = decompBuffer[src++];
                    }
                }
                break;
            case 7: //LongLength
                {
                    var nc = (l&0x1C)>>2;
                    var nl = (l&3)<<8;
                    nl |= data[0];
                    data = data.Slice(1);
                    DecompressCode(ref decompBuffer, ref data,ref offset,nl,nc);
                }
                break;
            
        }

    }

    public static int Decompress(ref byte[] toDecompressBuffer, ReadOnlySpan<byte> data)
    {
        //LC_LZ2
        var offset=0;

        while (data.Length>0)
        {
            var b = data[0];
            if (b==0xFF)
            {
                break;
            }
            data = data.Slice(1);
            var l = b&0x1F;
            var c = (b&0xE0)>>5;
            DecompressCode(ref toDecompressBuffer, ref data,ref offset,l,c);
        }
        return offset;
    }
}

public static class SMWAddresses    // Super Mario World Japan 1.0 (headerless)
{
    public const uint LevelDataLayer1 = 0x05E000;
    public const uint LevelDataLayer2 = 0x05E600;
    public const uint LevelDataSprites = 0x05EC00;
    public const uint TileData_000_072 = 0x0D8000;
    public const uint TileSet0Base_073 = 0x0D8B70;
    public const uint TileSet1Base_073 = 0x0DBC00;
    public const uint TileSet2Base_073 = 0x0DC800;
    public const uint TileSet3Base_073 = 0x0DD400;
    public const uint TileSet4Base_073 = 0x0DE300;
    public const uint TileSet0Base_100 = 0x0D8398;
    public const uint TileSet1Base_100 = 0x0DC068;
    public const uint TileSet2Base_100 = 0x0DCC68;
    public const uint TileSet3Base_100 = 0x0DD868;
    public const uint TileSet4Base_100 = 0x0DE768;
    public const uint TileData_107_110 = 0x0DC068;
    public const uint TileData_111_152 = 0x0D83D0;
    public const uint TileSet0Base_153 = 0x0D9028;
    public const uint TileSet1Base_153 = 0x0DC0B8;
    public const uint TileSet2Base_153 = 0x0DCCB8;
    public const uint TileSet3Base_153 = 0x0DD8B8;
    public const uint TileSet4Base_153 = 0x0DE7B8;
    public const uint TileData_16E_1C3 = 0x0D85E0;
    public const uint TileData_1C4_1C7 = 0x0D8890;
    public const uint TileData_1C8_1EB = 0x0D88B0;
    public const uint TileData_1EC_1EF = 0x0D89D0;
    public const uint TileData_1F0_1FF = 0x0D89F0;
    public const uint TileData_Map16BG = 0x0D9100;
    public const uint GFX00_31_LowByteTable = 0x00B933;
    public const uint GFX00_31_HighByteTable = 0x00B965;
    public const uint GFX00_31_BankByteTable = 0x00B997;
    public const uint GFX32 = 0x088000;
    public const uint GFX33 = 0x08BFC0;
    public const uint BGPaletteTable = 0x00B050;
    public const uint FGPaletteTable = 0x00B130;
    public const uint SpritePaletteTable = 0x00B2B8;
    public const uint PaletteRow4ToDTable = 0x00B1F0;
    public const uint PlayerPaletteTable = 0x00B268;
    public const uint Layer3PaletteTable = 0x00B110;
    public const uint BerryPaletteTable = 0x00B614;
    public const uint AnimatedPaletteEntryTable = 0x00B5AC;
    public const uint BackAreaColourTable = 0x00B040;
    public const uint ObjectGFXList = 0x00A8C9;
    public const uint SpriteGFXList = 0x00A861;

    public const uint LargeBush = 0x0DA6ED; // Definition of large bush layout
    public const uint MediumBush = 0x0DA747; // Definition of medium bush layout
}

public static class SomeConstants
{
    public const int DefaultLevel = 511;
}

ref struct SuperMarioWorldRomHelpers
{
    public uint Layer1Data => _layer1Address + 5;
    public uint Layer2Data => _layer2Address;
    public uint SpriteData => _spriteAddress + 1;
    public uint Layer1SnesAddress => _layer1SnesAddress;
    public uint Layer2SnesAddress => _layer2SnesAddress;
    public uint SpriteSnesAddress => _spriteSnesAddress;
    public bool Layer2IsImage => _layer2Image;
    public SMWLevelHeader Header => _header;
    public SMWSpriteHeader SpriteHeader => _spriteHeader;
    public bool Layer2ImagePage01 => _layer2ImagePage01;

    public SuperMarioWorldRomHelpers(IMemoryAccess rom, AddressTranslation addressTranslation, uint levelNumber)
    {
        _rom = rom;
        _addressTranslation = addressTranslation;

        var layer1Data = rom.ReadBytes(ReadKind.Rom, addressTranslation.ToImage(SMWAddresses.LevelDataLayer1 + 3 * levelNumber), 3);
        var layer2Data = _rom.ReadBytes(ReadKind.Rom, _addressTranslation.ToImage(SMWAddresses.LevelDataLayer2 + 3 * levelNumber), 3);
        var spriteData = _rom.ReadBytes(ReadKind.Rom, _addressTranslation.ToImage(SMWAddresses.LevelDataSprites + 2 * levelNumber), 2);

        _layer1SnesAddress = (uint)((layer1Data[2] << 16) | (layer1Data[1] << 8) | layer1Data[0]);
        _layer2SnesAddress = (uint)(0x0C << 16) | (uint)((layer2Data[1] << 8) | layer2Data[0]);
        _spriteSnesAddress = (uint)((0x07 << 16) | (spriteData[1] << 8) | spriteData[0]);


        _layer1Address = addressTranslation.ToImage(_layer1SnesAddress);
        // Todo - grab secondary level header information from $05F000	$05F200	$05F400	$05F600	 <- check against J rom
        // Todo - handle identifying layer 2 as Background, or layer data
        _layer2Image = layer2Data[2] == 0xFF;
        _layer2ImagePage01 = (_layer2SnesAddress & 0xFFFF) >= 0xE8FE;
        if (_layer2Image)
        {
            // Unclear if header present for this... 
            _layer2Address = addressTranslation.ToImage(_layer2SnesAddress);
        }
        else
        {
            _layer2Address = addressTranslation.ToImage(_layer2SnesAddress) + 5;
        }
        _spriteAddress = addressTranslation.ToImage(_spriteSnesAddress);

        var headerData = rom.ReadBytes(ReadKind.Rom, _layer1Address, 5);
        _header = new SMWLevelHeader(headerData);

        var spriteHeaderData = rom.ReadBytes(ReadKind.Rom, _spriteAddress, 1);
        _spriteHeader = new SMWSpriteHeader(spriteHeaderData);
    }

    private IMemoryAccess _rom;
    private AddressTranslation _addressTranslation;

    private uint _layer1SnesAddress;
    private uint _layer2SnesAddress;
    private uint _spriteSnesAddress;
    private uint _layer1Address;
    private uint _layer2Address;
    private bool _layer2Image;
    private bool _layer2ImagePage01;
    private uint _spriteAddress;
    private SMWLevelHeader _header;
    private SMWSpriteHeader _spriteHeader;

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
            menu.AddItem("Level", 
                (editorInterface,menuItem) => {
                    editorInterface.OpenUserWindow($"Level View", GetImage(editorInterface, rom));
                });
            menu.AddItem("GFX",
                (editorInterface,menuItem) => {
                    editorInterface.OpenUserWindow($"GFX View", GetGFXPageImage(editorInterface, rom));
                });
            menu.AddItem("VRAM",
                (editorInterface,menuItem) => {
                    editorInterface.OpenUserWindow($"VRAM View", GetVRAMImage(editorInterface, rom));
                });
            menu.AddItem("Palette",
                (editorInterface,menuItem) => {
                    editorInterface.OpenUserWindow($"Palette View", GetPaletteImage(editorInterface, rom));
                });
            menu.AddItem("Map 16",
                (editorInterface,menuItem) => {
                    editorInterface.OpenUserWindow($"Map16 View", GetMap16Image(editorInterface, rom));
                });
    }

    public SuperMarioWorldTestImage GetImage(IEditor editorInterface, IMemoryAccess rom)
    {
        return new SuperMarioWorldTestImage(editorInterface, rom);
    }

    public SuperMarioWorldGFXPageImage GetGFXPageImage(IEditor editorInterface, IMemoryAccess rom)
    {
        return new SuperMarioWorldGFXPageImage(editorInterface, rom);
    }
    
    public SuperMarioWorldVramImage GetVRAMImage(IEditor editorInterface, IMemoryAccess rom)
    {
        return new SuperMarioWorldVramImage(editorInterface, rom);
    }

    public SuperMarioWorldPaletteImage GetPaletteImage(IEditor editorInterface, IMemoryAccess rom)
    {
        return new SuperMarioWorldPaletteImage(editorInterface, rom);
    }
    
    public SuperMarioWorldMap16Image GetMap16Image(IEditor editorInterface, IMemoryAccess rom)
    {
        return new SuperMarioWorldMap16Image(editorInterface, rom);
    }

}

public struct SMWLevelHeader
{
    public byte BGPalette;
    public byte NumberOfScreens;
    public byte BackAreaColour;
    public bool Layer3Priority;
    public byte Music;
    public byte SpriteGFXSetting;
    public byte TimerSetting;
    public byte SpritePalette;
    public byte FGPalette;
    public byte ItemMemorySetting;
    public byte VerticalScrollSetting;
    public byte FGBGGFXSetting;

    public SMWLevelHeader(ReadOnlySpan<byte> data)
    {
        BGPalette = (byte)((data[0] & 0xE0) >> 5);
        NumberOfScreens = (byte)((data[0] & 0x1F) + 1);
        BackAreaColour = (byte)((data[1] & 0xE0) >> 5);
        Layer3Priority = (data[2] & 0x80) == 0x80;
        Music = (byte)((data[2] & 0x70) >> 4);
        SpriteGFXSetting = (byte)(data[2] & 0x0F);
        TimerSetting = (byte)((data[3] & 0xC0) >> 6);
        SpritePalette = (byte)((data[3] & 0x38) >> 3);
        FGPalette = (byte)(data[3] & 0x07);
        ItemMemorySetting = (byte)((data[4] & 0xC0) >> 6);
        VerticalScrollSetting = (byte)((data[4] & 0x30) >> 4);
        FGBGGFXSetting = (byte)(data[4] & 0x0F);
    }
}

public struct SMWSpriteHeader
{
    public bool SpriteBouyancyS;
    public bool SpriteBouyancyB;
    public byte SpriteMemory;   // upto 0x12

    public SMWSpriteHeader(ReadOnlySpan<byte> data)
    {
        SpriteBouyancyS = (data[0] & 0x80) == 0x80;
        SpriteBouyancyB = (data[0] & 0x40) == 0x40;
        SpriteMemory = (byte)(data[0] & 0x3F);
    }
}


struct SubTile
{
    public int tile;
    public int palette;
    public bool flipx;
    public bool flipy;
    public bool priority;

    public SubTile(int a, int b)
    {
        tile = a + ((b & 3) << 8);
        palette = (b & 0x1C) >> 2;
        flipx = (b & 0x40) == 0x40;
        flipy = (b & 0x80) == 0x80;
        priority = (b & 0x20) == 0x20;
    }
}

struct Tile16x16
{
    public SubTile TL, TR, BL, BR;

    public Tile16x16(ReadOnlySpan<byte> data)
    {
        TL = new SubTile(data[0], data[1]);
        BL = new SubTile(data[2], data[3]);
        TR = new SubTile(data[4], data[5]);
        BR = new SubTile(data[6], data[7]);
    }
}


class SMWMap16
{
    public bool Has(int index) => map16ToTile.ContainsKey(index);
    public Tile16x16 this[int index] => map16ToTile[index];
    private Dictionary<int, Tile16x16> map16ToTile = new Dictionary<int, Tile16x16>();

    private void SetUpTiles(int start, int end, uint Address, IMemoryAccess rom, AddressTranslation addressTranslation)
    {
        var data = rom.ReadBytes(ReadKind.Rom, addressTranslation.ToImage(Address), (uint)(8*(end-start+1)));
        for (int a=start;a<=end;a++)
        {
            var Test2D = data.Slice((a-start)*8, 8);
            map16ToTile[a] = new Tile16x16(Test2D);
        }
    }

    public SMWMap16(IMemoryAccess rom, AddressTranslation addressTranslation, SMWLevelHeader header)
    {
        var baseAddressTileSpecific073 = SMWAddresses.TileSet0Base_073;
        var baseAddressTileSpecific100 = SMWAddresses.TileSet0Base_100;
        var baseAddressTileSpecific153 = SMWAddresses.TileSet0Base_153;
        switch (header.FGBGGFXSetting)
        {
            default:
            case 0x00:                      // Normal 1
            case 0x07:                      // Normal 2
            case 0x0C:                      // Cloud/Forest
                break;
            case 0x01:                      // Castle 1
                baseAddressTileSpecific073 = SMWAddresses.TileSet1Base_073;
                baseAddressTileSpecific100 = SMWAddresses.TileSet1Base_100;
                baseAddressTileSpecific153 = SMWAddresses.TileSet1Base_153;
                break;
            case 0x02:                      // Rope 1
            case 0x06:                      // Rope 2
            case 0x08:                      // Rope 3
                baseAddressTileSpecific073 = SMWAddresses.TileSet2Base_073;
                baseAddressTileSpecific100 = SMWAddresses.TileSet2Base_100;
                baseAddressTileSpecific153 = SMWAddresses.TileSet2Base_153;
                break;
            case 0x03:                      // Underground 1
            case 0x09:                      // Underground 2
            case 0x0E:                      // Underground 3
            case 0x0A:                      // Switch Palace 2
            case 0x0B:                      // Castle 2
                baseAddressTileSpecific073 = SMWAddresses.TileSet3Base_073;
                baseAddressTileSpecific100 = SMWAddresses.TileSet3Base_100;
                baseAddressTileSpecific153 = SMWAddresses.TileSet3Base_153;
                break;
            case 0x04:                      // Switch Palace 1
            case 0x05:                      // Ghost House 1
            case 0x0D:                      // Ghost House 2
                baseAddressTileSpecific073 = SMWAddresses.TileSet4Base_073;
                baseAddressTileSpecific100 = SMWAddresses.TileSet4Base_100;
                baseAddressTileSpecific153 = SMWAddresses.TileSet4Base_153;
                break;
        }

        SetUpTiles(0x000,0x072,SMWAddresses.TileData_000_072,rom,addressTranslation);
        SetUpTiles(0x073,0x0FF,baseAddressTileSpecific073,rom,addressTranslation);
        SetUpTiles(0x100,0x106,baseAddressTileSpecific100,rom,addressTranslation);
        SetUpTiles(0x107,0x110,SMWAddresses.TileData_107_110,rom,addressTranslation);
        SetUpTiles(0x111,0x152,SMWAddresses.TileData_111_152,rom,addressTranslation);
        SetUpTiles(0x153,0x16D,baseAddressTileSpecific153,rom,addressTranslation);
        SetUpTiles(0x16E,0x1C3,SMWAddresses.TileData_16E_1C3,rom,addressTranslation);
        SetUpTiles(0x1C4,0x1C7,SMWAddresses.TileData_1C4_1C7,rom,addressTranslation);
        SetUpTiles(0x1C8,0x1EB,SMWAddresses.TileData_1C8_1EB,rom,addressTranslation);
        SetUpTiles(0x1EC,0x1EF,SMWAddresses.TileData_1EC_1EF,rom,addressTranslation);
        SetUpTiles(0x1F0,0x1FF,SMWAddresses.TileData_1F0_1FF,rom,addressTranslation);

        // Finally, setup the map16 BG data (stored in slots 512-1023)
        SetUpTiles(0x200,0x3FF,SMWAddresses.TileData_Map16BG,rom,addressTranslation);
    }


}

public class SuperMarioWorldTestImage : IImage, IUserWindow
{
    public uint Width => 16*16*32;

    public uint Height => 416;

    public float ScaleX => 1.0f;

    public float ScaleY => 1.0f;

    public float UpdateInterval => 1/60.0f;

    private IMemoryAccess _rom;
    private AddressTranslation _addressTranslation;
    private IWidgetRanged temp_levelSelect;
    private IWidgetLabel widgetLabel;
    private IEditor _editorInterface;

    private SuperMarioPalette _palette;
    private SMWMap16 _map16ToTile;


    public SuperMarioWorldTestImage(IEditor editorInterface, IMemoryAccess rom)
    {
        _rom = rom;
        _addressTranslation = new LoRom();
        _editorInterface = editorInterface;
    }

    public void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls)
    {
        widget.AddImageView(this);
        temp_levelSelect = widget.AddSlider("Level", SomeConstants.DefaultLevel, 0, 511, () => { runOnce = false; });
        widgetLabel = widget.AddLabel("");
    }

    private bool runOnce = false;

    enum StandardObject
    {
        WaterBlue = 0x01,
        InvisibleCoinBlocks = 0x02,
        InvisibleNoteBlocks = 0x03,
        InvisiblePowCoins = 0x04,
        Coins = 0x05,
        WalkThroughDirt = 0x06,
        WaterOtherColor = 0x07,
        NoteBlocks = 0x08,
        TurnBlocks = 0x09,
        CoinQuestionBlocks = 0x0A,
        ThrowBlocks = 0x0B,
        BlackPiranhaPlants = 0x0C,
        CementBlocks = 0x0D,
        BrownBlocks = 0x0E,
        VerticalPipes = 0x0F,
        HorizontalPipes = 0x10,
        BulletShooter = 0x11,
        Slopes = 0x12,
        LedgeEdges = 0x13,
        GroundLedge = 0x14,
        MidwayGoalPoint = 0x15,
        BlueCoins = 0x16,
        RopeOrClouds = 0x17,
        WaterSurceAnimated = 0x18,
        WaterSurfaceStatic = 0x19,
        LavaSurfaceAnimated = 0x1A,
        NetTopEdge = 0x1B,
        DonutBridge = 0x1C,
        NetBottomEdge = 0x1D,
        NetVerticalEdge = 0x1E,
        VerticalPipeOrBoneOrLog = 0x1F,
        HorizontalPipeOrBoneOrLog = 0x20,
        LongGroundLedge = 0x21,
        TilesetSpecificStart01 = 0x2E,
        TilesetSpecificStart02 = 0x2F,
        TilesetSpecificStart03 = 0x30,
        TilesetSpecificStart04 = 0x31,
        TilesetSpecificStart05 = 0x32,
        TilesetSpecificStart06 = 0x33,
        TilesetSpecificStart07 = 0x34,
        TilesetSpecificStart08 = 0x35,
        TilesetSpecificStart09 = 0x36,
        TilesetSpecificStart10 = 0x37,
        TilesetSpecificStart11 = 0x38,
        TilesetSpecificStart12 = 0x39,
        TilesetSpecificStart13 = 0x3A,
        TilesetSpecificStart14 = 0x3B,
        TilesetSpecificStart15 = 0x3C,
        TilesetSpecificStart16 = 0x3D,
        TilesetSpecificStart17 = 0x3E,
        TilesetSpecificStart18 = 0x3F,
    }

    enum ExtendedObject
    {
        ScreenExit = 0,
        ScreenJump = 1,
        Moon3Up = 0x18,
        Invisible1Up1 = 0x19,
        Invisible1Up2 = 0x1A,
        Invisible1Up3 = 0x1B,
        Invisible1Up4 = 0x1C,
        RedBerry = 0x1D,
        PinkBerry = 0x1E,
        GreenBerry = 0x1F,
        QBlockFlower = 0x30,
        QBlockFeather = 0x31,
        QBlockStar = 0x32,
        QBlockStar2 = 0x33,
        QBlockMultipleCoins = 0x34,
        QBlockKeyWingsBalloonShell = 0x35,
        QBlockYoshi = 0x36,
        QBlockShell1 = 0x37,
        QBlockShell2 = 0x38,
        TranslucentBlock = 0x40,
        YoshiCoin = 0x41,
        TopLeftSlope = 0x42,
        TopRightSlope = 0x43,
        PurpleTriangleLeft = 0x44,
        PurpleTriangleRight = 0x45,
        MidwayPointRope = 0x46,
        BigBush1 = 0x82,
        BigBush2 = 0x83,
        ArrowSign = 0x86,
    }

    enum SpriteObject
    {

        RedKoopa = 0x05,

    }


    Pixel[] pixels;

    void Draw16x16Tile(int tx,int ty, Pixel colour)
    {
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                int px = tx * 16 + x;
                int py = ty * 16 + y;
                if (px >= 0 && px < Width && py >= 0 && py < Height)
                {
                    pixels[py * Width + px] = colour;
                }
            }
        }
    }

    void DrawTiles(int tx,int ty,int tw,int th, Pixel colour)
    {
        tw++;
        th++;
        for (int y = 0; y < th; y++)
        {
            for (int x = 0; x < tw; x++)
            {
                Draw16x16Tile(tx + x, ty + y, colour);
            }
        }
    }


    void Draw8x8(int tx, int ty, int xo,int yo, SubTile tile, SuperMarioVRam vram)
    {
        var tileNum = tile.tile;
        var gfx = vram.Tile(tileNum);
        // compute tile position TL
        var tileX=tileNum%16;
        var tileY=tileNum/16;

        int ox = tx * 16 + 8 * xo;
        int oy = ty * 16 + 8 * yo;
        int g=0;
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                var px =ox + (tile.flipx ? 7 - x : x);
                var py =oy + (tile.flipy ? 7 - y : y);
                if (px >= 0 && px < Width && py >= 0 && py < Height)
                {
                    var c = gfx[g++];
                    if (c!=0)
                    {
                        pixels[py * Width + px] = _palette[tile.palette,c];
                    }
                }
            }
        }
    }

    void DrawGfxTile(int tx,int ty, int tile, SuperMarioVRam vram)
    {
        var tile16 = _map16ToTile[tile];

        // TODO Make vram just be an array of 8x8 tiles to make lookups simpler

        // Just draw the TL tile for now
        Draw8x8(tx, ty, 0, 0, tile16.TL, vram);
        Draw8x8(tx, ty, 1, 0, tile16.TR, vram);
        Draw8x8(tx, ty, 0, 1, tile16.BL, vram);
        Draw8x8(tx, ty, 1, 1, tile16.BR, vram);
    }

    void DrawGfxTilesYTopOther(int tx,int ty,int tw,int th, SuperMarioVRam vram, int topTile, int otherRows)
    {
        tw++;
        th++;
        for (int y = 0; y < th; y++)
        {
            for (int x = 0; x < tw; x++)
            {
                DrawGfxTile(tx + x, ty + y, topTile, vram);
            }
            topTile = otherRows;
        }
    }
    
    void DrawGfxTiles(int tx,int ty,int tw,int th, SuperMarioVRam vram, int leftTile, int middleTile, int rightTile)
    {
        tw++;
        th++;
        for (int y = 0; y < th; y++)
        {
            for (int x = 0; x < tw; x++)
            {
                if (x==0)
                    DrawGfxTile(tx + x, ty + y, leftTile, vram);
                else if (x==tw-1)
                    DrawGfxTile(tx + x, ty + y, rightTile, vram);
                else
                    DrawGfxTile(tx + x, ty + y, middleTile, vram);
            }
        }
    }


    void DrawGfxTilesFixedPattern(int tx, int ty, int tw, uint totalCount, SuperMarioVRam vram, uint snesAddress)
    {
        var data = _rom.ReadBytes(ReadKind.Rom, _addressTranslation.ToImage(snesAddress), totalCount);
        int o=0;
        while (true)
        {
            for (int x = 0; x < tw; x++)
            {
                DrawGfxTile(tx + x, ty, data[o++], vram);
                totalCount--;
                if (totalCount==0)
                    return;
            }
            ty++;
        }
    }



    public ReadOnlySpan<Pixel> GetImageData(float seconds)
    {
        if (!runOnce)
        {
            runOnce = true;

            pixels = new Pixel[Width * Height];

            // Get the level select value, index into the layer 1 data etc
            var levelSelect = (uint)temp_levelSelect.Value;
            var smwRom = new SuperMarioWorldRomHelpers(_rom, _addressTranslation, levelSelect);
            var smwLevelHeader = smwRom.Header;
            _palette = new SuperMarioPalette(_rom, smwLevelHeader);
            _map16ToTile = new SMWMap16(_rom, _addressTranslation, smwLevelHeader);
            var vram = new SuperMarioVRam(_rom, smwLevelHeader);

            widgetLabel.Name = $"Layer 1 : {smwRom.Layer1SnesAddress:X6} Layer 2 : {smwRom.Layer2SnesAddress:X6} Sprite : {smwRom.SpriteSnesAddress:X6}";

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = _palette.BackgroundColour;
            }

            if (smwRom.Layer2IsImage)
            {
                RenderLayer2Image(ref smwRom, smwLevelHeader, vram, smwRom.Layer2Data, smwRom.Layer2ImagePage01);
            }
            else
            {
                RenderObjectLayer(ref smwRom, smwLevelHeader, vram, smwRom.Layer2Data);
            }

            RenderObjectLayer(ref smwRom, smwLevelHeader, vram, smwRom.Layer1Data);

            RenderSpriteLayer(ref smwRom, smwLevelHeader, vram);
        }

        return pixels;
    }

    private void RenderLayer2Image(ref SuperMarioWorldRomHelpers smwRom, SMWLevelHeader smwLevelHeader, SuperMarioVRam vram, uint layerAddress, bool useUpperPage)
    {
        var decompBuffer = new byte [32768];
        var writeOffs = 0;
        // First off, decompress the layer 2 data, then splat it across the level
        while (true)
        {
            var bytes = _rom.ReadBytes(ReadKind.Rom, layerAddress, 2);
            layerAddress += 2;
            var b0 = bytes[0];
            var b1 = bytes[1];
            if (b0==b1 && b0==0xFF)
            {
                break;
            }
            var length = (b0 & 0x7F) + 1;
            if ((b0&0x80)==0)
            {
                // copy length bytes
                decompBuffer[writeOffs++] = bytes[1];
                length--;
                if (length>0)
                {
                    var data = _rom.ReadBytes(ReadKind.Rom, layerAddress, (uint)length);
                    for (int i=0;i<length;i++)
                    {
                        decompBuffer[writeOffs++] = data[i];
                    }
                }
                layerAddress += (uint)length;
            }
            else
            {
                // copy b1 length times
                for (int i=0;i<length;i++)
                {
                    decompBuffer[writeOffs++] = b1;
                }
            }
        }

        // The decompBuffer is data to directly index the map16 second half data.
        var offset = 512 + (useUpperPage ? 256 : 0);
        for (int a=0;a<Width/256;a++)
        {
            var wOffset=a*16;
            var decompBufferOffset = (a&1)*16*27;
            for (int y = 0; y < 27; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    var tile = decompBuffer[decompBufferOffset + y * 16 + x] + offset;
                    DrawGfxTile(wOffset+x, y, tile, vram);
                }
            }
        }

    }

    private void RenderObjectLayer(ref SuperMarioWorldRomHelpers smwRom, SMWLevelHeader smwLevelHeader, SuperMarioVRam vram,  uint layerAddress)
    {
        var screenOffsetNumber = 0;
        bool layerDone = false;
        uint offset = 0;
        while (!layerDone)
        {
            var triple = _rom.ReadBytes(ReadKind.Rom, layerAddress + offset, 3);
            if (triple[0] == 0xFF)
            {
                return;
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
                    t3 = _rom.ReadBytes(ReadKind.Rom, layerAddress + offset, 1)[0];
                    offset += 1;
                }
            }

            var objectNumber = ((t0 & 0x60) >> 1) | (t1 >> 4);
            if (objectNumber == 0)
            {
                objectNumber = t2;
            }

            if ((t0 & 0x80) != 0)
            {
                screenOffsetNumber++;
            }

            // TODO deal with vertical levels...

            var yPos = t0 & 0x1F;
            var xPos = screenOffsetNumber * 16 + (t1 & 0x0F);
            var p0 = (t2 & 0xF0) >> 4;
            var p1 = t2 & 0x0F;

            var screenNumber = t0 & 0x1F;

            if (extended)
            {
                switch ((ExtendedObject)objectNumber)
                {
                    case ExtendedObject.ScreenExit:
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(ExtendedObject)objectNumber} @{xPos:X2},{yPos:X2} - {t3:X2}");
                        break;
                    case ExtendedObject.ScreenJump:
                        screenOffsetNumber = screenNumber;
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(ExtendedObject)objectNumber} @{xPos:X2},{yPos:X2} - Screen {screenNumber:X2}");
                        break;
                    case ExtendedObject.Moon3Up:
                    case ExtendedObject.Invisible1Up1:
                    case ExtendedObject.Invisible1Up2:
                    case ExtendedObject.Invisible1Up3:
                    case ExtendedObject.Invisible1Up4:
                    case ExtendedObject.QBlockFlower:
                    case ExtendedObject.QBlockFeather:
                    case ExtendedObject.QBlockStar:
                    case ExtendedObject.QBlockStar2:
                    case ExtendedObject.QBlockMultipleCoins:
                    case ExtendedObject.QBlockKeyWingsBalloonShell:
                    case ExtendedObject.QBlockShell1:
                    case ExtendedObject.QBlockShell2:
                    case ExtendedObject.TranslucentBlock:
                    case ExtendedObject.TopLeftSlope:
                    case ExtendedObject.TopRightSlope:
                    case ExtendedObject.PurpleTriangleLeft:
                    case ExtendedObject.PurpleTriangleRight:
                    case ExtendedObject.MidwayPointRope:
                    case ExtendedObject.ArrowSign:
                        Draw16x16Tile(xPos, yPos, new Pixel(128, 128, 128, 255));
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(ExtendedObject)objectNumber} @{xPos:X2},{yPos:X2}");
                        break;
                    case ExtendedObject.QBlockYoshi:
                        DrawGfxTile(xPos, yPos, 0x126, vram);
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(ExtendedObject)objectNumber} @{xPos:X2},{yPos:X2}");
                        break;
                    case ExtendedObject.BigBush1:
                        // See table referenced by code at DA106 - For now, I've just done things by hand, but perhaps we should automate this
                        DrawGfxTilesFixedPattern(xPos, yPos, 9, 45, vram, SMWAddresses.LargeBush);
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(ExtendedObject)objectNumber} @{xPos:X2},{yPos:X2}");
                        break;
                    case ExtendedObject.BigBush2:
                        DrawGfxTilesFixedPattern(xPos, yPos, 6, 24, vram, SMWAddresses.MediumBush);
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(ExtendedObject)objectNumber} @{xPos:X2},{yPos:X2}");
                        break;
                    case ExtendedObject.YoshiCoin:
                        DrawGfxTilesYTopOther(xPos, yPos, 0, 1, vram, 0x2D, 0x2E);
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(ExtendedObject)objectNumber} @{xPos:X2},{yPos:X2}");
                        break;
                    case ExtendedObject.RedBerry:
                    case ExtendedObject.PinkBerry:
                    case ExtendedObject.GreenBerry:
                        DrawGfxTile(xPos, yPos, (objectNumber - (int)ExtendedObject.RedBerry) + 0x45, vram);
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(ExtendedObject)objectNumber} @{xPos:X2},{yPos:X2}");
                        break;
                    default:
                        Draw16x16Tile(xPos, yPos, new Pixel(32, 32, 32, 255));

                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} Unknown Extended Object @{xPos:X2},{yPos:X2}");
                        break;
                }
            }
            else
            {
                switch ((StandardObject)objectNumber)
                {
                    case StandardObject.GroundLedge:
                        DrawGfxTilesYTopOther(xPos, yPos, p1, p0, vram, 0x100, 0x3F);
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(StandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Height {p0:X2} - Width {p1:X2}");
                        break;
                    case StandardObject.WaterBlue:
                    case StandardObject.InvisibleCoinBlocks:
                    case StandardObject.InvisibleNoteBlocks:
                    case StandardObject.InvisiblePowCoins:
                    case StandardObject.WalkThroughDirt:
                    case StandardObject.WaterOtherColor:
                    case StandardObject.NoteBlocks:
                    case StandardObject.TurnBlocks:
                    case StandardObject.CoinQuestionBlocks:
                    case StandardObject.ThrowBlocks:
                    case StandardObject.BlackPiranhaPlants:
                    case StandardObject.CementBlocks:
                    case StandardObject.BrownBlocks:
                    case StandardObject.BlueCoins:
                    case StandardObject.WaterSurceAnimated:
                    case StandardObject.WaterSurfaceStatic:
                    case StandardObject.LavaSurfaceAnimated:
                    case StandardObject.NetTopEdge:
                    case StandardObject.NetBottomEdge:
                        DrawTiles(xPos, yPos, p1, p0, new Pixel(0, 255, 255, 255));
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(StandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Height {p0:X2} - Width {p1:X2}");
                        break;
                    case StandardObject.Coins:
                        DrawGfxTilesYTopOther(xPos, yPos, p1, p0, vram, 0x2B, 0x2B);
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(StandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Height {p0:X2} - Width {p1:X2}");
                        break;
                    case StandardObject.VerticalPipes:
                    case StandardObject.LedgeEdges:
                    case StandardObject.MidwayGoalPoint:
                    case StandardObject.NetVerticalEdge:
                        DrawTiles(xPos, yPos, 1, p0, new Pixel(255, 0, 255, 255));
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(StandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Height {p0:X2} - Type {p1:X2}");
                        break;
                    case StandardObject.Slopes:
                        if (p1 == 5)
                        {
                            // NOTE PADS next hieght with 3Fs on the left
                            DrawGfxTile(xPos, yPos, 0x182, vram);
                            DrawGfxTile(xPos + 1, yPos, 0x187, vram);
                            DrawGfxTile(xPos + 2, yPos, 0x18C, vram);
                            DrawGfxTile(xPos + 3, yPos, 0x191, vram);
                            yPos += 1;
                            DrawGfxTile(xPos, yPos, 0x1E6, vram);
                            DrawGfxTile(xPos + 1, yPos, 0x1E6, vram);
                            DrawGfxTile(xPos + 2, yPos, 0x1DB, vram);
                            DrawGfxTile(xPos + 3, yPos, 0x1DC, vram);
                        }
                        else
                            DrawTiles(xPos, yPos, 1, p0, new Pixel(255, 0, 255, 255));
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(StandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Height {p0:X2} - Type {p1:X2}");
                        break;

                    case StandardObject.HorizontalPipes:
                        switch (p0)
                        {
                            case 0: // Ends on left
                            case 1: // exit enabled
                                DrawGfxTile(xPos, yPos, 0x13B, vram);
                                DrawGfxTile(xPos, yPos+1, 0x13C, vram);
                                for (int a=1;a<=p1;a++)
                                {
                                    DrawGfxTile(xPos+a, yPos, 0x13D, vram);
                                    DrawGfxTile(xPos+a, yPos+1, 0x13E, vram);
                                }
                                break;
                            case 2: // Ends on right
                            case 3: // exit enabled
                                for (int a=0;a<p1;a++)
                                {
                                    DrawGfxTile(xPos+a, yPos, 0x13D, vram);
                                    DrawGfxTile(xPos+a, yPos+1, 0x13E, vram);
                                }
                                DrawGfxTile(xPos+p1, yPos, 0x13B, vram);
                                DrawGfxTile(xPos+p1, yPos+1, 0x13C, vram);
                                break;
                        }
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(StandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Type {p0:X2} - Width {p1:X2}");
                        break;
                    case StandardObject.RopeOrClouds:
                        DrawTiles(xPos, yPos, p1, 1, new Pixel(255, 255, 0, 255));
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(StandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Type {p0:X2} - Width {p1:X2}");
                        break;
                    case StandardObject.BulletShooter:
                    case StandardObject.VerticalPipeOrBoneOrLog:
                        DrawTiles(xPos, yPos, 1, p0, new Pixel(0, 0, 255, 255));
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(StandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Height {p0:X2}");
                        break;
                    case StandardObject.LongGroundLedge:      // Long ground ledge
                        DrawGfxTilesYTopOther(xPos, yPos, t2, 1, vram, 0x100, 0x3F);
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(StandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Length {t2:X2}");
                        break;
                    case StandardObject.DonutBridge:
                    case StandardObject.HorizontalPipeOrBoneOrLog:
                        DrawTiles(xPos, yPos, p1, 1, new Pixel(255, 0, 0, 255));
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(StandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Width {p1:X2}");
                        break;
                    case StandardObject.TilesetSpecificStart01:
                    case StandardObject.TilesetSpecificStart02:
                    case StandardObject.TilesetSpecificStart03:
                    case StandardObject.TilesetSpecificStart04:
                    case StandardObject.TilesetSpecificStart05:
                    case StandardObject.TilesetSpecificStart06:
                    case StandardObject.TilesetSpecificStart07:
                    case StandardObject.TilesetSpecificStart08:
                    case StandardObject.TilesetSpecificStart09:
                    case StandardObject.TilesetSpecificStart10:
                    case StandardObject.TilesetSpecificStart11:
                    case StandardObject.TilesetSpecificStart12:
                    case StandardObject.TilesetSpecificStart15:
                    case StandardObject.TilesetSpecificStart16:
                    case StandardObject.TilesetSpecificStart17:
                        Draw16x16Tile(xPos, yPos, new Pixel(128, 0, 0, 255));
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(StandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Special {t2:X2}");
                        break;
                    case StandardObject.TilesetSpecificStart13:
                        // Left facing diagonal ledge (see right, just different codes basically)
                        {
                            DrawGfxTile(xPos, yPos, 0x1AA, vram);
                            DrawGfxTile(xPos + 1, yPos, 0x0A1, vram);
                            yPos += 1;
                            xPos -= 1;
                            DrawGfxTile(xPos, yPos, 0x1AA, vram);
                            DrawGfxTile(xPos + 1, yPos, 0x1E2, vram);
                            DrawGfxTile(xPos + 2, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 3, yPos, 0x0A6, vram);
                            yPos += 1;
                            xPos -= 1;
                            DrawGfxTile(xPos, yPos, 0x1AA, vram);
                            DrawGfxTile(xPos + 1, yPos, 0x1E2, vram);
                            DrawGfxTile(xPos + 2, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 3, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 4, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 5, yPos, 0x0A6, vram);
                            yPos += 1;
                            xPos -= 1;
                            DrawGfxTile(xPos, yPos, 0x1AA, vram);
                            DrawGfxTile(xPos + 1, yPos, 0x1E2, vram);
                            DrawGfxTile(xPos + 2, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 3, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 4, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 5, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 6, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 7, yPos, 0x0A6, vram);
                            yPos += 1;
                            DrawGfxTile(xPos, yPos, 0x1F7, vram);
                            DrawGfxTile(xPos + 1, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 2, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 3, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 4, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 5, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 6, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 7, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 8, yPos, 0x0A6, vram);
                            yPos += 1;
                            xPos += 1;
                            DrawGfxTile(xPos, yPos, 0x0A3, vram);
                            DrawGfxTile(xPos + 1, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 2, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 3, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 4, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 5, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 6, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 7, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 8, yPos, 0x0A6, vram);
                            yPos += 1;
                            xPos += 1;
                            DrawGfxTile(xPos, yPos, 0x0A3, vram);
                            DrawGfxTile(xPos + 1, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 2, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 3, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 4, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 5, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 6, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 7, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 8, yPos, 0x0A6, vram);


                        }
                        break;
                    case StandardObject.TilesetSpecificStart14:
                        // Right facing diagonal ledge (
                        //
                        // A                         0x0AF 0x1AF
                        // B                   0x0A9 0x03F 0x1E4 0x1AF
                        // C             0x0A9 0x03F 0x03F 0x03F 0x1E4 0x1AF
                        // D       0x0A9 0x03F 0x03F 0x03F 0x03F 0x03F 0x1F9
                        // E 0x0A9 0x03F 0x03F 0x03F 0x03F 0x03F 0x0AC            <- when P0>0 append rows like this
                        //
                        // p1 is done first, then p0 using longest length computed in p1
                        // e.g. 0 0 would produce row A then a row 0x0A9 0x3F 0x1AF
                        /// 02
                        {
                            DrawGfxTile(xPos, yPos, 0x0AF, vram);
                            DrawGfxTile(xPos + 1, yPos, 0x1AF, vram);
                            yPos += 1;
                            xPos -= 1;
                            DrawGfxTile(xPos, yPos, 0x0A9, vram);
                            DrawGfxTile(xPos + 1, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 2, yPos, 0x1E4, vram);
                            DrawGfxTile(xPos + 3, yPos, 0x1AF, vram);
                            yPos += 1;
                            xPos -= 1;
                            DrawGfxTile(xPos, yPos, 0x0A9, vram);
                            DrawGfxTile(xPos + 1, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 2, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 3, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 4, yPos, 0x1E4, vram);
                            DrawGfxTile(xPos + 5, yPos, 0x1AF, vram);
                            yPos += 1;
                            xPos -= 1;
                            DrawGfxTile(xPos, yPos, 0x0A9, vram);
                            DrawGfxTile(xPos + 1, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 2, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 3, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 4, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 5, yPos, 0x03F, vram);
                            DrawGfxTile(xPos + 6, yPos, 0x1F9, vram);
                        }

                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(StandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Special {t2:X2}");
                        break;
                    case StandardObject.TilesetSpecificStart18:
                        // grass  - p1 width, p0 style (0,1,2)
                        switch (p0)
                        {
                            default:
                            case 0:
                                DrawGfxTiles(xPos, yPos, p1, 0, vram, 0x73, 0x74, 0x79);
                                break;
                            case 1:
                                DrawGfxTiles(xPos, yPos, p1, 0, vram, 0x7A, 0x7B, 0x80);
                                break;
                            case 2:
                                DrawGfxTiles(xPos, yPos, p1, 0, vram, 0x85, 0x86, 0x87);
                                break;
                        }
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(StandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Special {t2:X2}");
                        break;
                    default:
                        Draw16x16Tile(xPos, yPos, new Pixel(128, 128, 0, 255));
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} Unknown Object @{xPos:X2},{yPos:X2}");
                        break;
                }
            }
        }
    }

    private void RenderSpriteLayer(ref SuperMarioWorldRomHelpers smwRom, SMWLevelHeader smwLevelHeader, SuperMarioVRam vram)
    {
        var spriteData = smwRom.SpriteData;
        bool layerDone = false;
        uint offset = 0;
        _editorInterface.Log(LogType.Info, $"Sprites:");
        while (!layerDone)
        {
            var triple = _rom.ReadBytes(ReadKind.Rom, spriteData + offset, 3);
            if (triple[0] == 0xFF)
            {
                return;
            }
            // Check if Standard Object / Extended Object

            var t0 = triple[0];  // yyyyEESY
            var t1 = triple[1];  // XXXXssss
            var t2 = triple[2];  // NNNNNNNN

            offset += 3;

            var spriteY = ((t0 & 1) << 4) | (t0 >> 4);
            var spriteX = (t1 & 0xF0) >> 4;
            var screenNumber = ((t0 & 0x02) << 3) | (t1 & 0x0F);
            var spriteId = t2;
            var spriteExtra = (t0 & 0x0C) >> 2;
            
            var yPos = spriteY;
            var xPos = screenNumber * 16 + spriteX;

            switch ((SpriteObject)spriteId)
            {
                default:
                case SpriteObject.RedKoopa:
                    Draw16x16Tile(xPos, yPos, new Pixel(255, 255, 128, 255));
                    _editorInterface.Log(LogType.Info, $"{screenNumber:X2} | {spriteId:X2} {(SpriteObject)spriteId} @{xPos:X2},{yPos:X2} - Extra {spriteExtra:X2}");
                    break;
            }

        }
    }


    public void OnClose()
    {
    }
}

public class SuperMarioWorldGFXPageImage : IImage, IUserWindow
{
    public uint Width => 256;

    public uint Height => 256;

    public float ScaleX => 2.0f;

    public float ScaleY => 2.0f;

    public float UpdateInterval => 1/60.0f;

    private IMemoryAccess _rom;
    private AddressTranslation _addressTranslation;
    private IWidgetRanged temp_pageSelect;
    private IWidgetLabel temp_pageInfo;
    private IEditor _editorInterface;

    public SuperMarioWorldGFXPageImage(IEditor editorInterface, IMemoryAccess rom)
    {
        _rom = rom;
        _addressTranslation = new LoRom();
        _editorInterface = editorInterface;

        for (int i=0;i<=0x33;i++)
        {
            GFXPageKind[i] = SNESTileKind.Tile_3bpp;
        }
        GFXPageKind[0x27] = SNESTileKind.Tile_3bppMode7;
        for (int i=0x28;i<=0x2B;i++)
        {
            GFXPageKind[i] = SNESTileKind.Tile_2bpp;
        }
        GFXPageKind[0x2F] = SNESTileKind.Tile_2bpp;
        GFXPageKind[0x32] = SNESTileKind.Tile_4bpp;
    }

    public void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls)
    {
        widget.AddImageView(this);
        temp_pageSelect = widget.AddSlider("GFX Page", 0x00, 0, 0x33, () => { runOnce = false; });
        temp_pageInfo = widget.AddLabel("");
    }

    private bool runOnce = false;

    Pixel[] pixels;

    enum SNESTileKind
    {
        Tile_2bpp,
        Tile_3bpp,
        Tile_4bpp,
        Tile_3bppMode7,
    }

    SNESTileKind[] GFXPageKind = new SNESTileKind[52];

    public ReadOnlySpan<Pixel> GetImageData(float seconds)
    {
        if (!runOnce)
        {
            runOnce = true;

            pixels = new Pixel[Width * Height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Pixel(0, 0, 0, 255);
            }

            uint gfxPtr;
            if (temp_pageSelect.Value < 0x32)
            {
                var lo = _rom.ReadBytes(ReadKind.Rom, _addressTranslation.ToImage(SMWAddresses.GFX00_31_LowByteTable + (uint)temp_pageSelect.Value), 1)[0];
                var hi = _rom.ReadBytes(ReadKind.Rom, _addressTranslation.ToImage(SMWAddresses.GFX00_31_HighByteTable + (uint)temp_pageSelect.Value), 1)[0];
                var bk = _rom.ReadBytes(ReadKind.Rom, _addressTranslation.ToImage(SMWAddresses.GFX00_31_BankByteTable + (uint)temp_pageSelect.Value), 1)[0];

                gfxPtr = _addressTranslation.ToImage((uint)((bk << 16) | (hi << 8) | lo));
            }
            else if (temp_pageSelect.Value==0x32)
            {
                gfxPtr=_addressTranslation.ToImage(SMWAddresses.GFX32);
            }
            else
            {
                gfxPtr=_addressTranslation.ToImage(SMWAddresses.GFX33);
            }


            var decomp = new byte[32768];
            var GFX = new ReadOnlySpan<byte>(decomp);

            var size = LC_LZ2.Decompress(ref decomp, _rom.ReadBytes(ReadKind.Rom, gfxPtr, 32768));  // Overread but should be ok
            temp_pageInfo.Name = $"GFX Page {temp_pageSelect.Value:X2} {GFXPageKind[temp_pageSelect.Value]} Decoded Size {size}";

            var rasteriseAs = GFXPageKind[temp_pageSelect.Value];
            var tileSize = 32;
            switch (rasteriseAs)
            {
                case SNESTileKind.Tile_2bpp:
                    tileSize=16;
                    break;
                case SNESTileKind.Tile_3bpp:
                case SNESTileKind.Tile_3bppMode7:
                    tileSize=24;
                    break;
                case SNESTileKind.Tile_4bpp:
                default:
                    tileSize=32;
                    break;
            }
            var tileCount = size / tileSize;
            for (int i = 0; i < tileCount; i++)
            {
                var tx = (i % 256) % 16;
                var ty = (i % 256) / 16;
                var tzx = ((i / 256) % 2) * Width/2;
                var tzy = ((i / 256) / 2) * (Height/2) * Width;
                ReadOnlySpan<byte> tile;
                tile = GFX.Slice(i * tileSize, tileSize);
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        switch (rasteriseAs)
                        {
                            case SNESTileKind.Tile_2bpp:
                                {
                                    var bp0 = tile[y * 2 + 0];
                                    var bp1 = tile[y * 2 + 1];
                                    var bit = 7 - x;
                                    var colour = ((bp0 >> bit) & 1) | (((bp1 >> bit) & 1) << 1);
                                    pixels[tzx+tzy+(ty * 8 + y) * Width + tx * 8 + x] = new Pixel((byte)(colour * 64), (byte)(colour * 64), (byte)(colour * 64), 255);
                                }
                                break;
                            case SNESTileKind.Tile_3bpp:
                                {
                                    var bp0 = tile[y * 2 + 0];
                                    var bp1 = tile[y * 2 + 1];
                                    var bp2 = tile[16 + y];
                                    var bit = 7 - x;
                                    var colour = ((bp0 >> bit) & 1) | (((bp1 >> bit) & 1) << 1) | (((bp2 >> bit) & 1) << 2);
                                    pixels[tzx+tzy+(ty * 8 + y) * Width + tx * 8 + x] = new Pixel((byte)(colour * 32), (byte)(colour * 32), (byte)(colour * 32), 255);
                                }
                                break;
                            case SNESTileKind.Tile_4bpp:
                                {
                                    var bp0 = tile[y * 2 + 0];
                                    var bp1 = tile[y * 2 + 1];
                                    var bp2 = tile[16 + y * 2 + 0];
                                    var bp3 = tile[16 + y * 2 + 1];
                                    var bit = 7 - x;
                                    var colour = ((bp0 >> bit) & 1) | (((bp1 >> bit) & 1) << 1) | (((bp2 >> bit) & 1) << 2) | (((bp3 >> bit) & 1) << 3);
                                    pixels[tzx+tzy+(ty * 8 + y) * Width + tx * 8 + x] = new Pixel((byte)(colour * 16), (byte)(colour * 16), (byte)(colour * 16), 255);
                                }
                                break;
                            case SNESTileKind.Tile_3bppMode7:
                                {
                                    var row = tile[y * 3 + 0] << 16 | tile[y * 3 + 1] << 8 | tile[y * 3 + 2];
                                    var colour = (byte)((row >> (21 - x * 3)) & 0x07);
                                    pixels[tzx+tzy+(ty * 8 + y) * Width + tx * 8 + x] = new Pixel((byte)(colour * 32), (byte)(colour * 32), (byte)(colour * 32), 255);
                                }
                                break;  

                        }
                    }
                }
            }


        }

        return pixels;
    }

    public void OnClose()
    {
    }
}

public class SuperMarioPalette
{
    Pixel[,] _palette;
    Pixel _backgroundColour;

    Pixel SNESToPixel(ushort c)
    {
        var r = ((c & 0x1F) << 3) | ((c & 1) != 0 ? 7 : 0);
        var g = (((c >> 5) & 0x1F) << 3) | ((c & 0x20) != 0 ? 7 : 0);
        var b = (((c >> 10) & 0x1F) << 3) | ((c & 0x400) != 0 ? 7 : 0);
        return new Pixel((byte)r,(byte)g,(byte)b,255);
    }

    public SuperMarioPalette(IMemoryAccess rom, SMWLevelHeader header)
    {
        _palette = new Pixel[16, 16];  // Perhaps we should have platform colour constructors e.g. SNESToPixel, etc?
        var addressTranslation = new LoRom();

        for (int i=0;i<16;i++)
        {
            _palette[i, 0] = SNESToPixel(0x0000);
            _palette[i, 1] = SNESToPixel(0x7FDD);
        }

        // BG Palette x = $00B0B0 + (#$18 * x). (Palette 0,2 to 0,7 and 1,2 to 1,7)       

        var bgPalette = rom.ReadBytes(ReadKind.Rom, addressTranslation.ToImage(SMWAddresses.BGPaletteTable + 0x18u * header.BGPalette), 0x18);
        var fgPalette = rom.ReadBytes(ReadKind.Rom, addressTranslation.ToImage(SMWAddresses.FGPaletteTable + 0x18u * header.FGPalette), 0x18);
        var spPalette = rom.ReadBytes(ReadKind.Rom, addressTranslation.ToImage(SMWAddresses.SpritePaletteTable + 0x18u * header.SpritePalette), 0x18);
        for (int i = 2; i < 8; i++)
        {
            var p = i - 2;
            var c = rom.FetchMachineOrder16(p * 2, bgPalette);
            _palette[0, i] = SNESToPixel(c);
            c = rom.FetchMachineOrder16(0x0C + p * 2, bgPalette);
            _palette[1, i] = SNESToPixel(c);
            c = rom.FetchMachineOrder16(p * 2, fgPalette);
            _palette[2, i] = SNESToPixel(c);
            c = rom.FetchMachineOrder16(0x0C + p * 2, fgPalette);
            _palette[3, i] = SNESToPixel(c);
            c = rom.FetchMachineOrder16(p * 2, spPalette);
            _palette[14, i] = SNESToPixel(c);
            c = rom.FetchMachineOrder16(0x0C + p * 2, spPalette);
            _palette[14, i] = SNESToPixel(c);
        }
        for (uint row = 4; row < 14; row++)
        {
            var rowPalette = rom.ReadBytes(ReadKind.Rom, addressTranslation.ToImage(SMWAddresses.PaletteRow4ToDTable + 0x0Cu * (row - 4u)), 0x0C);
            for (int i = 2; i < 8; i++)
            {
                var p = i - 2;
                var c = rom.FetchMachineOrder16(p * 2, rowPalette);
                _palette[row, i] = SNESToPixel(c);
            }
        }
        var palette = rom.ReadBytes(ReadKind.Rom, addressTranslation.ToImage(SMWAddresses.PlayerPaletteTable + 0u/*Mario*/* 0x14u), 0x14);
        for (int i = 6; i < 16; i++)
        {
            var p = i - 6;
            var c = rom.FetchMachineOrder16(p * 2, palette);
            _palette[8, i] = SNESToPixel(c);
        }
        var layer3Palette = rom.ReadBytes(ReadKind.Rom, addressTranslation.ToImage(SMWAddresses.Layer3PaletteTable), 0x20);
        for (int i = 8; i < 16; i++)
        {
            var p = i - 8;
            var c = rom.FetchMachineOrder16(p * 2, layer3Palette);
            _palette[0, i] = SNESToPixel(c);
            c = rom.FetchMachineOrder16(0x10 + p * 2, layer3Palette);
            _palette[1, i] = SNESToPixel(c);
        }
        for (uint row = 2; row < 5; row++)
        {
            var rowPalette = rom.ReadBytes(ReadKind.Rom, addressTranslation.ToImage(SMWAddresses.BerryPaletteTable + 0x0Eu * (row - 2u)), 0x0E);
            for (int i = 9; i < 16; i++)
            {
                var p = i - 9;
                var c = rom.FetchMachineOrder16(p * 2, rowPalette);
                _palette[row, i] = SNESToPixel(c);
            }
        }
        var animatedPalette = rom.ReadBytes(ReadKind.Rom, addressTranslation.ToImage(SMWAddresses.AnimatedPaletteEntryTable), 0x10);
        // For now, just use first colour
        var animCol = rom.FetchMachineOrder16(0, animatedPalette);
        _palette[6, 4] = SNESToPixel(animCol);

        var bgColour = rom.ReadBytes(ReadKind.Rom, addressTranslation.ToImage(SMWAddresses.BackAreaColourTable + 0x02u * header.BackAreaColour), 2);
        var bgSnesColour = rom.FetchMachineOrder16(0, bgColour);
        _backgroundColour = SNESToPixel(bgSnesColour);
    }

    public Pixel this[int palette,int colour]
    {
        get
        {
            return _palette[palette,colour];
        }
    }

    public Pixel BackgroundColour => _backgroundColour;
}

public class SuperMarioVRam 
{
    public ReadOnlySpan<byte> Tile(int tile)
    {
        return tileCache[tile];
    }

    private IMemoryAccess _rom;
    private AddressTranslation _addressTranslation;
    private SNESTileKind[] GFXPageKind = new SNESTileKind[52];

    private byte[] decomp = new byte[32768];

    private byte[][] tileCache = new byte[16*16*6][];

    // TODO needs information from level for vram layout
    public SuperMarioVRam(IMemoryAccess rom, SMWLevelHeader header)
    {
        _rom = rom;
        _addressTranslation = new LoRom();

        for (int i=0;i<=0x33;i++)
        {
            GFXPageKind[i] = SNESTileKind.Tile_3bpp;
        }
        GFXPageKind[0x27] = SNESTileKind.Tile_3bppMode7;
        for (int i=0x28;i<=0x2B;i++)
        {
            GFXPageKind[i] = SNESTileKind.Tile_2bpp;
        }
        GFXPageKind[0x2F] = SNESTileKind.Tile_2bpp;
        GFXPageKind[0x32] = SNESTileKind.Tile_4bpp;

        // Need to fill VRAM with the GFX data that is expected for a level
        for (int i=0;i<16*16*6;i++)
        {
            tileCache[i] = new byte[8 * 8];
        }

        // Fetch correct gfx pages from object and sprite tables
        var objectPages = rom.ReadBytes(ReadKind.Rom, _addressTranslation.ToImage(SMWAddresses.ObjectGFXList + header.FGBGGFXSetting*0x04u), 0x4);
        var spritePages = rom.ReadBytes(ReadKind.Rom, _addressTranslation.ToImage(SMWAddresses.SpriteGFXList + header.SpriteGFXSetting*0x04u), 0x4);

        // Step 1, try and setup the first 128x128 of VRAM with the GFX data for the level
        // Fetch GFX Page 0x14 data 16x8 tiles

        RasterisePage(objectPages[0], 0x000, 0, 16 * 4);
        /// TODO Figure out animated tiles here - 16*4
        RasterisePage(objectPages[1], 0x080, 0, 16 * 8);
        RasterisePage(objectPages[2], 0x100, 0, 16 * 8);
        RasterisePage(objectPages[3], 0x180, 0, 16 * 8);
        // BLANKPAGE - SKIPPED
        // BLANKPAGE - SKIPPED
        RasterisePage(spritePages[0], 0x400, 0, 16 * 8);
        RasterisePage(spritePages[1], 0x480, 0, 16 * 8);
        RasterisePage(spritePages[2], 0x500, 0, 16 * 8);
        RasterisePage(spritePages[3], 0x580, 0, 16 * 8);


        //Table5B96B - animation kind , 0 constant, 1 triggered, 2 tileset specific
        //Table5B97D - 0 blue P switch, 1 silver P Switch, 2 is on/off
        //Table5B98B - which anim set to use (indexed by tileset - only if anim kind == 2)
        //Table5B999 - WRAM frame indexes for each anim
        //Table5Ba39 - remainder of 5B999 table

        //var animatedTileData = rom.ReadBytes(ReadKind.Rom, _addressTranslation.ToImage(SMWAddresses.AnimatedTileData), 0x100);

        // Step 1, Setup by hand... (as need to determine how to figure this out from level data)
        // Start with Coin, because that is easy :
        CopyAnimFrame(0x54, (12)*16+12, 0, 4);
        CopyAnimFrame(0x6C, (12)*16+12, 0, 4);
        // QBlock
        CopyAnimFrame(0x60, (12)*16+0, 0, 4);


    }

    enum SNESTileKind
    {
        Tile_2bpp,
        Tile_3bpp,
        Tile_4bpp,
        Tile_3bppMode7,
    }

    public ReadOnlySpan<byte> FetchGFXPage(uint pageNumber)
    {
        var lo = _rom.ReadBytes(ReadKind.Rom, _addressTranslation.ToImage(SMWAddresses.GFX00_31_LowByteTable + pageNumber), 1)[0];
        var hi = _rom.ReadBytes(ReadKind.Rom, _addressTranslation.ToImage(SMWAddresses.GFX00_31_HighByteTable + pageNumber), 1)[0];
        var bk = _rom.ReadBytes(ReadKind.Rom, _addressTranslation.ToImage(SMWAddresses.GFX00_31_BankByteTable + pageNumber), 1)[0];
        var gfxPtr = _addressTranslation.ToImage((uint)((bk << 16) | (hi << 8) | lo));

        var GFX = new ReadOnlySpan<byte>(decomp);

        LC_LZ2.Decompress(ref decomp, _rom.ReadBytes(ReadKind.Rom, gfxPtr, 32768));  // Overread but should be ok

        return GFX;
    }

    private void CopyAnimFrame(uint destTile, uint srcTile, uint frame, uint cnt)
    {
        var gfxPtr=_addressTranslation.ToImage(SMWAddresses.GFX33);
        var GFX = new ReadOnlySpan<byte>(decomp);
        LC_LZ2.Decompress(ref decomp, _rom.ReadBytes(ReadKind.Rom, gfxPtr, 32768));  // Overread but should be ok
        var tileSize = 24;
        srcTile += 16 * frame;
        for (int i = 0; i < cnt; i++)
        {
            var pixels = tileCache[destTile++];
            ReadOnlySpan<byte> tile = GFX.Slice((int)(srcTile++ * tileSize), tileSize);
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    var bp0 = tile[y * 2 + 0];
                    var bp1 = tile[y * 2 + 1];
                    var bp2 = tile[16 + y];
                    var bit = 7 - x;
                    var colour = ((bp0 >> bit) & 1) | (((bp1 >> bit) & 1) << 1) | (((bp2 >> bit) & 1) << 2);
                    pixels[y * 8 + x] = (byte)colour;
                }
            }
        }
    }

    private void RasterisePage(uint page, uint offset, int tileOffset, uint tileCount)
    {
        var GFX = FetchGFXPage(page);
        var rasteriseAs = GFXPageKind[page];
        var tileSize = 32;
        switch (rasteriseAs)
        {
            case SNESTileKind.Tile_2bpp:
                tileSize = 16;
                break;
            case SNESTileKind.Tile_3bpp:
            case SNESTileKind.Tile_3bppMode7:
                tileSize = 24;
                break;
            case SNESTileKind.Tile_4bpp:
            default:
                tileSize = 32;
                break;
        }
        for (int i = tileOffset; i < tileOffset+tileCount; i++)
        {
            var tx = i % 16;
            var ty = i / 16;
            ReadOnlySpan<byte> tile;
            tile = GFX.Slice(i * tileSize, tileSize);

            var pixels = tileCache[offset++];

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    switch (rasteriseAs)
                    {
                        case SNESTileKind.Tile_2bpp:
                            {
                                var bp0 = tile[y * 2 + 0];
                                var bp1 = tile[y * 2 + 1];
                                var bit = 7 - x;
                                var colour = ((bp0 >> bit) & 1) | (((bp1 >> bit) & 1) << 1);
                                pixels[y * 8 + x] = (byte)colour;
                            }
                            break;
                        case SNESTileKind.Tile_3bpp:
                            {
                                var bp0 = tile[y * 2 + 0];
                                var bp1 = tile[y * 2 + 1];
                                var bp2 = tile[16 + y];
                                var bit = 7 - x;
                                var colour = ((bp0 >> bit) & 1) | (((bp1 >> bit) & 1) << 1) | (((bp2 >> bit) & 1) << 2);
                                pixels[y * 8 + x] = (byte)colour;
                            }
                            break;
                        case SNESTileKind.Tile_4bpp:
                            {
                                var bp0 = tile[y * 2 + 0];
                                var bp1 = tile[y * 2 + 1];
                                var bp2 = tile[16 + y * 2 + 0];
                                var bp3 = tile[16 + y * 2 + 1];
                                var bit = 7 - x;
                                var colour = ((bp0 >> bit) & 1) | (((bp1 >> bit) & 1) << 1) | (((bp2 >> bit) & 1) << 2) | (((bp3 >> bit) & 1) << 3);
                                pixels[y * 8 + x] = (byte)colour;
                            }
                            break;
                        case SNESTileKind.Tile_3bppMode7:
                            {
                                var row = tile[y * 3 + 0] << 16 | tile[y * 3 + 1] << 8 | tile[y * 3 + 2];
                                var colour = (byte)((row >> (21 - x * 3)) & 0x07);
                                pixels[y * 8 + x] = (byte)colour;
                            }
                            break;

                    }
                }
            }

            // Berry Colour Fix
            if (page==0x17 && (i==0||i==1|i==16|i==17))
            {
                for (int a=0;a<64;a++)
                {
                    pixels[a]|=0x08;
                }
            }

        }
    }


}




public class SuperMarioWorldVramImage : IImage, IUserWindow
{
    public uint Width => 128*2;

    public uint Height => 128*3;

    public float ScaleX => 2.0f;

    public float ScaleY => 2.0f;

    public float UpdateInterval => 1/60.0f;

    private const uint NumberOfTiles = 16 * 16 * 6;

    private Pixel[] pixels;

    private IMemoryAccess _rom;
    private AddressTranslation _addressTranslation;
    private IWidgetRanged temp_levelSelect;
    private bool runOnce = false;

    public SuperMarioWorldVramImage(IEditor editorInterface, IMemoryAccess rom)
    {
        _rom=rom;
        _addressTranslation=new LoRom();
    }

    public void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls)
    {
        widget.AddImageView(this);
        temp_levelSelect = widget.AddSlider("Level", SomeConstants.DefaultLevel, 0, 511, () => { runOnce = false; });
    }

    public ReadOnlySpan<Pixel> GetImageData(float seconds)
    {
        if (!runOnce)
        {
            runOnce = true;
            var levelSelect = (uint)temp_levelSelect.Value;
            var smwRom = new SuperMarioWorldRomHelpers(_rom, _addressTranslation, levelSelect);
            var smwLevelHeader = smwRom.Header;
            var smwVRam = new SuperMarioVRam(_rom, smwLevelHeader);
            pixels = new Pixel[Width * Height];

            for (int tiles = 0; tiles < NumberOfTiles; tiles++)
            {
                var gfx = smwVRam.Tile(tiles);
                int g = 0;
                var tx = tiles % 16;
                var ty = (tiles % (NumberOfTiles/2)) / 16;
                var tz = (tiles / (NumberOfTiles/2)) * 128;
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        var colour = gfx[g++];
                        pixels[(ty * 8 + y) * Width + tx * 8 + x + tz] = new Pixel((byte)(colour * 16), (byte)(colour * 16), (byte)(colour * 16), 255);
                    }
                }
            }
        }

        return pixels;
    }

    public void OnClose()
    {
    }
}

public class SuperMarioWorldPaletteImage : IImage, IUserWindow
{
    public uint Width => 256;

    public uint Height => 256;

    public float ScaleX => 1.0f;

    public float ScaleY => 1.0f;

    public float UpdateInterval => 1/60.0f;

    private Pixel[] pixels;

    private IMemoryAccess _rom;
    private AddressTranslation _addressTranslation;
    private IWidgetRanged temp_levelSelect;
    private bool runOnce = false;

    public SuperMarioWorldPaletteImage(IEditor editorInterface, IMemoryAccess rom)
    {
        _rom=rom;
        _addressTranslation=new LoRom();
    }

    public void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls)
    {
        widget.AddImageView(this);
        temp_levelSelect = widget.AddSlider("Level",SomeConstants.DefaultLevel, 0, 511, () => { runOnce = false; });
    }

    public ReadOnlySpan<Pixel> GetImageData(float seconds)
    {
        if (!runOnce)
        {
            runOnce = true;
            var levelSelect = (uint)temp_levelSelect.Value;
            var smwRom = new SuperMarioWorldRomHelpers(_rom, _addressTranslation, levelSelect);

            var smwLevelHeader = smwRom.Header;
            var smwColours = new SuperMarioPalette(_rom, smwLevelHeader);
            pixels = new Pixel[Width * Height];

            for (int py = 0; py < 16; py++)
            {
                for (int px = 0; px < 16; px++)
                {
                    var c = smwColours[py, px];
                    for (int y = 0; y < 16; y++)
                    {
                        for (int x = 0; x < 16; x++)
                        {
                            pixels[(py * 16 + y) * Width + px * 16 + x] = c;
                        }
                    }
                }
            }
        }
        return pixels;
    }

    public void OnClose()
    {
    }
}

public class SuperMarioWorldMap16Image : IImage, IUserWindow
{
    public uint Width => 256;

    public uint Height => 256*2;

    public float ScaleX => 1.5f;

    public float ScaleY => 1.5f;

    public float UpdateInterval => 1/60.0f;

    private Pixel[] pixels;

    private IMemoryAccess _rom;
    private AddressTranslation _addressTranslation;
    private IWidgetRanged temp_levelSelect;
    private IWidgetCheckable temp_ShowBGMap;
    private bool runOnce = false;

    public SuperMarioWorldMap16Image(IEditor editorInterface, IMemoryAccess rom)
    {
        _rom=rom;
        _addressTranslation=new LoRom();
    }

    public void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls)
    {
        widget.AddImageView(this);
        temp_levelSelect = widget.AddSlider("Level", SomeConstants.DefaultLevel, 0, 511, () => { runOnce = false; });
        temp_ShowBGMap = widget.AddCheckbox("Show BG Map", false, () => { runOnce = false; });
    }

    private const uint NumberOfTiles = 0x200;

    void Draw8x8(int tx, int ty, int xo,int yo, SubTile tile, SuperMarioVRam vram, SuperMarioPalette palette)
    {
        var tileNum = tile.tile;
        var gfx = vram.Tile(tileNum);
        // compute tile position TL
        var tileX=tileNum%16;
        var tileY=tileNum/16;

        int ox = tx * 16 + 8 * xo;
        int oy = ty * 16 + 8 * yo;
        int g=0;
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                var px =ox + (tile.flipx ? 7 - x : x);
                var py =oy + (tile.flipy ? 7 - y : y);
                if (px >= 0 && px < Width && py >= 0 && py < Height)
                {
                    var c = gfx[g++];
                    if (c!=0)
                    {
                        pixels[py * Width + px] = palette[tile.palette,c];
                    }
                }
            }
        }
    }

    public ReadOnlySpan<Pixel> GetImageData(float seconds)
    {
        if (!runOnce)
        {
            runOnce = true;
            var levelSelect = (uint)temp_levelSelect.Value;
            var smwRom = new SuperMarioWorldRomHelpers(_rom, _addressTranslation, levelSelect);
            var smwLevelHeader = smwRom.Header;
            var smwVRam = new SuperMarioVRam(_rom, smwLevelHeader);
            var map16 = new SMWMap16(_rom, _addressTranslation, smwLevelHeader);
            var palette = new SuperMarioPalette(_rom, smwLevelHeader);
            pixels = new Pixel[Width * Height];

            int tilesOffset = temp_ShowBGMap.Checked ? 0x200 : 0;
            for (int tiles = 0; tiles < NumberOfTiles; tiles++)
            {
                var tx = tiles % 16;
                var ty = tiles / 16;

                var offset = tiles + tilesOffset;

                // Fetch map16 tile and render
                if (map16.Has(offset))
                {
                    var map16Tile = map16[offset];

                    Draw8x8(tx, ty, 0, 0, map16Tile.TL, smwVRam, palette);
                    Draw8x8(tx, ty, 1, 0, map16Tile.TR, smwVRam, palette);
                    Draw8x8(tx, ty, 0, 1, map16Tile.BL, smwVRam, palette);
                    Draw8x8(tx, ty, 1, 1, map16Tile.BR, smwVRam, palette);
                }
            }
        }

        return pixels;
    }

    public void OnClose()
    {
    }
}

