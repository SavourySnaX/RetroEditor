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
            menu.AddItem("Level", 
                (editorInterface,menuItem) => {
                    editorInterface.OpenUserWindow($"Level View", GetImage(editorInterface, rom));
                });
            menu.AddItem("Map16",
                (editorInterface,menuItem) => {
                    editorInterface.OpenUserWindow($"Map16 View", GetMap16Image(editorInterface, rom));
                });
    }

    public SuperMarioWorldTestImage GetImage(IEditor editorInterface, IMemoryAccess rom)
    {
        return new SuperMarioWorldTestImage(editorInterface, rom);
    }

    public SuperMarioWorldMap16Image GetMap16Image(IEditor editorInterface, IMemoryAccess rom)
    {
        return new SuperMarioWorldMap16Image(editorInterface, rom);
    }
}

public class SuperMarioWorldTestImage : IImage, IUserWindow
{
    public uint Width => 2048;

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
        temp_levelSelect = widget.AddSlider("Level", 0xC7, 0, 511, () => { runOnce = false; });
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
        WaterSurceAnimated=0x18,
        WaterSurfaceStatic=0x19,
        LavaSurfaceAnimated=0x1A,
        NetTopEdge=0x1B,
        DonutBridge=0x1C,
        NetBottomEdge=0x1D,
        NetVerticalEdge=0x1E,
        VerticalPipeOrBoneOrLog=0x1F,
        HorizontalPipeOrBoneOrLog=0x20,
        LongGroundLedge = 0x21,
        TilesetSpecificStart01 = 0x2E,
        TilesetSpecificStart02 = 0x2F,
        TilesetSpecificStart03 =0x30,
        TilesetSpecificStart04=0x31,
        TilesetSpecificStart05=0x32,
        TilesetSpecificStart06=0x33,
        TilesetSpecificStart07=0x34,
        TilesetSpecificStart08=0x35,
        TilesetSpecificStart09=0x36,
        TilesetSpecificStart10=0x37,
        TilesetSpecificStart11=0x38,
        TilesetSpecificStart12=0x39,
        TilesetSpecificStart13=0x3A,
        TilesetSpecificStart14=0x3B,
        TilesetSpecificStart15=0x3C,
        TilesetSpecificStart16=0x3D,
        TilesetSpecificStart17=0x3E,
        TilesetSpecificStart18=0x3F,
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
        RedBerry=0x1D,
        QBlockFlower=0x30,
        QBlockFeather=0x31,
        QBlockStar=0x32,
        QBlockStar2=0x33,
        QBlockMultipleCoins=0x34,
        QBlockKeyWingsBalloonShell=0x35,
        QBlockYoshi=0x36,
        QBlockShell1=0x37,
        QBlockShell2=0x38,
        TranslucentBlock=0x40,
        YoshiCoin=0x41,
        TopLeftSlope=0x42,
        TopRightSlope=0x43,
        PurpleTriangleLeft=0x44,
        PurpleTriangleRight=0x45,
        MidwayPointRope=0x46,
        BigBush1=0x82,
        BigBush2=0x83,
        ArrowSign=0x86,
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

    public ReadOnlySpan<Pixel> GetImageData(float seconds)
    {
        if (!runOnce)
        {
            runOnce = true;

            var screenOffsetNumber = 0;

            pixels = new Pixel[Width * Height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Pixel(0, 0, 0, 255);
            }

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

                var newScreen = false;
                if ((t0 & 0x80) != 0)
                {
                    screenOffsetNumber++;
                    newScreen = true;
                }

                // TODO deal with vertical levels...

                var yPos = t0 & 0x1F;
                var xPos = screenOffsetNumber*16 + (t1 & 0x0F);
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
                        case ExtendedObject.RedBerry:
                        case ExtendedObject.QBlockFlower:
                        case ExtendedObject.QBlockFeather:
                        case ExtendedObject.QBlockStar:
                        case ExtendedObject.QBlockStar2:
                        case ExtendedObject.QBlockMultipleCoins:
                        case ExtendedObject.QBlockKeyWingsBalloonShell:
                        case ExtendedObject.QBlockYoshi:
                        case ExtendedObject.QBlockShell1:
                        case ExtendedObject.QBlockShell2:
                        case ExtendedObject.TranslucentBlock:
                        case ExtendedObject.YoshiCoin:
                        case ExtendedObject.TopLeftSlope:
                        case ExtendedObject.TopRightSlope:
                        case ExtendedObject.PurpleTriangleLeft:
                        case ExtendedObject.PurpleTriangleRight:
                        case ExtendedObject.MidwayPointRope:
                        case ExtendedObject.BigBush1:
                        case ExtendedObject.BigBush2:
                        case ExtendedObject.ArrowSign:
                            Draw16x16Tile(xPos, yPos, new Pixel(128, 128, 128, 255));
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
                        case StandardObject.WaterBlue:
                        case StandardObject.InvisibleCoinBlocks:
                        case StandardObject.InvisibleNoteBlocks:
                        case StandardObject.InvisiblePowCoins:
                        case StandardObject.Coins:
                        case StandardObject.WalkThroughDirt:
                        case StandardObject.WaterOtherColor:
                        case StandardObject.NoteBlocks:
                        case StandardObject.TurnBlocks:
                        case StandardObject.CoinQuestionBlocks:
                        case StandardObject.ThrowBlocks:
                        case StandardObject.BlackPiranhaPlants:
                        case StandardObject.CementBlocks:
                        case StandardObject.BrownBlocks:
                        case StandardObject.GroundLedge:
                        case StandardObject.BlueCoins:
                        case StandardObject.WaterSurceAnimated:
                        case StandardObject.WaterSurfaceStatic:
                        case StandardObject.LavaSurfaceAnimated:
                        case StandardObject.NetTopEdge:
                        case StandardObject.NetBottomEdge:
                            DrawTiles(xPos, yPos, p1, p0, new Pixel(0, 255, 255, 255));
                            _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(StandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Height {p0:X2} - Width {p1:X2}"); 
                            break;
                        case StandardObject.VerticalPipes:
                        case StandardObject.Slopes:
                        case StandardObject.LedgeEdges:
                        case StandardObject.MidwayGoalPoint:
                        case StandardObject.NetVerticalEdge:
                            DrawTiles(xPos, yPos, 1, p0, new Pixel(255, 0, 255, 255));
                            _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(StandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Height {p0:X2} - Type {p1:X2}"); 
                            break;
                        case StandardObject.HorizontalPipes:
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
                            DrawTiles(xPos, yPos, t2, 1, new Pixel(0, 255, 0, 255));
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
                        case StandardObject.TilesetSpecificStart13:
                        case StandardObject.TilesetSpecificStart14:
                        case StandardObject.TilesetSpecificStart15:
                        case StandardObject.TilesetSpecificStart16:
                        case StandardObject.TilesetSpecificStart17:
                        case StandardObject.TilesetSpecificStart18:
                            Draw16x16Tile(xPos, yPos, new Pixel(128, 0, 0, 255));
                            _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(StandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Special {t2:X2}");
                            break;
                        default:
                            Draw16x16Tile(xPos, yPos, new Pixel(128, 128, 0, 255));
                            _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} Unknown Object @{xPos:X2},{yPos:X2}");
                            break;
                    }
                }


            }


            var graphicData000_072 = _rom.ReadBytes(ReadKind.Rom, _addressTranslation.ToImage(0x0D8000), 920);   // TTTTTTTT YXPCCCTT  T-Tile, Y-Y flip, X-X flip, P-Palette, C-Colour, T-Top priority
        }

        return pixels;
    }

    public void OnClose()
    {
    }
}

public class SuperMarioWorldMap16Image : IImage, IUserWindow
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

    public SuperMarioWorldMap16Image(IEditor editorInterface, IMemoryAccess rom)
    {
        _rom = rom;
        _addressTranslation = new LoRom();
        _editorInterface = editorInterface;
    }

    public void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls)
    {
        widget.AddImageView(this);
        temp_levelSelect = widget.AddSlider("Level", 0xC7, 0, 511, () => { runOnce = false; });
    }

    private bool runOnce = false;

    Pixel[] pixels;

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

            var graphicData000_072 = _rom.ReadBytes(ReadKind.Rom, _addressTranslation.ToImage(0x0D8000), 920);   // TTTTTTTT YXPCCCTT  T-Tile, Y-Y flip, X-X flip, P-Palette, C-Colour, T-Top priority
        }

        return pixels;
    }

    public void OnClose()
    {
    }
}

