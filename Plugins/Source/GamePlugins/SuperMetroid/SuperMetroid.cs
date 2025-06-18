using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using RetroEditor.Plugins;

public class SuperMetroid : IRetroPlugin, IMenuProvider
{
    public static string Name => "Super Metroid";

    public string RomPluginName => "SNES";

    public bool RequiresAutoLoad => false;

    byte[] super_metroid_us = [0x21, 0xf3, 0xe9, 0x8d, 0xf4, 0x78, 0x0e, 0xe1, 0xc6, 0x67, 0xb8, 0x4e, 0x57, 0xd8, 0x86, 0x75];

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
        return hash.SequenceEqual(super_metroid_us);
    }

    public ISave Export(IMemoryAccess romAcess)
    {
        throw new System.NotImplementedException();
    }

    public void SetupGameTemporaryPatches(IMemoryAccess romAccess)
    {
    }

    public SuperMetroidTesting GetTesting(IEditor editorInterface, IMemoryAccess rom)
    {
        return new SuperMetroidTesting(editorInterface, rom);
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

public class SuperMetroidTesting : IUserWindow, IImage
{
    private readonly IEditor _editorInterface;
    private readonly IMemoryAccess _rom;

    public uint Width => 512;

    public uint Height => 512;

    public float ScaleX => 2.0f;

    public float ScaleY => 2.0f;

    public float UpdateInterval => 1 / 30.0f;

    public SuperMetroidTesting(IEditor editorInterface, IMemoryAccess rom)
    {
        _editorInterface = editorInterface;
        _rom = rom;
    }

    struct RoomHeader
    {
        public byte RoomIndex;
        public byte RoomArea;
        public byte XPosMiniMap;
        public byte YPosMiniMap;
        public byte Width;
        public byte Height;
        public byte UpScroll;
        public byte DownScroll;
        public byte CREBitSet;
        public ushort DoorListPointer;      // Bank 8F

        public RoomHeader(IMemoryAccess rom, ReadOnlySpan<byte> data)
        {
            if (data.Length < 11)
                throw new ArgumentException("Data must be at least 11 bytes long", nameof(data));

            RoomIndex = data[0];
            RoomArea = data[1];
            XPosMiniMap = data[2];
            YPosMiniMap = data[3];
            Width = data[4];
            Height = data[5];
            UpScroll = data[6];
            DownScroll = data[7];
            CREBitSet = data[8];
            DoorListPointer = rom.FetchMachineOrder16(9, data);
        }
    }

    struct StateHeader
    {
        public ushort LevelDataPointer;
        public byte LevelDataBank;
        public byte TileSet;
        public byte MusicCollection;
        public byte MusicTrack;
        public ushort FXPointer;                // Bank 83
        public ushort EnemyPopulationPointer;   // Bank A1
        public ushort EnemySet;                 // Bank B4
        public byte Layer2ScrollX;              // sssssssb
        public byte Layer2ScrollY;              // sssssssb
        public ushort ScrollPointer;        // Optional (which 16x16 blocks are scollable)
        public ushort SpecialXRayBlocks;    // Optional (pointer to data defining special X-Ray blocks)
        public ushort MainASMPointer;       // Optional (per frame asm)
        public ushort PLMPopulation;        // Optional (PLM placement and params)
        public ushort LibraryBG;            // Optional (operations for loading layer 2)
        public ushort SetupAsmPointer;      // Optional (per room setup asm)

        public StateHeader(IMemoryAccess rom, ReadOnlySpan<byte> data)
        {
            if (data.Length < 26)
                throw new ArgumentException("Data must be at least 26 bytes long", nameof(data));

            LevelDataPointer = rom.FetchMachineOrder16(0, data);
            LevelDataBank = data[2];
            TileSet = data[3];
            MusicCollection = data[4];
            MusicTrack = data[5];
            FXPointer = rom.FetchMachineOrder16(6, data);
            EnemyPopulationPointer = rom.FetchMachineOrder16(8, data);
            EnemySet = rom.FetchMachineOrder16(10, data);
            Layer2ScrollX = data[12];
            Layer2ScrollY = data[13];
            ScrollPointer = rom.FetchMachineOrder16(14, data);
            SpecialXRayBlocks = rom.FetchMachineOrder16(16, data);
            MainASMPointer = rom.FetchMachineOrder16(18, data);
            PLMPopulation = rom.FetchMachineOrder16(20, data);
            LibraryBG = rom.FetchMachineOrder16(22, data);
            SetupAsmPointer = rom.FetchMachineOrder16(24, data);
        }

        public uint LevelDataAddress => (uint)(LevelDataPointer | (LevelDataBank << 16));
    }

    public ReadOnlySpan<Pixel> GetImageData(float seconds)
    {
        RetroEditorPlugin_SuperMetroid.LoRom addressTranslation = new();

        var roomAddress = 0x793FEu;
        var snesAddress = addressTranslation.ToImage(roomAddress);

        var roomProperties = _rom.ReadBytes(ReadKind.Rom, roomAddress, 41);

        var roomHeader = new RoomHeader(_rom, roomProperties[0..11]);
        var stateCondition = _rom.FetchMachineOrder16(11, roomProperties);
        if (stateCondition != 0xE5E6) // DEFAULT
        {
            _editorInterface.Log(LogType.Error, $"Unexpected state condition: {stateCondition:X4} at address {snesAddress:X6}");
            return new Pixel[Width * Height];
        }
        var stateHeader = new StateHeader(_rom, roomProperties[13..]);

        var levelDataAddress = stateHeader.LevelDataAddress;
        var levelRomLocation = addressTranslation.ToImage(levelDataAddress);

        var levelData = _rom.ReadBytes(ReadKind.Rom, levelRomLocation, 32768); // Read 32KB of level data

        var decompressedData = new byte[0x10000];

        // Decompress the level data
        var decompSize = RetroEditorPlugin_SuperMetroid.LC_LZ5.Decompress(ref decompressedData, levelData, out var bytesRead);

        var vram = new SuperMetroidVRam(_rom);

        var pixels = new Pixel[Width * Height];
        for (int i = 0; i < vram.TileCount; i++)
        {
            var tx = i % 64; 
            var ty = i / 64;
            var rasteriseTile = vram.Tile(i);

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    var colourIndex = rasteriseTile[y * 8 + x];
                    var pixelIndex = (ty * 8 + y) * Width + (tx * 8 + x);
//                    if (pixelIndex < pixels.Length)
                    {
                        pixels[pixelIndex] = new Pixel((byte)(colourIndex * 8), (byte)(colourIndex * 8), (byte)(colourIndex * 8));
                    }
                }
            }
        }
        return pixels;
    }

    public void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls)
    {
        widget.AddImageView(this);
    }

    public void OnClose()
    {
    }
}

// TOOD Merge with SuperMario one
public class SuperMetroidVRam
{
    public ReadOnlySpan<byte> Tile(int tile)
    {
        return tileCache[tile];
    }

    public uint TileCount => (uint)tileCache.Length;

    private IMemoryAccess _rom;
    private RetroEditorPlugin_SuperMetroid.AddressTranslation _addressTranslation;

    private byte[] decomp = new byte[0x10000]; // 64KB buffer for decompression

    private byte[][] tileCache = new byte[65536/32][];

    // TODO needs information from level for vram layout
    public SuperMetroidVRam(IMemoryAccess rom)
    {
        _rom = rom;
        _addressTranslation = new RetroEditorPlugin_SuperMetroid.LoRom();

        for (int i = 0; i < tileCache.Length; i++)
        {
            tileCache[i] = new byte[8 * 8];
        }

        // Step 1, fetch CRE and CRE tiletables (we will deal with SRE after)
        var creTilesetAddress = 0xB98000u;
        var creRomLocation = _addressTranslation.ToImage(creTilesetAddress);

        var creTileset = _rom.ReadBytes(ReadKind.Rom, creRomLocation, 32768); // Read 32KB of CRE tileset

        // Decompress the CRE tileset
        var creDecompSize = RetroEditorPlugin_SuperMetroid.LC_LZ5.Decompress(ref decomp, creTileset, out var creBytesRead);

        var totalCRE = (uint)(creBytesRead / 32);

        RasteriseDecomp(0x280, 0, totalCRE, SNESTileKind.Tile_4bpp);

        // Step 2, fetch SRE tileset
        var sreRomLocation = 0x1D4629u;
        var sreTileset = _rom.ReadBytes(ReadKind.Rom, sreRomLocation, 32768); // Read 32KB of CRE tileset

        // Decompress the CRE tileset
        var sreDecompSize = RetroEditorPlugin_SuperMetroid.LC_LZ5.Decompress(ref decomp, sreTileset, out var sreBytesRead);

        var totalSRE = (uint)(sreBytesRead / 32);

        // CRE at 2800 (word address) (a tile is 32 bytes (4BBP), so 16 words. So to convert to a tile offset) - 0x2800/16 = 280

        RasteriseDecomp(0x000, 0, totalSRE, SNESTileKind.Tile_4bpp);

    }

    enum SNESTileKind
    {
        Tile_2bpp,
        Tile_3bpp,
        Tile_4bpp,
        Tile_3bppMode7,
    }

    private void RasteriseDecomp(uint offset, int tileOffset, uint tileCount, SNESTileKind rasteriseAs)
    {
        var GFX = decomp.AsSpan();
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
                break;
        }
        for (int i = tileOffset; i < tileOffset + tileCount; i++)
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
        }
    }
    public class SuperMetroidPalette
    {
        Pixel[,] _palette;

        Pixel SNESToPixel(ushort c)
        {
            var r = ((c & 0x1F) << 3) | ((c & 1) != 0 ? 7 : 0);
            var g = (((c >> 5) & 0x1F) << 3) | ((c & 0x20) != 0 ? 7 : 0);
            var b = (((c >> 10) & 0x1F) << 3) | ((c & 0x400) != 0 ? 7 : 0);
            return new Pixel((byte)r, (byte)g, (byte)b, 255);
        }

        public SuperMetroidPalette(IMemoryAccess rom)
        {
            _palette = new Pixel[16, 16];  // Perhaps we should have platform colour constructors e.g. SNESToPixel, etc?

            // For now greyscale
            for (int i = 0; i < 16; i++)
            {
                for (int j=0;j<16;j++)
                {
                    _palette[i, j] = SNESToPixel((ushort)(i * 0x1111 + j * 0x0001)); // Greyscale
                }
            }

        }

        public Pixel this[int palette, int colour]
        {
            get
            {
                return _palette[palette, colour];
            }
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


    static class SuperMetroidRenderHelpers
    {
        static void Draw8x8(int tx, int ty, int xo, int yo, SubTile tile, SuperMetroidVRam vram, ref Pixel[] pixels, uint Width, uint Height, SuperMetroidPalette palette)
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

        public static void DrawGfxTile(int tx, int ty, Tile16x16 tile16, SuperMetroidVRam vram, ref Pixel[] pixels, uint Width, uint Height, SuperMetroidPalette palette)
        {
            // Just draw the TL tile for now
            Draw8x8(tx, ty, 0, 0, tile16.TL, vram, ref pixels, Width, Height, palette);
            Draw8x8(tx, ty, 1, 0, tile16.TR, vram, ref pixels, Width, Height, palette);
            Draw8x8(tx, ty, 0, 1, tile16.BL, vram, ref pixels, Width, Height, palette);
            Draw8x8(tx, ty, 1, 1, tile16.BR, vram, ref pixels, Width, Height, palette);
        }

    }

}