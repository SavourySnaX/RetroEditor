using System;
using RetroEditor.Plugins;

using RetroEditorPlugin_SuperMarioWorld;

public class SuperMarioWorldLevelViewImage : IImage, IUserWindow
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


    public SuperMarioWorldLevelViewImage(IEditor editorInterface, IMemoryAccess rom)
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

        SMWRenderHelpers.DrawGfxTile(tx, ty, tile16, vram, ref pixels, Width, Height, _palette); 

        // TODO Make vram just be an array of 8x8 tiles to make lookups simpler

        // Just draw the TL tile for now
        Draw8x8(tx, ty, 0, 0, tile16.TL, vram);
        Draw8x8(tx, ty, 1, 0, tile16.TR, vram);
        Draw8x8(tx, ty, 0, 1, tile16.BL, vram);
        Draw8x8(tx, ty, 1, 1, tile16.BR, vram);
    }

    void DrawGfxTilesYTopOther(int tx,int ty,int tw,int th, SuperMarioVRam vram, int topTile, int otherRows)
    {
        DrawGfx9Tile(tx, ty, tw, th, vram, new int[] { topTile, topTile, topTile, otherRows, otherRows, otherRows, otherRows, otherRows, otherRows });
    }

    void DrawGfx9Tile(int tx,int ty,int tw,int th, SuperMarioVRam vram, int[] tiles)
    {
        int t0,t1,t2;
        tw++;
        th++;
        for (int y = 0; y < th; y++)
        {
            if (y==0)
            {
                t0=tiles[0];
                t1=tiles[1];
                t2=tiles[2];
            }
            else if (y==th-1)
            {
                t0=tiles[6];
                t1=tiles[7];
                t2=tiles[8];
            }
            else
            {
                t0=tiles[3];
                t1=tiles[4];
                t2=tiles[5];
            }
            for (int x = 0; x < tw; x++)
            {
                if (x==0)
                    DrawGfxTile(tx + x, ty + y, t0, vram);
                else if (x==tw-1)
                    DrawGfxTile(tx + x, ty + y, t2, vram);
                else
                    DrawGfxTile(tx + x, ty + y, t1, vram);
            }
        }
    }

    void DrawGfxTiles(int tx,int ty,int tw,int th, SuperMarioVRam vram, int leftTile, int middleTile, int rightTile)
    {
        DrawGfx9Tile(tx, ty, tw, th, vram, new int[] { leftTile, middleTile, rightTile, leftTile, middleTile, rightTile, leftTile, middleTile, rightTile });
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

            widgetLabel.Name = $"Layer 1 : {smwRom.Layer1SnesAddress:X6} Layer 2 : {smwRom.Layer2SnesAddress:X6} Sprite : {smwRom.SpriteSnesAddress:X6} - {smwLevelHeader.GetTileset()}";

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

    private void RenderObjectLayer(ref SuperMarioWorldRomHelpers smwRom, SMWLevelHeader smwLevelHeader, SuperMarioVRam vram, uint layerAddress)
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
                switch ((EExtendedObject)objectNumber)
                {
                    case EExtendedObject.ScreenExit:
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EExtendedObject)objectNumber} @{xPos:X2},{yPos:X2} - {t3:X2}");
                        break;
                    case EExtendedObject.ScreenJump:
                        screenOffsetNumber = screenNumber;
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EExtendedObject)objectNumber} @{xPos:X2},{yPos:X2} - Screen {screenNumber:X2}");
                        break;
                    case EExtendedObject.Moon3Up:
                    case EExtendedObject.Invisible1Up1:
                    case EExtendedObject.Invisible1Up2:
                    case EExtendedObject.Invisible1Up3:
                    case EExtendedObject.Invisible1Up4:
                    case EExtendedObject.QBlockFlower:
                    case EExtendedObject.QBlockFeather:
                    case EExtendedObject.QBlockStar:
                    case EExtendedObject.QBlockStar2:
                    case EExtendedObject.QBlockMultipleCoins:
                    case EExtendedObject.QBlockKeyWingsBalloonShell:
                    case EExtendedObject.QBlockShell1:
                    case EExtendedObject.QBlockShell2:
                    case EExtendedObject.TranslucentBlock:
                    case EExtendedObject.TopLeftSlope:
                    case EExtendedObject.TopRightSlope:
                    case EExtendedObject.PurpleTriangleLeft:
                    case EExtendedObject.PurpleTriangleRight:
                    case EExtendedObject.MidwayPointRope:
                    case EExtendedObject.ArrowSign:
                        Draw16x16Tile(xPos, yPos, new Pixel(128, 128, 128, 255));
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EExtendedObject)objectNumber} @{xPos:X2},{yPos:X2}");
                        break;
                    case EExtendedObject.QBlockYoshi:
                        DrawGfxTile(xPos, yPos, 0x126, vram);
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EExtendedObject)objectNumber} @{xPos:X2},{yPos:X2}");
                        break;
                    case EExtendedObject.BigBush1:
                        // See table referenced by code at DA106 - For now, I've just done things by hand, but perhaps we should automate this
                        DrawGfxTilesFixedPattern(xPos, yPos, 9, 45, vram, SMWAddresses.LargeBush);
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EExtendedObject)objectNumber} @{xPos:X2},{yPos:X2}");
                        break;
                    case EExtendedObject.BigBush2:
                        DrawGfxTilesFixedPattern(xPos, yPos, 6, 24, vram, SMWAddresses.MediumBush);
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EExtendedObject)objectNumber} @{xPos:X2},{yPos:X2}");
                        break;
                    case EExtendedObject.YoshiCoin:
                        DrawGfxTilesYTopOther(xPos, yPos, 0, 1, vram, 0x2D, 0x2E);
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EExtendedObject)objectNumber} @{xPos:X2},{yPos:X2}");
                        break;
                    case EExtendedObject.RedBerry:
                    case EExtendedObject.PinkBerry:
                    case EExtendedObject.GreenBerry:
                        DrawGfxTile(xPos, yPos, (objectNumber - (int)EExtendedObject.RedBerry) + 0x45, vram);
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EExtendedObject)objectNumber} @{xPos:X2},{yPos:X2}");
                        break;
                    default:
                        Draw16x16Tile(xPos, yPos, new Pixel(32, 32, 32, 255));

                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} Unknown Extended Object @{xPos:X2},{yPos:X2}");
                        break;
                }
            }
            else
            {
                switch ((EStandardObject)objectNumber)
                {
                    case EStandardObject.GroundLedge:
                        DrawGfxTilesYTopOther(xPos, yPos, p1, p0, vram, 0x100, 0x3F);
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EStandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Height {p0:X2} - Width {p1:X2}");
                        break;
                    case EStandardObject.WaterBlue:
                    case EStandardObject.InvisibleCoinBlocks:
                    case EStandardObject.InvisibleNoteBlocks:
                    case EStandardObject.InvisiblePowCoins:
                    case EStandardObject.WaterOtherColor:
                    case EStandardObject.NoteBlocks:
                    case EStandardObject.TurnBlocks:
                    case EStandardObject.CoinQuestionBlocks:
                    case EStandardObject.ThrowBlocks:
                    case EStandardObject.BlackPiranhaPlants:
                    case EStandardObject.CementBlocks:
                    case EStandardObject.BrownBlocks:
                    case EStandardObject.BlueCoins:
                    case EStandardObject.WaterSurceAnimated:
                    case EStandardObject.WaterSurfaceStatic:
                    case EStandardObject.LavaSurfaceAnimated:
                    case EStandardObject.NetTopEdge:
                    case EStandardObject.NetBottomEdge:
                        DrawTiles(xPos, yPos, p1, p0, new Pixel(0, 255, 255, 255));
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EStandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Height {p0:X2} - Width {p1:X2}");
                        break;
                    case EStandardObject.WalkThroughDirt:
                        DrawGfxTiles(xPos, yPos, p1, p0, vram, 0x3F, 0x3F, 0x3F);
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EStandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Height {p0:X2} - Width {p1:X2}");
                        break;
                    case EStandardObject.Coins:
                        DrawGfxTilesYTopOther(xPos, yPos, p1, p0, vram, 0x2B, 0x2B);
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EStandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Height {p0:X2} - Width {p1:X2}");
                        break;
                    case EStandardObject.VerticalPipes:
                    case EStandardObject.NetVerticalEdge:
                        DrawTiles(xPos, yPos, 1, p0, new Pixel(255, 0, 255, 255));
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EStandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Height {p0:X2} - Type {p1:X2}");
                        break;
                    case EStandardObject.LedgeEdges:
                        switch (p1)
                        {
                            case 0xB:
                                DrawGfx9Tile(xPos, yPos, 0, p0 + 1, vram, new int[] { 0x145, 0x145, 0x145, 0x14B, 0x14B, 0x14B, 0x1E2, 0x1E2, 0x1E2 });
                                break;
                            case 0xD:
                                DrawGfx9Tile(xPos, yPos, 0, p0 + 1, vram, new int[] { 0x148, 0x148, 0x148, 0x14C, 0x14C, 0x14C, 0x1E4, 0x1E4, 0x1E4 });
                                break;
                            default:
                                DrawTiles(xPos, yPos, 1, p0, new Pixel(255, 0, 255, 255));
                                break;
                        }
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EStandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Height {p0:X2} - Type {p1:X2}");
                        break;
                    case EStandardObject.MidwayGoalPoint:
                        if (p1 == 1)
                        {
                            DrawGfxTiles(xPos, yPos + 0, 2, 0, vram, 0x39, 0x25, 0x3C);
                            for (int a = 1; a < p0; a++)
                            {
                                DrawGfxTiles(xPos, yPos + a, 2, 0, vram, 0x3A, 0x25, 0x3D);
                            }
                            DrawGfxTiles(xPos, yPos + p0, 2, 0, vram, 0x3B, 0x25, 0x3E);
                        }
                        else
                        {
                            DrawTiles(xPos, yPos, 1, p0, new Pixel(255, 0, 255, 255));
                        }
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EStandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Height {p0:X2} - Type {p1:X2}");
                        break;
                    case EStandardObject.Slopes:
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
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EStandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Height {p0:X2} - Type {p1:X2}");
                        break;

                    case EStandardObject.HorizontalPipes:
                        switch (p0)
                        {
                            case 0: // Ends on left
                            case 1: // exit enabled
                                DrawGfxTile(xPos, yPos, 0x13B, vram);
                                DrawGfxTile(xPos, yPos + 1, 0x13C, vram);
                                for (int a = 1; a <= p1; a++)
                                {
                                    DrawGfxTile(xPos + a, yPos, 0x13D, vram);
                                    DrawGfxTile(xPos + a, yPos + 1, 0x13E, vram);
                                }
                                break;
                            case 2: // Ends on right
                            case 3: // exit enabled
                                for (int a = 0; a < p1; a++)
                                {
                                    DrawGfxTile(xPos + a, yPos, 0x13D, vram);
                                    DrawGfxTile(xPos + a, yPos + 1, 0x13E, vram);
                                }
                                DrawGfxTile(xPos + p1, yPos, 0x13B, vram);
                                DrawGfxTile(xPos + p1, yPos + 1, 0x13C, vram);
                                break;
                        }
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EStandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Type {p0:X2} - Width {p1:X2}");
                        break;
                    case EStandardObject.RopeOrClouds:
                        DrawTiles(xPos, yPos, p1, 1, new Pixel(255, 255, 0, 255));
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EStandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Type {p0:X2} - Width {p1:X2}");
                        break;
                    case EStandardObject.BulletShooter:
                    case EStandardObject.VerticalPipeOrBoneOrLog:
                        DrawTiles(xPos, yPos, 1, p0, new Pixel(0, 0, 255, 255));
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EStandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Height {p0:X2}");
                        break;
                    case EStandardObject.LongGroundLedge:      // Long ground ledge
                        DrawGfxTilesYTopOther(xPos, yPos, t2, 1, vram, 0x100, 0x3F);
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EStandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Length {t2:X2}");
                        break;
                    case EStandardObject.DonutBridge:
                    case EStandardObject.HorizontalPipeOrBoneOrLog:
                        DrawTiles(xPos, yPos, p1, 1, new Pixel(255, 0, 0, 255));
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EStandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Width {p1:X2}");
                        break;
                    case EStandardObject.TilesetSpecificStart01:
                    case EStandardObject.TilesetSpecificStart02:
                    case EStandardObject.TilesetSpecificStart03:
                    case EStandardObject.TilesetSpecificStart04:
                    case EStandardObject.TilesetSpecificStart05:
                    case EStandardObject.TilesetSpecificStart06:
                    case EStandardObject.TilesetSpecificStart07:
                    case EStandardObject.TilesetSpecificStart08:
                    case EStandardObject.TilesetSpecificStart09:
                    case EStandardObject.TilesetSpecificStart10:
                    case EStandardObject.TilesetSpecificStart11:
                    case EStandardObject.TilesetSpecificStart12:
                    case EStandardObject.TilesetSpecificStart13:
                    case EStandardObject.TilesetSpecificStart14:
                    case EStandardObject.TilesetSpecificStart15:
                    case EStandardObject.TilesetSpecificStart16:
                    case EStandardObject.TilesetSpecificStart17:
                    case EStandardObject.TilesetSpecificStart18:
                        switch (smwLevelHeader.GetTileset())
                        {
                            case SMWLevelHeader.Tileset.NormalCloudForest:
                                RenderTilesetSpecificSetNormalCloudForest((EStandardObject)objectNumber, xPos, yPos, p0, p1, t2, vram);
                                break;
                            case SMWLevelHeader.Tileset.Castle1:
                                RenderTilesetSpecificSetCastle1((EStandardObject)objectNumber, xPos, yPos, p0, p1, t2, vram);
                                break;
                            case SMWLevelHeader.Tileset.Rope:
                                RenderTilesetSpecificSetRope((EStandardObject)objectNumber, xPos, yPos, p0, p1, t2, vram);
                                break;
                            case SMWLevelHeader.Tileset.UndergroundPalace2Castle2:
                                RenderTilesetSpecificSetUndergroundPalace2Castle2((EStandardObject)objectNumber, xPos, yPos, p0, p1, t2, vram);
                                break;
                            case SMWLevelHeader.Tileset.GhostHouseSwitchPalace1:
                                RenderTilesetSpecificSetGhostHouseSwitchPalace1((EStandardObject)objectNumber, xPos, yPos, p0, p1, t2, vram);
                                break;
                        }
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EStandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Special {t2:X2}");
                        break;
                    default:
                        Draw16x16Tile(xPos, yPos, new Pixel(128, 128, 0, 255));
                        _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} Unknown Object @{xPos:X2},{yPos:X2}");
                        break;
                }
            }
        }
    }

    private void RenderTilesetSpecificSetNormalCloudForest(EStandardObject objectNumber, int xPos, int yPos, int p0, int p1, byte t2, SuperMarioVRam vram)
    {
        switch (objectNumber)
        {
            case EStandardObject.TilesetSpecificStart01:
            case EStandardObject.TilesetSpecificStart02:
            case EStandardObject.TilesetSpecificStart03:
            case EStandardObject.TilesetSpecificStart04:
            case EStandardObject.TilesetSpecificStart05:
            case EStandardObject.TilesetSpecificStart06:
            case EStandardObject.TilesetSpecificStart07:
            case EStandardObject.TilesetSpecificStart08:
            case EStandardObject.TilesetSpecificStart09:
            case EStandardObject.TilesetSpecificStart10:
            case EStandardObject.TilesetSpecificStart11:
            case EStandardObject.TilesetSpecificStart12:
            case EStandardObject.TilesetSpecificStart15:
            case EStandardObject.TilesetSpecificStart16:
            case EStandardObject.TilesetSpecificStart17:
                Draw16x16Tile(xPos, yPos, new Pixel(128, 0, 0, 255));
                break;
            case EStandardObject.TilesetSpecificStart13:
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
            case EStandardObject.TilesetSpecificStart14:
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
                break;
            case EStandardObject.TilesetSpecificStart18:
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
                break;
        }

    }

    private void RenderTilesetSpecificSetCastle1(EStandardObject objectNumber, int xPos, int yPos, int p0, int p1, byte t2, SuperMarioVRam vram)
    {
        switch (objectNumber)
        {
            case EStandardObject.TilesetSpecificStart01:
            case EStandardObject.TilesetSpecificStart02:
            case EStandardObject.TilesetSpecificStart03:
            case EStandardObject.TilesetSpecificStart04:
            case EStandardObject.TilesetSpecificStart05:
            case EStandardObject.TilesetSpecificStart06:
            case EStandardObject.TilesetSpecificStart07:
            case EStandardObject.TilesetSpecificStart08:
            case EStandardObject.TilesetSpecificStart09:
            case EStandardObject.TilesetSpecificStart10:
            case EStandardObject.TilesetSpecificStart11:
            case EStandardObject.TilesetSpecificStart12:
            case EStandardObject.TilesetSpecificStart13:
            case EStandardObject.TilesetSpecificStart14:
            case EStandardObject.TilesetSpecificStart16:
            case EStandardObject.TilesetSpecificStart17:
            case EStandardObject.TilesetSpecificStart18:
                Draw16x16Tile(xPos, yPos, new Pixel(128, 0, 0, 255));
                break;
            case EStandardObject.TilesetSpecificStart15:
                DrawGfx9Tile(xPos, yPos, p1, p0, vram, new int[] { 0x15D, 0x15E, 0x15F, 0x160, 0x161, 0x162, 0x163, 0x164, 0x165 });
                break;
        }
    }

    private void RenderTilesetSpecificSetRope(EStandardObject objectNumber, int xPos, int yPos, int p0, int p1, byte t2, SuperMarioVRam vram)
    {
        switch (objectNumber)
        {
            case EStandardObject.TilesetSpecificStart01:
            case EStandardObject.TilesetSpecificStart02:
            case EStandardObject.TilesetSpecificStart03:
            case EStandardObject.TilesetSpecificStart04:
            case EStandardObject.TilesetSpecificStart05:
            case EStandardObject.TilesetSpecificStart06:
            case EStandardObject.TilesetSpecificStart07:
            case EStandardObject.TilesetSpecificStart08:
            case EStandardObject.TilesetSpecificStart09:
            case EStandardObject.TilesetSpecificStart10:
            case EStandardObject.TilesetSpecificStart11:
            case EStandardObject.TilesetSpecificStart12:
            case EStandardObject.TilesetSpecificStart13:
            case EStandardObject.TilesetSpecificStart14:
            case EStandardObject.TilesetSpecificStart17:
            case EStandardObject.TilesetSpecificStart18:
                Draw16x16Tile(xPos, yPos, new Pixel(128, 0, 0, 255));
                break;
            case EStandardObject.TilesetSpecificStart15:
                // mushroom platform top (p1 width)
                DrawGfxTiles(xPos, yPos, p1, 0, vram, 0x107, 0x108, 0x109);
                break;
            case EStandardObject.TilesetSpecificStart16:
                // mushroom platform bottom (p1 width, p0 height)
                DrawGfxTiles(xPos, yPos, p1, p0, vram, 0x73, 0x074, 0x75);
                break;
        }
    }

    private void RenderTilesetSpecificSetUndergroundPalace2Castle2(EStandardObject objectNumber, int xPos, int yPos, int p0, int p1, byte t2, SuperMarioVRam vram)
    {
        switch (objectNumber)
        {
            case EStandardObject.TilesetSpecificStart01:
            case EStandardObject.TilesetSpecificStart02:
            case EStandardObject.TilesetSpecificStart03:
            case EStandardObject.TilesetSpecificStart04:
            case EStandardObject.TilesetSpecificStart05:
            case EStandardObject.TilesetSpecificStart06:
            case EStandardObject.TilesetSpecificStart07:
            case EStandardObject.TilesetSpecificStart08:
            case EStandardObject.TilesetSpecificStart09:
            case EStandardObject.TilesetSpecificStart10:
            case EStandardObject.TilesetSpecificStart11:
            case EStandardObject.TilesetSpecificStart12:
            case EStandardObject.TilesetSpecificStart13:
            case EStandardObject.TilesetSpecificStart14:
            case EStandardObject.TilesetSpecificStart15:
            case EStandardObject.TilesetSpecificStart16:
            case EStandardObject.TilesetSpecificStart17:
            case EStandardObject.TilesetSpecificStart18:
                Draw16x16Tile(xPos, yPos, new Pixel(128, 0, 0, 255));
                break;
        }
    }

    private void RenderTilesetSpecificSetGhostHouseSwitchPalace1(EStandardObject objectNumber, int xPos, int yPos, int p0, int p1, byte t2, SuperMarioVRam vram)
    {
        switch (objectNumber)
        {
            case EStandardObject.TilesetSpecificStart01:
            case EStandardObject.TilesetSpecificStart02:
            case EStandardObject.TilesetSpecificStart03:
            case EStandardObject.TilesetSpecificStart04:
            case EStandardObject.TilesetSpecificStart05:
            case EStandardObject.TilesetSpecificStart06:
            case EStandardObject.TilesetSpecificStart07:
            case EStandardObject.TilesetSpecificStart08:
            case EStandardObject.TilesetSpecificStart09:
            case EStandardObject.TilesetSpecificStart10:
            case EStandardObject.TilesetSpecificStart11:
            case EStandardObject.TilesetSpecificStart12:
            case EStandardObject.TilesetSpecificStart13:
            case EStandardObject.TilesetSpecificStart14:
            case EStandardObject.TilesetSpecificStart15:
            case EStandardObject.TilesetSpecificStart16:
            case EStandardObject.TilesetSpecificStart17:
            case EStandardObject.TilesetSpecificStart18:
                Draw16x16Tile(xPos, yPos, new Pixel(128, 0, 0, 255));
                break;
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
