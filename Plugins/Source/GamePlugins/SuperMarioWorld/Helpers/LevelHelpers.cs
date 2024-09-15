using System;
using System.Collections.Generic;
using RetroEditor.Plugins;

namespace RetroEditorPlugin_SuperMarioWorld
{
    public class SMWObject : IObject
    {
        public uint Width => _width;

        public uint Height => _height;

        public uint X => _x;

        public uint Y => _y;

        public string Name => _name;

        public SMWObject(uint x,uint y, uint width, uint height, string name, uint[] mapData)
        {
            _x = x * 16;
            _y = y * 16;
            _width = width;
            _height = height;
            _name = name;
            _mapData = mapData;
        }

        public ReadOnlySpan<uint> GetMapData()
        {
            return _mapData;
        }

        protected uint _x, _y, _width, _height;
        protected string _name;
        protected uint[] _mapData;
    }
    enum EStandardObject
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

    enum EExtendedObject
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


    public class StandardObject : SMWObject
    {
        public StandardObject(uint x, uint y, uint width, uint height, string name, uint[] mapData) : base(x, y, width, height, name, mapData)
        {
        }
        public StandardObject(uint x, uint y, uint width, uint height, string name, ReadOnlySpan<byte> mapData) : base(x, y, width, height, name, null)
        {
            var n = new uint[mapData.Length];
            for (int a=0;a<mapData.Length;a++)
            {
                n[a] = mapData[a];
            }
            _mapData = n;
        }

        public void Move(uint x, uint y)
        {
            var tx = x;
            var ty = y;
            // Clamp to tile 16x16 grid
            tx = (tx / 16) * 16;
            ty = (ty / 16) * 16;
            _x = Math.Min(Math.Max(tx, 0u), 16u*16u*32u);
            _y = Math.Min(Math.Max(ty, 0u), 416u);
        }
    }

    public class SizeXYObject : StandardObject
    {
        public SizeXYObject(uint x, uint y, uint width, uint height, string name, uint[] mapData) : base(x, y, width, height, name, mapData)
        {
            // Override mapdata resized based on 9tile
            var actual = new uint [width*height];
            for (int a=0;a<width*height;a++)
            {
                actual[a] = mapData[0];
            }
            _mapData = actual;
        }
    }

    public class Size9TileObject : StandardObject
    {
        public Size9TileObject(uint x, uint y, uint width, uint height, string name, uint[] mapData) : base(x, y, width, height, name, mapData)
        {
            // Override mapdata resized based on 9tile
            var actual = new uint [width*height];
            var actualIdx = 0;
            uint t0, t1, t2;
            for (int yy = 0; yy < height; yy++)
            {
                if (yy == 0)
                {
                    t0 = mapData[0];
                    t1 = mapData[1];
                    t2 = mapData[2];
                }
                else if (yy == height - 1)
                {
                    t0 = mapData[6];
                    t1 = mapData[7];
                    t2 = mapData[8];
                }
                else
                {
                    t0 = mapData[3];
                    t1 = mapData[4];
                    t2 = mapData[5];
                }
                for (int xx = 0; xx < width; xx++)
                {
                    if (xx == 0)
                        actual[actualIdx++] = t0;
                    else if (xx == width - 1)
                        actual[actualIdx++] = t2;
                    else
                        actual[actualIdx++] = t1;
                }
            }
            _mapData = actual;
        }

    }

    class LevelHelpers
    {
        private IMemoryAccess _rom;
        private AddressTranslation _addressTranslation;
        private IEditor _editorInterface;
        public LevelHelpers(IMemoryAccess rom, IEditor editorInterface)
        {
            _rom = rom;
            _addressTranslation = new LoRom();
            _editorInterface = editorInterface;
        }

        public List<IObject> FetchObjectLayer(ref SuperMarioWorldRomHelpers smwRom, SMWLevelHeader smwLevelHeader, SuperMarioVRam vram, uint layerAddress)
        {
            var objectList = new List<IObject>();
            var screenOffsetNumber = 0;
            bool layerDone = false;
            uint offset = 0;
            while (!layerDone)
            {
                var triple = _rom.ReadBytes(ReadKind.Rom, layerAddress + offset, 3);
                if (triple[0] == 0xFF)
                {
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

                uint yPos = t0 & 0x1Fu;
                uint xPos = (uint)(screenOffsetNumber * 16u + (t1 & 0x0Fu));
                uint p0 = (t2 & 0xF0u) >> 4;
                uint p1 = t2 & 0x0Fu;

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
                            //Draw16x16Tile(xPos, yPos, new Pixel(128, 128, 128, 255));
                            _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EExtendedObject)objectNumber} @{xPos:X2},{yPos:X2}");
                            break;
                        case EExtendedObject.QBlockYoshi:
                            objectList.Add(new StandardObject(xPos, yPos, 1, 1, "QBlock With Yoshi", new uint[] { 0x126 }));
                            _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EExtendedObject)objectNumber} @{xPos:X2},{yPos:X2}");
                            break;
                        case EExtendedObject.BigBush1:
                            objectList.Add(new StandardObject(xPos, yPos, 9u, 5u, "BigBush1", _rom.ReadBytes(ReadKind.Rom, _addressTranslation.ToImage(SMWAddresses.LargeBush), 45)));
                            _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EExtendedObject)objectNumber} @{xPos:X2},{yPos:X2}");
                            break;
                        case EExtendedObject.BigBush2:
                            objectList.Add(new StandardObject(xPos, yPos, 6u, 4u, "BigBush2", _rom.ReadBytes(ReadKind.Rom, _addressTranslation.ToImage(SMWAddresses.MediumBush), 24)));
                            _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EExtendedObject)objectNumber} @{xPos:X2},{yPos:X2}");
                            break;
                        case EExtendedObject.YoshiCoin:
                            objectList.Add(new StandardObject(xPos, yPos, 1, 2, "Yoshi Coin", new uint[] { 0x2D, 0x2E }));
                            _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EExtendedObject)objectNumber} @{xPos:X2},{yPos:X2}");
                            break;
                        case EExtendedObject.RedBerry:
                            objectList.Add(new StandardObject(xPos, yPos, 1, 1, "Red Berry", new uint[] { 0x45 }));
                            _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EExtendedObject)objectNumber} @{xPos:X2},{yPos:X2}");
                            break;
                        case EExtendedObject.PinkBerry:
                            objectList.Add(new StandardObject(xPos, yPos, 1, 1, "Pink Berry", new uint[] { 0x46 }));
                            _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EExtendedObject)objectNumber} @{xPos:X2},{yPos:X2}");
                            break;
                        case EExtendedObject.GreenBerry:
                            objectList.Add(new StandardObject(xPos, yPos, 1, 1, "Green Berry", new uint[] { 0x47 }));
                            _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EExtendedObject)objectNumber} @{xPos:X2},{yPos:X2}");
                            break;
                        default:
                            //Draw16x16Tile(xPos, yPos, new Pixel(32, 32, 32, 255));

                            _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} Unknown Extended Object @{xPos:X2},{yPos:X2}");
                            break;
                    }
                }
                else
                {
                    switch ((EStandardObject)objectNumber)
                    {
                        case EStandardObject.GroundLedge:
                            objectList.Add(new Size9TileObject(xPos, yPos, p1 + 1u, p0 + 1u, "GroundLedge", new uint[] { 0x100, 0x100, 0x100, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F }));
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
                            //DrawTiles(xPos, yPos, p1, p0, new Pixel(0, 255, 255, 255));
                            _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EStandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Height {p0:X2} - Width {p1:X2}");
                            break;
                        case EStandardObject.WalkThroughDirt:
                            objectList.Add(new Size9TileObject(xPos, yPos, p1 + 1u, p0 + 1u, "WalkThroughDirt", new uint[] { 0x3F, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F }));
                            _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EStandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Height {p0:X2} - Width {p1:X2}");
                            break;
                        case EStandardObject.Coins:
                            objectList.Add(new SizeXYObject(xPos, yPos, p1+1u, p0+1u, "Coins", new uint[] { 0x2B }));
                            _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EStandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Height {p0:X2} - Width {p1:X2}");
                            break;
                        case EStandardObject.VerticalPipes:
                        case EStandardObject.NetVerticalEdge:
                            //DrawTiles(xPos, yPos, 1, p0, new Pixel(255, 0, 255, 255));
                            _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EStandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Height {p0:X2} - Type {p1:X2}");
                            break;
                        case EStandardObject.LedgeEdges:
                            switch (p1)
                            {
                                case 0x3:
                                    objectList.Add(new Size9TileObject(xPos, yPos, 1u, p0 + 2u, "LedgeEdge3", new uint[] { 0x145, 0x145, 0x145, 0x14B, 0x14B, 0x14B, 0x14B, 0x14B, 0x14B }));
                                    break;
                                case 0x5:
                                    objectList.Add(new Size9TileObject(xPos, yPos, 1u, p0 + 2u, "LedgeEdge5", new uint[] { 0x148, 0x148, 0x148, 0x14C, 0x14C, 0x14C, 0x14C, 0x14C, 0x14C }));
                                    break;
                                case 0x7:
                                    objectList.Add(new Size9TileObject(xPos, yPos, 1u, p0 + 1u, "LedgeEdge7", new uint[] { 0x101, 0x101, 0x101, 0x040, 0x040, 0x040, 0x040, 0x040, 0x040 }));
                                    break;
                                case 0x8:
                                    objectList.Add(new Size9TileObject(xPos, yPos, 1u, p0 + 1u, "LedgeEdge8", new uint[] { 0x103, 0x103, 0x103, 0x041, 0x041, 0x041, 0x041, 0x041, 0x041 }));
                                    break;
                                case 0xB:
                                    objectList.Add(new Size9TileObject(xPos, yPos, 1u, p0 + 2u, "LedgeEdgeB", new uint[] { 0x145, 0x145, 0x145, 0x14B, 0x14B, 0x14B, 0x1E2, 0x1E2, 0x1E2 }));
                                    break;
                                case 0xD:
                                    objectList.Add(new Size9TileObject(xPos, yPos, 1u, p0 + 2u, "LedgeEdgeD", new uint[] { 0x148, 0x148, 0x148, 0x14C, 0x14C, 0x14C, 0x1E4, 0x1E4, 0x1E4 }));
                                    break;
                                default:
                                    //DrawTiles(xPos, yPos, 1, p0, new Pixel(255, 0, 255, 255));
                                    break;
                            }
                            _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EStandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Height {p0:X2} - Type {p1:X2}");
                            break;
                        case EStandardObject.MidwayGoalPoint:
                            if (p1 == 1)
                            {
                                objectList.Add(new Size9TileObject(xPos, yPos, 3u, p0+1u, "MidwayGoalB", new uint[] { 0x39, 0x25, 0x3C, 0x3A, 0x25, 0x3D, 0x3B, 0x25, 0x3E }));
                            }
                            else
                            {
                                //DrawTiles(xPos, yPos, 1, p0, new Pixel(255, 0, 255, 255));
                            }
                            _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EStandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Height {p0:X2} - Type {p1:X2}");
                            break;
                        case EStandardObject.Slopes:
                            if (p1 == 5)
                            {
                                // NOTE PADS next hieght with 3Fs on the left
                                //DrawGfxTile(xPos, yPos, 0x182, vram);
                                //DrawGfxTile(xPos + 1, yPos, 0x187, vram);
                                //DrawGfxTile(xPos + 2, yPos, 0x18C, vram);
                                //DrawGfxTile(xPos + 3, yPos, 0x191, vram);
                                yPos += 1;
                                //DrawGfxTile(xPos, yPos, 0x1E6, vram);
                                //DrawGfxTile(xPos + 1, yPos, 0x1E6, vram);
                                //DrawGfxTile(xPos + 2, yPos, 0x1DB, vram);
                                //DrawGfxTile(xPos + 3, yPos, 0x1DC, vram);
                            }
                            else
                            {
                                //DrawTiles(xPos, yPos, 1, p0, new Pixel(255, 0, 255, 255));
                            }
                            _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EStandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Height {p0:X2} - Type {p1:X2}");
                            break;

                        case EStandardObject.HorizontalPipes:
                            switch (p0)
                            {
                                case 0: // Ends on left
                                case 1: // exit enabled
                                    //DrawGfxTile(xPos, yPos, 0x13B, vram);
                                    //DrawGfxTile(xPos, yPos + 1, 0x13C, vram);
                                    for (int a = 1; a <= p1; a++)
                                    {
                                        //DrawGfxTile(xPos + a, yPos, 0x13D, vram);
                                        //DrawGfxTile(xPos + a, yPos + 1, 0x13E, vram);
                                    }
                                    break;
                                case 2: // Ends on right
                                case 3: // exit enabled
                                    for (int a = 0; a < p1; a++)
                                    {
                                        //DrawGfxTile(xPos + a, yPos, 0x13D, vram);
                                        //DrawGfxTile(xPos + a, yPos + 1, 0x13E, vram);
                                    }
                                    //DrawGfxTile(xPos + p1, yPos, 0x13B, vram);
                                    //DrawGfxTile(xPos + p1, yPos + 1, 0x13C, vram);
                                    break;
                            }
                            _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EStandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Type {p0:X2} - Width {p1:X2}");
                            break;
                        case EStandardObject.RopeOrClouds:
                            //DrawTiles(xPos, yPos, p1, 1, new Pixel(255, 255, 0, 255));
                            _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EStandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Type {p0:X2} - Width {p1:X2}");
                            break;
                        case EStandardObject.BulletShooter:
                        case EStandardObject.VerticalPipeOrBoneOrLog:
                            //DrawTiles(xPos, yPos, 1, p0, new Pixel(0, 0, 255, 255));
                            _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EStandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Height {p0:X2}");
                            break;
                        case EStandardObject.LongGroundLedge:      // Long ground ledge
                            objectList.Add(new Size9TileObject(xPos, yPos, t2 + 1u, 2u, "LongGroundLedge", new uint[] { 0x100, 0x100, 0x100, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F }));
                            _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EStandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Length {t2:X2}");
                            break;
                        case EStandardObject.DonutBridge:
                        case EStandardObject.HorizontalPipeOrBoneOrLog:
                            //DrawTiles(xPos, yPos, p1, 1, new Pixel(255, 0, 0, 255));
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
                                    TilesetSpecificSetNormalCloudForest(ref objectList, (EStandardObject)objectNumber, xPos, yPos, p0, p1, t2, vram);
                                    break;
                                case SMWLevelHeader.Tileset.Castle1:
                                    TilesetSpecificSetCastle1(ref objectList, (EStandardObject)objectNumber, xPos, yPos, p0, p1, t2, vram);
                                    break;
                                case SMWLevelHeader.Tileset.Rope:
                                    TilesetSpecificSetRope(ref objectList, (EStandardObject)objectNumber, xPos, yPos, p0, p1, t2, vram);
                                    break;
                                case SMWLevelHeader.Tileset.UndergroundPalace2Castle2:
                                    TilesetSpecificSetUndergroundPalace2Castle2(ref objectList, (EStandardObject)objectNumber, xPos, yPos, p0, p1, t2, vram);
                                    break;
                                case SMWLevelHeader.Tileset.GhostHouseSwitchPalace1:
                                    TilesetSpecificSetGhostHouseSwitchPalace1(ref objectList, (EStandardObject)objectNumber, xPos, yPos, p0, p1, t2, vram);
                                    break;
                            }
                            _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} {(EStandardObject)objectNumber} @{xPos:X2},{yPos:X2} - Special {t2:X2}");
                            break;
                        default:
                            //Draw16x16Tile(xPos, yPos, new Pixel(128, 128, 0, 255));
                            _editorInterface.Log(LogType.Info, $"{screenOffsetNumber:X2} | {objectNumber:X2} Unknown Object @{xPos:X2},{yPos:X2}");
                            break;
                    }
                }
            }
            return objectList;
        }

        private void TilesetSpecificSetNormalCloudForest(ref List<IObject> objectList, EStandardObject objectNumber, uint xPos, uint yPos, uint p0, uint p1, byte t2, SuperMarioVRam vram)
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
                    //Draw16x16Tile(xPos, yPos, new Pixel(128, 0, 0, 255));
                    break;
                case EStandardObject.TilesetSpecificStart13:
                    // Left facing diagonal ledge (see right, just different codes basically)
                    {
                        //DrawGfxTile(xPos, yPos, 0x1AA, vram);
                        //DrawGfxTile(xPos + 1, yPos, 0x0A1, vram);
                        yPos += 1;
                        xPos -= 1;
                        //DrawGfxTile(xPos, yPos, 0x1AA, vram);
                        //DrawGfxTile(xPos + 1, yPos, 0x1E2, vram);
                        //DrawGfxTile(xPos + 2, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 3, yPos, 0x0A6, vram);
                        yPos += 1;
                        xPos -= 1;
                        //DrawGfxTile(xPos, yPos, 0x1AA, vram);
                        //DrawGfxTile(xPos + 1, yPos, 0x1E2, vram);
                        //DrawGfxTile(xPos + 2, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 3, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 4, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 5, yPos, 0x0A6, vram);
                        yPos += 1;
                        xPos -= 1;
                        //DrawGfxTile(xPos, yPos, 0x1AA, vram);
                        //DrawGfxTile(xPos + 1, yPos, 0x1E2, vram);
                        //DrawGfxTile(xPos + 2, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 3, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 4, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 5, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 6, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 7, yPos, 0x0A6, vram);
                        yPos += 1;
                        //DrawGfxTile(xPos, yPos, 0x1F7, vram);
                        //DrawGfxTile(xPos + 1, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 2, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 3, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 4, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 5, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 6, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 7, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 8, yPos, 0x0A6, vram);
                        yPos += 1;
                        xPos += 1;
                        //DrawGfxTile(xPos, yPos, 0x0A3, vram);
                        //DrawGfxTile(xPos + 1, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 2, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 3, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 4, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 5, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 6, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 7, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 8, yPos, 0x0A6, vram);
                        yPos += 1;
                        xPos += 1;
                        //DrawGfxTile(xPos, yPos, 0x0A3, vram);
                        //DrawGfxTile(xPos + 1, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 2, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 3, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 4, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 5, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 6, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 7, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 8, yPos, 0x0A6, vram);
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
                        //DrawGfxTile(xPos, yPos, 0x0AF, vram);
                        //DrawGfxTile(xPos + 1, yPos, 0x1AF, vram);
                        yPos += 1;
                        xPos -= 1;
                        //DrawGfxTile(xPos, yPos, 0x0A9, vram);
                        //DrawGfxTile(xPos + 1, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 2, yPos, 0x1E4, vram);
                        //DrawGfxTile(xPos + 3, yPos, 0x1AF, vram);
                        yPos += 1;
                        xPos -= 1;
                        //DrawGfxTile(xPos, yPos, 0x0A9, vram);
                        //DrawGfxTile(xPos + 1, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 2, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 3, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 4, yPos, 0x1E4, vram);
                        //DrawGfxTile(xPos + 5, yPos, 0x1AF, vram);
                        yPos += 1;
                        xPos -= 1;
                        //DrawGfxTile(xPos, yPos, 0x0A9, vram);
                        //DrawGfxTile(xPos + 1, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 2, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 3, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 4, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 5, yPos, 0x03F, vram);
                        //DrawGfxTile(xPos + 6, yPos, 0x1F9, vram);
                    }
                    break;
                case EStandardObject.TilesetSpecificStart18:
                    switch (p0)
                    {
                        default:
                        case 0:
                            objectList.Add(new Size9TileObject(xPos, yPos, p1 + 1u, 1u, "grass", new uint[] { 0x73, 0x74, 0x79 }));
                            break;
                        case 1:
                            objectList.Add(new Size9TileObject(xPos, yPos, p1 + 1u, 1u, "grass", new uint[] { 0x7A, 0x7B, 0x80 }));
                            break;
                        case 2:
                            objectList.Add(new Size9TileObject(xPos, yPos, p1 + 1u, 1u, "grass", new uint[] { 0x85, 0x86, 0x87 }));
                            break;
                    }
                    break;
            }

        }

        private void TilesetSpecificSetCastle1(ref List<IObject> objectList,EStandardObject objectNumber, uint xPos, uint yPos, uint p0, uint p1, byte t2, SuperMarioVRam vram)
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
                    //Draw16x16Tile(xPos, yPos, new Pixel(128, 0, 0, 255));
                    break;
                case EStandardObject.TilesetSpecificStart15:
                    objectList.Add(new Size9TileObject(xPos, yPos, p1 + 1u, p0 + 1u, "CastleBlock", new uint[] { 0x15D, 0x15E, 0x15F, 0x160, 0x161, 0x162, 0x163, 0x164, 0x165 }));
                    break;
            }
        }

        private void TilesetSpecificSetRope(ref List<IObject> objectList, EStandardObject objectNumber, uint xPos, uint yPos, uint p0, uint p1, byte t2, SuperMarioVRam vram)
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
                    //Draw16x16Tile(xPos, yPos, new Pixel(128, 0, 0, 255));
                    break;
                case EStandardObject.TilesetSpecificStart15:
                    // mushroom platform top (p1 width)
                    objectList.Add(new Size9TileObject(xPos, yPos, p1 + 1u, 1, "MushroomPlatformTop", new uint[] { 0x107, 0x108, 0x109 }));
                    break;
                case EStandardObject.TilesetSpecificStart16:
                    // mushroom platform bottom (p1 width, p0 height)
                    objectList.Add(new Size9TileObject(xPos, yPos, p1 + 1u, p0 + 1u, "MushroomPlatformBottom", new uint[] { 0x73, 0x74, 0x75, 0x73, 0x74, 0x75, 0x73, 0x74, 0x75 }));
                    break;
            }
        }

        private void TilesetSpecificSetUndergroundPalace2Castle2(ref List<IObject> objectList, EStandardObject objectNumber, uint xPos, uint yPos, uint p0, uint p1, byte t2, SuperMarioVRam vram)
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
                    //Draw16x16Tile(xPos, yPos, new Pixel(128, 0, 0, 255));
                    break;
            }
        }

        private void TilesetSpecificSetGhostHouseSwitchPalace1(ref List<IObject> objectList, EStandardObject objectNumber, uint xPos, uint yPos, uint p0, uint p1, byte t2, SuperMarioVRam vram)
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
                    //Draw16x16Tile(xPos, yPos, new Pixel(128, 0, 0, 255));
                    break;
            }
        }

    }
}