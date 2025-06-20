using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using RetroEditor.Plugins;

using SuperNintendoEntertainmentSystem.Graphics;
using SuperNintendoEntertainmentSystem.Memory;
using SuperNintendoEntertainmentSystem.Compression;

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
        menu.AddItem("Room",
            (editorInterface, menuItem) =>
            {
                var room = new SuperMetroidRoom(rom);
                editorInterface.OpenUserWindow($"Room Editor", room);
            });
    }
}


// WE NEED 2 THINGS DOING SOON
// 1. ROM PLUGINS EXIST, GAME PLUGINS EXIST, we need a shared helpers PLUGINS - E.G. SNES Helpers that contain Map16 stuff etc...
// 2. WE NEED A WAY TO SHARE DATA BETWEEN WINDOWS/WIDGETS (although in principle data could be shared via the main plugin)
//    e.g. the current room, the tile palette for that room, the tilemap for that room, should be seperate windows .. 
public class SuperMetroidRoom : ITilePalette, ITileMap, IUserWindow
{
    public float UpdateInterval => 1 / 30.0f;

    public uint Width => 256 * 8;

    public uint Height => 256 * 6;

    public uint NumLayers => 1;

    public float ScaleX => 1.0f;

    public float ScaleY => 1.0f;

    public uint MaxTiles => TileCount;

    public int SelectedTile { get; set; }

    public uint TilesPerRow => 32;

    TilePaletteStore palette;
    SuperMetroidTile[] tiles;

    class SuperMetroidTile : ITile
    {
        public uint Width => 16;

        public uint Height => 16;

        public string Name => _name;

        private string _name;
        private Pixel[] _imageData;

        public SuperMetroidTile(string name, Tile16x16 map16, SuperMetroidVRam vram, SuperMetroidPalette palette)
        {
            _name = name;
            _imageData = new Pixel[16 * 16];

            // Rasterise the tiles
            SuperMetroidRenderHelpers.DrawGfxTile(0, 0, map16, false, false, vram, ref _imageData, 16, 16, palette);
        }
        public Pixel[] GetImageData()
        {
            return _imageData;
        }
    }

    class SuperMetroidLayer : ILayerWithFlip
    {
        private uint _width,_height;
        public uint Width => _width;

        public uint Height => _height;

        private uint[] map;
        private FlipState[] flips;

        public ReadOnlySpan<uint> GetMapData()
        {
            return map.AsSpan();
        }

        public ReadOnlySpan<FlipState> GetFlipData()
        {
            return flips.AsSpan();
        }

        public void SetTile(uint x, uint y, uint tile)
        {
            // TODO
        }

        public void SetFlip(uint x, uint y, FlipState state)
        {
            // TODO
        }

        public SuperMetroidLayer(IMemoryAccess rom, int w, int h, ReadOnlySpan<byte> mapData)
        {
            _width = (uint)w;
            _height = (uint)h;
            map = new uint[w * h];
            flips = new FlipState[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var blockIndex = (int)(y * w + x);
                    var block = mapData.Slice(blockIndex * 2, 2);

                    var layer1Data = rom.FetchMachineOrder16(0, block);
                    var type = layer1Data >> 12; // Tile type
                    var yFlip = (layer1Data >> 11) & 0x01; // Y Flip
                    var xFlip = (layer1Data >> 10) & 0x01; // Y Flip
                    var tileIndex = layer1Data & 0x3FF; // Tile index

                    map[blockIndex] = (uint)tileIndex;
                    flips[blockIndex] = FlipState.None;
                    if (xFlip == 1)
                    {
                        flips[blockIndex] |= FlipState.X;
                    }
                    if (yFlip == 1)
                    {
                        flips[blockIndex] |= FlipState.Y;
                    }
                }
            }
        }
    }

    private SuperMetroidLayer layer1;

    public void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls)
    {
        widget.AddLabel("Tile Palette");
        widget.AddTilePaletteWidget(palette);
        widget.AddLabel("Tile Map Editor");
        widget.AddTileMapWidget(this);
    }

    public void OnClose()
    {
    }

    public ILayer FetchLayer(uint layer)
    {
        return layer1;
    }

    public TilePaletteStore FetchPalette(uint layer)
    {
        return palette;
    }

    public void Update(float seconds)
    {
    }

    public ReadOnlySpan<ITile> FetchTiles()
    {
        return tiles;
    }

    private const uint TileCount = 1024;

    public SuperMetroidRoom(IMemoryAccess rom)
    {
        LoRom addressTranslation = new(isHeadered: false, bank80: true);

        var roomAddress = 0x7E82Cu; //0x793FEu;
        var snesAddress = addressTranslation.ToImage(roomAddress);

        var roomProperties = rom.ReadBytes(ReadKind.Rom, roomAddress, 41);

        var roomHeader = new RoomHeader(rom, roomProperties[0..11]);
        var stateCondition = rom.FetchMachineOrder16(11, roomProperties);
        if (stateCondition != 0xE5E6) // DEFAULT
        {
            // TODO: Handle unexpected state condition
        }
        var stateHeader = new StateHeader(rom, roomProperties[13..]);

        var levelDataAddress = stateHeader.LevelDataAddress;
        var tileset = stateHeader.TileSet;
        var levelRomLocation = addressTranslation.ToImage(levelDataAddress);

        var levelData = rom.ReadBytes(ReadKind.Rom, levelRomLocation, 32768); // Read 32KB of level data

        var decompressedData = new byte[0x10000];

        // Decompress the level data
        var decompSize = LC_LZ5.Decompress(ref decompressedData, levelData, out var bytesRead);

        var vram = new SuperMetroidVRam(rom, tileset);

        tiles = new SuperMetroidTile[TileCount];
        for (int a = 0; a < TileCount; a++)
        {
            var tile = vram.Map16[a];
            if (vram.Map16.Has(a))
            {
                var name = $"Tile {a}";
                tiles[a] = new SuperMetroidTile(name, tile, vram, vram.Palette);
            }
        }

        palette = new TilePaletteStore(this);

        var layer1Size = rom.FetchMachineOrder16(0, decompressedData);
        var numBlocks = layer1Size / 2;
        var blockData = decompressedData.AsSpan(2, numBlocks * 2);
        var btsData = decompressedData.AsSpan(2 + numBlocks * 2, numBlocks);
        var layer2Data = decompressedData.AsSpan(2 + numBlocks * 3, numBlocks * 2); // TODO make sure we have layer 2 data first :)


        layer1 = new SuperMetroidLayer(rom, roomHeader.Width * 16, roomHeader.Height * 16, blockData);
    }
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


public class SuperMetroidTesting : IUserWindow, IImage
{
    private readonly IEditor _editorInterface;
    private readonly IMemoryAccess _rom;

    public uint Width => 256 * 8;

    public uint Height => 256 * 6;

    public float ScaleX => 1.0f;

    public float ScaleY => 1.0f;

    public float UpdateInterval => 1 / 30.0f;

    public SuperMetroidTesting(IEditor editorInterface, IMemoryAccess rom)
    {
        _editorInterface = editorInterface;
        _rom = rom;
    }

    public ReadOnlySpan<Pixel> GetImageData(float seconds)
    {
        LoRom addressTranslation = new(isHeadered: false, bank80: true);

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
        var tileset = stateHeader.TileSet;
        var levelRomLocation = addressTranslation.ToImage(levelDataAddress);

        var levelData = _rom.ReadBytes(ReadKind.Rom, levelRomLocation, 32768); // Read 32KB of level data

        var decompressedData = new byte[0x10000];

        // Decompress the level data
        var decompSize = LC_LZ5.Decompress(ref decompressedData, levelData, out var bytesRead);

        var vram = new SuperMetroidVRam(_rom, tileset);

        var pixels = new Pixel[Width * Height];
        /*
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

                for (int a = 0; a < 512; a++)
                {
                    var tile = vram.Map16[a];
                    if (vram.Map16.Has(a))
                    {
                        SuperMetroidRenderHelpers.DrawGfxTile(a % 32, a / 32 + 256 / 16, tile, false, false, vram, ref pixels, Width, Height, vram.Palette);
                    }
                }*/

        // Lets unpack the level data using our newly working tiles
        var layer1Size = _rom.FetchMachineOrder16(0, decompressedData);
        var numBlocks = layer1Size / 2;
        var blockData = decompressedData.AsSpan(2, numBlocks * 2);
        var btsData = decompressedData.AsSpan(2 + numBlocks * 2, numBlocks);
        var layer2Data = decompressedData.AsSpan(2 + numBlocks * 3, numBlocks * 2); // TODO make sure we have layer 2 data first :)

        var mapWidth = roomHeader.Width * 16;
        var mapHeight = roomHeader.Height * 16;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                var blockIndex = y * mapWidth + x;
                if (blockIndex < numBlocks)
                {
                    var block = blockData.Slice(blockIndex * 2, 2);
                    var bts = btsData[blockIndex];
                    var layer2Block = layer2Data.Slice(blockIndex * 2, 2);


                    // Layer2
                    var layer2DataWord = _rom.FetchMachineOrder16(0, layer2Block);
                    var layer2Type = layer2DataWord >> 12; // Tile type
                    var layer2YFlip = (layer2DataWord >> 11) & 0x01; // Y Flip
                    var layer2XFlip = (layer2DataWord >> 10) & 0x01; // X Flip
                    var layer2TileIndex = layer2DataWord & 0x3FF; // Tile index

                    if (vram.Map16.Has(layer2TileIndex))
                    {
                        var tile = vram.Map16[layer2TileIndex];
                        SuperMetroidRenderHelpers.DrawGfxTile(x, y, tile, layer2XFlip == 1, layer2YFlip == 1, vram, ref pixels, Width, Height, vram.Palette);
                    }

                    // Get the tile index from the block data
                    var layer1Data = _rom.FetchMachineOrder16(0, block);
                    var type = layer1Data >> 12; // Tile type
                    var yFlip = (layer1Data >> 11) & 0x01; // Y Flip
                    var xFlip = (layer1Data >> 10) & 0x01; // Y Flip
                    var tileIndex = layer1Data & 0x3FF; // Tile index

                    if (vram.Map16.Has(tileIndex))
                    {
                        var tile = vram.Map16[tileIndex];
                        SuperMetroidRenderHelpers.DrawGfxTile(x, y, tile, xFlip == 1, yFlip == 1, vram, ref pixels, Width, Height, vram.Palette);
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

    public SuperMetroidMap16 Map16 => _map16;
    public SuperMetroidPalette Palette => _palette;

    private IMemoryAccess _rom;
    private AddressTranslation _addressTranslation;
    private SuperMetroidMap16 _map16 = new SuperMetroidMap16();
    private SuperMetroidPalette _palette;

    private byte[] decomp = new byte[0x10000]; // 64KB buffer for decompression

    private byte[][] tileCache = new byte[65536 / 32][];

    // TODO needs information from level for vram layout
    public SuperMetroidVRam(IMemoryAccess rom, byte tileset)
    {
        _rom = rom;
        _addressTranslation = new LoRom(isHeadered: false, bank80: true);

        for (int i = 0; i < tileCache.Length; i++)
        {
            tileCache[i] = new byte[8 * 8];
        }

        // Step 1, fetch CRE and CRE tiletables (we will deal with SRE after)
        var creTilesetAddress = 0xB98000u;
        var creRomLocation = _addressTranslation.ToImage(creTilesetAddress);

        var creTileset = _rom.ReadBytes(ReadKind.Rom, creRomLocation, 32768); // Read 32KB of CRE tileset

        // Decompress the CRE tileset
        var creDecompSize = LC_LZ5.Decompress(ref decomp, creTileset, out _);

        var totalCRE = (uint)(creDecompSize / 32);

        RasteriseDecomp(0x280, 0, totalCRE, SNESTileKind.Tile_4bpp);

        // Fetch tileset table data
        var tilesetTableAddress = 0x8FE6A2u;
        var tilesetTableRomLocation = _addressTranslation.ToImage(tilesetTableAddress);

        // Table is 3*3 bytes per entry, there are entries for 0x00-0x1C
        var tilesetOffset = tileset * 3u * 3u;
        var tilesetEntry = _rom.ReadBytes(ReadKind.Rom, tilesetTableRomLocation + tilesetOffset, 3 * 3);

        var tileTableWord = _rom.FetchMachineOrder16(0, tilesetEntry);
        var tileTableBank = tilesetEntry[2];
        var tileTableAddress = (uint)(tileTableWord | (tileTableBank << 16));
        var tilesWord = _rom.FetchMachineOrder16(3, tilesetEntry);
        var TilesBank = tilesetEntry[5];
        var tilesAddress = (uint)(tilesWord | (TilesBank << 16));
        var paletteWord = _rom.FetchMachineOrder16(6, tilesetEntry);
        var paletteBank = tilesetEntry[8];
        var paletteAddress = (uint)(paletteWord | (paletteBank << 16));

        var paletteRomLocation = _addressTranslation.ToImage(paletteAddress);
        _palette = new SuperMetroidPalette(_rom, paletteRomLocation);

        // Step 2, fetch SRE tileset
        var sreRomLocation = _addressTranslation.ToImage(tilesAddress);//   0x1D4629u;
        var sreTileset = _rom.ReadBytes(ReadKind.Rom, sreRomLocation, 32768); // Read 32KB of CRE tileset

        // Decompress the CRE tileset
        var sreDecompSize = LC_LZ5.Decompress(ref decomp, sreTileset, out _);

        var totalSRE = (uint)(sreDecompSize / 32);

        // CRE at 2800 (word address) (a tile is 32 bytes (4BBP), so 16 words. So to convert to a tile offset) - 0x2800/16 = 280

        RasteriseDecomp(0x000, 0, totalSRE, SNESTileKind.Tile_4bpp);

        var creTile16Address = 0xB9A09Du;
        var creTile16RomLocation = _addressTranslation.ToImage(creTile16Address);
        var creTile16Data = _rom.ReadBytes(ReadKind.Rom, creTile16RomLocation, 32768);

        // Decompress the CRE tile16 data
        var creTile16DecompSize = LC_LZ5.Decompress(ref decomp, creTile16Data, out _);

        _map16.AddTiles(0, (creTile16DecompSize / 8) - 1, decomp.AsSpan(0, creTile16DecompSize));

        var sreTile16RomLocation = _addressTranslation.ToImage(tileTableAddress);
        var sreTile16Data = _rom.ReadBytes(ReadKind.Rom, sreTile16RomLocation, 32768);

        var sreTile16DecompSize = LC_LZ5.Decompress(ref decomp, sreTile16Data, out _);

        _map16.AddTiles(creTile16DecompSize / 8, (creTile16DecompSize / 8) + ((sreTile16DecompSize / 8) - 1), decomp.AsSpan(0, sreTile16DecompSize));
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
}
public class SuperMetroidPalette
{
    Pixel[,] _palette;

    public SuperMetroidPalette(IMemoryAccess rom, uint paletteRomLocation)
    {
        _palette = new Pixel[8, 16];

        // Read the palette data from the ROM
        var paletteComp = rom.ReadBytes(ReadKind.Rom, paletteRomLocation, 32768);
        var paletteBuffer = new byte[2 * 8 * 16];
        // Decompress the palette data
        var paletteDecompSize = LC_LZ5.Decompress(ref paletteBuffer, paletteComp , out _);
        if (paletteDecompSize != 256)
        {
            throw new InvalidOperationException($"Palette decompression failed, expected 512 bytes but got {paletteDecompSize} bytes.");
        }

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 16; j++)
            {
                var index = i * 16 + j;
                if (index < paletteBuffer.Length / 2)
                {
                    var colourValue = (ushort)(paletteBuffer[index * 2] | (paletteBuffer[index * 2 + 1] << 8));
                    _palette[i, j].FromSNES(colourValue);
                }
                else
                {
                    _palette[i, j] = new Pixel(0, 0, 0, 255); // Default to black if out of range
                }
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

public struct SubTile
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

public struct Tile16x16
{
    public SubTile TL, TR, BL, BR;

    public Tile16x16(ReadOnlySpan<byte> data)
    {
        TL = new SubTile(data[0], data[1]);
        TR = new SubTile(data[2], data[3]);
        BL = new SubTile(data[4], data[5]);
        BR = new SubTile(data[6], data[7]);
    }
}

// Same as SuperMarioWorld, but tileflipping is overridable, and the tile order is TL,TR,BL,BR as opposed to TL,BL,TR,BR
public static class SuperMetroidRenderHelpers
{
    static void Draw8x8(int tx, int ty, int xo, int yo, SubTile tile, bool flipX, bool flipY, SuperMetroidVRam vram, ref Pixel[] pixels, uint Width, uint Height, SuperMetroidPalette palette)
    {
        var tileNum = tile.tile;
        var gfx = vram.Tile(tileNum);
        // compute tile position TL
        var tileX = tileNum % 16;
        var tileY = tileNum / 16;

        int ox = tx * 16 + 8 * xo;
        int oy = ty * 16 + 8 * yo;
        int g = 0;

        var doFlipX = tile.flipx ^ flipX;
        var doFlipY = tile.flipy ^ flipY;

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                var px = ox + (doFlipX ? 7 - x : x);
                var py = oy + (doFlipY ? 7 - y : y);
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

    public static void DrawGfxTile(int tx, int ty, Tile16x16 tile16, bool xFlip, bool yFlip, SuperMetroidVRam vram, ref Pixel[] pixels, uint Width, uint Height, SuperMetroidPalette palette)
    {
        if ((!xFlip) && (!yFlip))
        {
            // No flipping, draw normally
            Draw8x8(tx, ty, 0, 0, tile16.TL, xFlip, yFlip, vram, ref pixels, Width, Height, palette);
            Draw8x8(tx, ty, 1, 0, tile16.TR, xFlip, yFlip, vram, ref pixels, Width, Height, palette);
            Draw8x8(tx, ty, 0, 1, tile16.BL, xFlip, yFlip, vram, ref pixels, Width, Height, palette);
            Draw8x8(tx, ty, 1, 1, tile16.BR, xFlip, yFlip, vram, ref pixels, Width, Height, palette);
        }
        else if (xFlip && (!yFlip))
        {
            // X flip only
            Draw8x8(tx, ty, 0, 0, tile16.TR, xFlip, yFlip, vram, ref pixels, Width, Height, palette);
            Draw8x8(tx, ty, 1, 0, tile16.TL, xFlip, yFlip, vram, ref pixels, Width, Height, palette);
            Draw8x8(tx, ty, 0, 1, tile16.BR, xFlip, yFlip, vram, ref pixels, Width, Height, palette);
            Draw8x8(tx, ty, 1, 1, tile16.BL, xFlip, yFlip, vram, ref pixels, Width, Height, palette);
        }
        else if ((!xFlip) && yFlip)
        {
            // Y flip only
            Draw8x8(tx, ty, 0, 0, tile16.BL, xFlip, yFlip, vram, ref pixels, Width, Height, palette);
            Draw8x8(tx, ty, 1, 0, tile16.BR, xFlip, yFlip, vram, ref pixels, Width, Height, palette);
            Draw8x8(tx, ty, 0, 1, tile16.TL, xFlip, yFlip, vram, ref pixels, Width, Height, palette);
            Draw8x8(tx, ty, 1, 1, tile16.TR, xFlip, yFlip, vram, ref pixels, Width, Height, palette);
        }
        else
        {
            // Both flips
            Draw8x8(tx, ty, 0, 0, tile16.BR, xFlip, yFlip, vram, ref pixels, Width, Height, palette);
            Draw8x8(tx, ty, 1, 0, tile16.BL, xFlip, yFlip, vram, ref pixels, Width, Height, palette);
            Draw8x8(tx, ty, 0, 1, tile16.TR, xFlip, yFlip, vram, ref pixels, Width, Height, palette);
            Draw8x8(tx, ty, 1, 1, tile16.TL, xFlip, yFlip, vram, ref pixels, Width, Height, palette);
        }
    }

}

public class SuperMetroidMap16
{
    public bool Has(int index) => map16ToTile.ContainsKey(index);
    public Tile16x16 this[int index] => map16ToTile[index];
    private Dictionary<int, Tile16x16> map16ToTile = new Dictionary<int, Tile16x16>();

    public void AddTiles(int start, int end, ReadOnlySpan<byte> data)
    {
        for (int a = start; a <= end; a++)
        {
            var Test2D = data.Slice((a - start) * 8, 8);
            map16ToTile[a] = new Tile16x16(Test2D);
        }
    }

    public SuperMetroidMap16()
    {
    }
}