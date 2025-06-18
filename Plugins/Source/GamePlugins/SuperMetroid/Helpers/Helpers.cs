using System;
using System.Collections.Generic;
using RetroEditor.Plugins;

// TODO STOLEN FROM SMW - SHOULD BE IN A SHARED LIBRARY (ONE PER ROMPLUGIN?)
namespace RetroEditorPlugin_SuperMetroid
{
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
            uint bank = (address >> 16) & 0x7F;
            uint rom = address & 0xFFFF;

            if (rom < 0x8000)
            {
                throw new System.Exception("Invalid address");
            }
            rom -= 0x8000;
            return bank * 0x8000 + rom;
        }

        public uint FromImage(uint address)
        {
            uint bank = address / 0x8000;
            uint rom = address % 0x8000;
            return (bank << 16) | rom;
        }
    }

    public static class LC_LZ5
    {
        private static void DecompressCode(ref byte[] decompBuffer, ref ReadOnlySpan<byte> data, ref int offset, int l, int c, bool LongLength)
        {
            var count = l;
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
                        bool oddeven = false;
                        for (int i = 0; i <= count; i++)
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
                        var src = (h << 8) | L;
                        data = data.Slice(2);
                        for (int i = 0; i <= count; i++)
                        {
                            decompBuffer[offset++] = decompBuffer[src++];
                        }
                    }
                    break;
                case 5: //EOR Repeat
                    {
                        var h = data[1];
                        var L = data[0];
                        var src = (h << 8) | L;
                        data = data.Slice(2);
                        for (int i = 0; i <= count; i++)
                        {
                            decompBuffer[offset++] = (byte)(decompBuffer[src++] ^ 0xFF);
                        }
                    }
                    break;
                case 6: //Minus Copy
                    {
                        var negOffs = data[0];
                        var src = offset - negOffs;
                        data = data.Slice(1);
                        for (int i = 0; i <= count; i++)
                        {
                            decompBuffer[offset++] = decompBuffer[src++];
                        }
                    }
                    break;
                case 7: //LongLength
                    {
                        if (LongLength)
                        {
                            throw new NotImplementedException("LongLength exclusive code not implemented yet");
                        }
                        else
                        {
                            var nc = (l & 0x1C) >> 2;
                            var nl = (l & 3) << 8;
                            nl |= data[0];
                            data = data.Slice(1);
                            DecompressCode(ref decompBuffer, ref data, ref offset, nl, nc, true);
                        }
                    }
                    break;

            }

        }

        public static int Decompress(ref byte[] toDecompressBuffer, ReadOnlySpan<byte> data, out int bytesRead)
        {
            //LC_LZ2
            var offset = 0;
            var origDataLength = data.Length;

            while (data.Length > 0)
            {
                var b = data[0];
                if (b == 0xFF)
                {
                    break;
                }
                data = data.Slice(1);
                var l = b & 0x1F;
                var c = (b & 0xE0) >> 5;
                DecompressCode(ref toDecompressBuffer, ref data, ref offset, l, c, false);
            }

            bytesRead = origDataLength - data.Length;
            return offset;
        }
    }
/*
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
        public const uint TileSet1Base_100 = 0x0D8398;    // To align floor top tiles in 510
        public const uint TileSet2Base_100 = 0x0DCC30;    // To align mushroom floor in 511
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
        public const int DefaultLevel = 510;
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


    public class SuperMarioPalette
    {
        Pixel[,] _palette;
        Pixel _backgroundColour;

        Pixel SNESToPixel(ushort c)
        {
            var r = ((c & 0x1F) << 3) | ((c & 1) != 0 ? 7 : 0);
            var g = (((c >> 5) & 0x1F) << 3) | ((c & 0x20) != 0 ? 7 : 0);
            var b = (((c >> 10) & 0x1F) << 3) | ((c & 0x400) != 0 ? 7 : 0);
            return new Pixel((byte)r, (byte)g, (byte)b, 255);
        }

        public SuperMarioPalette(IMemoryAccess rom, SMWLevelHeader header)
        {
            _palette = new Pixel[16, 16];  // Perhaps we should have platform colour constructors e.g. SNESToPixel, etc?
            var addressTranslation = new LoRom();

            for (int i = 0; i < 16; i++)
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
            var palette = rom.ReadBytes(ReadKind.Rom, addressTranslation.ToImage(SMWAddresses.PlayerPaletteTable + 0u* 0x14u), 0x14);
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

        public Pixel this[int palette, int colour]
        {
            get
            {
                return _palette[palette, colour];
            }
        }

        public Pixel BackgroundColour => _backgroundColour;
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

        public enum Tileset
        {
            NormalCloudForest = 0x00,
            Castle1 = 0x01,
            Rope = 0x02,
            UndergroundPalace2Castle2 = 0x03,
            GhostHouseSwitchPalace1 = 0x04,
        }

        public Tileset GetTileset()
        {
            switch (FGBGGFXSetting)
            {
                default:
                case 0x00:                      // Normal 1
                case 0x07:                      // Normal 2
                case 0x0C:                      // Cloud/Forest
                    return Tileset.NormalCloudForest;
                case 0x01:                      // Castle 1
                    return Tileset.Castle1;
                case 0x02:                      // Rope 1
                case 0x06:                      // Rope 2
                case 0x08:                      // Rope 3
                    return Tileset.Rope;
                case 0x03:                      // Underground 1
                case 0x09:                      // Underground 2
                case 0x0E:                      // Underground 3
                case 0x0A:                      // Switch Palace 2
                case 0x0B:                      // Castle 2
                    return Tileset.UndergroundPalace2Castle2;
                case 0x04:                      // Switch Palace 1
                case 0x05:                      // Ghost House 1
                case 0x0D:                      // Ghost House 2
                    return Tileset.GhostHouseSwitchPalace1;
            }
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
            var data = rom.ReadBytes(ReadKind.Rom, addressTranslation.ToImage(Address), (uint)(8 * (end - start + 1)));
            for (int a = start; a <= end; a++)
            {
                var Test2D = data.Slice((a - start) * 8, 8);
                map16ToTile[a] = new Tile16x16(Test2D);
            }
        }

        public SMWMap16(IMemoryAccess rom, AddressTranslation addressTranslation, SMWLevelHeader header)
        {
            var baseAddressTileSpecific073 = SMWAddresses.TileSet0Base_073;
            var baseAddressTileSpecific100 = SMWAddresses.TileSet0Base_100;
            var baseAddressTileSpecific153 = SMWAddresses.TileSet0Base_153;
            switch (header.GetTileset())
            {
                case SMWLevelHeader.Tileset.NormalCloudForest:
                    break;
                case SMWLevelHeader.Tileset.Castle1:
                    baseAddressTileSpecific073 = SMWAddresses.TileSet1Base_073;
                    baseAddressTileSpecific100 = SMWAddresses.TileSet1Base_100;
                    baseAddressTileSpecific153 = SMWAddresses.TileSet1Base_153;
                    break;
                case SMWLevelHeader.Tileset.Rope:
                    baseAddressTileSpecific073 = SMWAddresses.TileSet2Base_073;
                    baseAddressTileSpecific100 = SMWAddresses.TileSet2Base_100;
                    baseAddressTileSpecific153 = SMWAddresses.TileSet2Base_153;
                    break;
                case SMWLevelHeader.Tileset.UndergroundPalace2Castle2:
                    baseAddressTileSpecific073 = SMWAddresses.TileSet3Base_073;
                    baseAddressTileSpecific100 = SMWAddresses.TileSet3Base_100;
                    baseAddressTileSpecific153 = SMWAddresses.TileSet3Base_153;
                    break;
                case SMWLevelHeader.Tileset.GhostHouseSwitchPalace1:
                    baseAddressTileSpecific073 = SMWAddresses.TileSet4Base_073;
                    baseAddressTileSpecific100 = SMWAddresses.TileSet4Base_100;
                    baseAddressTileSpecific153 = SMWAddresses.TileSet4Base_153;
                    break;
            }

            SetUpTiles(0x000, 0x072, SMWAddresses.TileData_000_072, rom, addressTranslation);
            SetUpTiles(0x073, 0x0FF, baseAddressTileSpecific073, rom, addressTranslation);
            SetUpTiles(0x100, 0x110, baseAddressTileSpecific100, rom, addressTranslation);
            SetUpTiles(0x111, 0x152, SMWAddresses.TileData_111_152, rom, addressTranslation);
            SetUpTiles(0x153, 0x16D, baseAddressTileSpecific153, rom, addressTranslation);
            SetUpTiles(0x16E, 0x1C3, SMWAddresses.TileData_16E_1C3, rom, addressTranslation);
            SetUpTiles(0x1C4, 0x1C7, SMWAddresses.TileData_1C4_1C7, rom, addressTranslation);
            SetUpTiles(0x1C8, 0x1EB, SMWAddresses.TileData_1C8_1EB, rom, addressTranslation);
            SetUpTiles(0x1EC, 0x1EF, SMWAddresses.TileData_1EC_1EF, rom, addressTranslation);
            SetUpTiles(0x1F0, 0x1FF, SMWAddresses.TileData_1F0_1FF, rom, addressTranslation);

            // Finally, setup the map16 BG data (stored in slots 512-1023)
            SetUpTiles(0x200, 0x3FF, SMWAddresses.TileData_Map16BG, rom, addressTranslation);
        }


    }

    static class SMWRenderHelpers
    {
        static void Draw8x8(int tx, int ty, int xo, int yo, SubTile tile, SuperMarioVRam vram, ref Pixel[] pixels, uint Width, uint Height, SuperMarioPalette palette)
        {
            var tileNum = tile.tile;
            var gfx = vram.Tile(tileNum);
            // compute tile position TL
            var tileX = tileNum % 16;
            var tileY = tileNum / 16;

            int ox = tx * 16 + 8 * xo;
            int oy = ty * 16 + 8 * yo;
            int g = 0;
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    var px = ox + (tile.flipx ? 7 - x : x);
                    var py = oy + (tile.flipy ? 7 - y : y);
                    if (px >= 0 && px < Width && py >= 0 && py < Height)
                    {
                        var c = gfx[g++];
                        if (c != 0)
                        {
                            pixels[py * Width + px] = palette[tile.palette, c];
                        }
                    }
                }
            }
        }

        public static void DrawGfxTile(int tx, int ty, Tile16x16 tile16, SuperMarioVRam vram, ref Pixel[] pixels, uint Width, uint Height, SuperMarioPalette palette)
        {
            // Just draw the TL tile for now
            Draw8x8(tx, ty, 0, 0, tile16.TL, vram, ref pixels, Width, Height, palette);
            Draw8x8(tx, ty, 1, 0, tile16.TR, vram, ref pixels, Width, Height, palette);
            Draw8x8(tx, ty, 0, 1, tile16.BL, vram, ref pixels, Width, Height, palette);
            Draw8x8(tx, ty, 1, 1, tile16.BR, vram, ref pixels, Width, Height, palette);
        }

    }
*/
}