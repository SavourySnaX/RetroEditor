using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using RetroEditor.Plugins;

using SuperNintendoEntertainmentSystem.Graphics;
using SuperNintendoEntertainmentSystem.Compression;
using SuperNintendoEntertainmentSystem.Memory;

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

    public LinkToThePastOverworld GetOverworld(IEditor editorInterface, IMemoryAccess rom)
    {
        return new LinkToThePastOverworld(editorInterface, rom);
    }

    public void ConfigureMenu(IMemoryAccess rom, IMenu menu)
    {
        menu.AddItem("Testing",
            (editorInterface, menuItem) =>
            {
                editorInterface.OpenUserWindow($"Testing", GetTesting(editorInterface, rom));
            });
        menu.AddItem("Overworld",
            (editorInterface, menuItem) =>
            {
                editorInterface.OpenUserWindow($"Overworld", GetOverworld(editorInterface, rom));
            });
    }
}

public class LinkToThePastTesting : IUserWindow, IImage
{
    private readonly IEditor _editorInterface;
    private readonly IMemoryAccess _rom;

    public uint Width => 512;

    public uint Height => 512;

    public float ScaleX => 2.0f;

    public float ScaleY => 2.0f;

    public float UpdateInterval => 1 / 30.0f;

    public LinkToThePastTesting(IEditor editorInterface, IMemoryAccess rom)
    {
        _editorInterface = editorInterface;
        _rom = rom;
    }

    uint GetGFXAddress(AddressTranslation addr, uint page)
    {
        var data = _rom.ReadBytes(ReadKind.Rom, 0x6790, 16);

        var gfxPointer1 = addr.ToImage(_rom.FetchMachineOrder16(0x00, data));
        var gfxPointer2 = addr.ToImage(_rom.FetchMachineOrder16(0x05, data));
        var gfxPointer3 = addr.ToImage(_rom.FetchMachineOrder16(0x0A, data));

        var bank = _rom.ReadBytes(ReadKind.Rom, gfxPointer1 + page, 1)[0];
        var high = _rom.ReadBytes(ReadKind.Rom, gfxPointer2 + page, 1)[0];
        var low = _rom.ReadBytes(ReadKind.Rom, gfxPointer3 + page, 1)[0];

        return (uint)((bank << 16) | (high << 8) | low);
    }

    public void DrawPage2BPP(ref Pixel[] pixels, ReadOnlySpan<byte> gfx3bpp, int offsX, int offsY)
    {
        var numTiles = 64;
        for (int i = 0; i < numTiles; i++)
        {
            var tx = (i % 256) % 16;
            var ty = (i % 256) / 16;
            var tzx = offsX;
            var tzy = offsY;
            ReadOnlySpan<byte> tile;
            tile = gfx3bpp.Slice(i * 16, 16);
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    var bp0 = tile[y * 2 + 0];
                    var bp1 = tile[y * 2 + 1];
                    var bit = 7 - x;
                    var colour = ((bp0 >> bit) & 1) | (((bp1 >> bit) & 1) << 1);
                    pixels[tzx + tzy + (ty * 8 + y) * Width + tx * 8 + x] = new Pixel((byte)(colour * 64), (byte)(colour * 64), (byte)(colour * 64), 255);
                }
            }
        }
    }

    public void DrawPage3BPP(ref Pixel[] pixels, ReadOnlySpan<byte> gfx3bpp, int offsX, int offsY)
    {
        var numTiles = 64;
        for (int i = 0; i < numTiles; i++)
        {
            var tx = (i % 256) % 16;
            var ty = (i % 256) / 16;
            var tzx = offsX;
            var tzy = offsY;
            ReadOnlySpan<byte> tile;
            tile = gfx3bpp.Slice(i * 24, 24);
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    var bp0 = tile[y * 2 + 0];
                    var bp1 = tile[y * 2 + 1];
                    var bp2 = tile[16 + y];
                    var bit = 7 - x;
                    var colour = ((bp0 >> bit) & 1) | (((bp1 >> bit) & 1) << 1) | (((bp2 >> bit) & 1) << 2);
                    pixels[tzx + tzy + (ty * 8 + y) * Width + tx * 8 + x] = new Pixel((byte)(colour * 32), (byte)(colour * 32), (byte)(colour * 32), 255);
                }
            }
        }
    }

    public ReadOnlySpan<Pixel> GetImageData(float seconds)
    {
        var lorom = new LoRom(false, false);

        // Sheet 73 - 7E  (3bpp) (uncompressed)
        //var gfx3bppAddress = 0x87000u;
        //var length = 0x8B800u - gfx3bppAddress;
        //       var gfx3bpp = _rom.ReadBytes(ReadKind.Rom, gfx3bppAddress, length);

        // 0-112 COMP 3BPP
        // 113-114 2BPP sprites
        // 115-126 3BPP sprites
        // 127-217 COMP 3BPP sprites

        var pixels = new Pixel[Width * Height];

        uint start = 64;
        uint end = Math.Min(128u,0xCE);

        for (uint a = start; a < end; a++)
        {
            var gfx3bppAddress = GetGFXAddress(lorom, a);
            var gfxRomLoc = lorom.ToImage(gfx3bppAddress);
            var toDecomp = _rom.ReadBytes(ReadKind.Rom, gfxRomLoc, 32768);
            ReadOnlySpan<byte> gfx3bpp;
            var buffer = new byte[32768];
            uint length;

            if (a >= 0x73 && a <= 0x7E)
            {
                length = 0x600;
                gfx3bpp = toDecomp;
            }
            else
            {
                length = (uint)LC_LZ2.Decompress(ref buffer, toDecomp, out var bytesRead);
                gfx3bpp = buffer.AsSpan(0, (int)length);
            }

            var numTiles = length / 24;
            var offsX = ((a - start) % 4) * 128;
            var offsY = ((a - start) / 4) * 32 * Width;

            if (a >= 113 && a <= 114)
            {
                numTiles = length / 16;
                DrawPage2BPP(ref pixels, gfx3bpp, (int)offsX, (int)offsY); // Draw first page at (0,0)
            }
            else
            {
                DrawPage3BPP(ref pixels, gfx3bpp, (int)offsX, (int)offsY); // Draw first page at (0,0)
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

public class LinkToThePastOverworld : IUserWindow, IImage
{
    private readonly IEditor _editorInterface;
    private readonly IMemoryAccess _rom;

    public uint Width => 512;

    public uint Height => 512;

    public float ScaleX => 2.0f;

    public float ScaleY => 2.0f;

    public float UpdateInterval => 1 / 30.0f;

    public LinkToThePastOverworld(IEditor editorInterface, IMemoryAccess rom)
    {
        _editorInterface = editorInterface;
        _rom = rom;
    }

    uint GetGFXAddress(AddressTranslation addr, uint page)
    {
        var data = _rom.ReadBytes(ReadKind.Rom, 0x6790, 16);

        var gfxPointer1 = addr.ToImage(_rom.FetchMachineOrder16(0x00, data));
        var gfxPointer2 = addr.ToImage(_rom.FetchMachineOrder16(0x05, data));
        var gfxPointer3 = addr.ToImage(_rom.FetchMachineOrder16(0x0A, data));

        var bank = _rom.ReadBytes(ReadKind.Rom, gfxPointer1 + page, 1)[0];
        var high = _rom.ReadBytes(ReadKind.Rom, gfxPointer2 + page, 1)[0];
        var low = _rom.ReadBytes(ReadKind.Rom, gfxPointer3 + page, 1)[0];

        return (uint)((bank << 16) | (high << 8) | low);
    }

    public void DrawPage2BPP(ref Pixel[] pixels, ReadOnlySpan<byte> gfx3bpp, int offsX, int offsY)
    {
        var numTiles = 64;
        for (int i = 0; i < numTiles; i++)
        {
            var tx = (i % 256) % 16;
            var ty = (i % 256) / 16;
            var tzx = offsX;
            var tzy = offsY;
            ReadOnlySpan<byte> tile;
            tile = gfx3bpp.Slice(i * 16, 16);
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    var bp0 = tile[y * 2 + 0];
                    var bp1 = tile[y * 2 + 1];
                    var bit = 7 - x;
                    var colour = ((bp0 >> bit) & 1) | (((bp1 >> bit) & 1) << 1);
                    pixels[tzx + tzy + (ty * 8 + y) * Width + tx * 8 + x] = new Pixel((byte)(colour * 64), (byte)(colour * 64), (byte)(colour * 64), 255);
                }
            }
        }
    }

    public void DrawPage3BPP(ref Pixel[] pixels, ReadOnlySpan<byte> gfx3bpp, int offsX, int offsY)
    {
        var numTiles = 64;
        for (int i = 0; i < numTiles; i++)
        {
            var tx = (i % 256) % 16;
            var ty = (i % 256) / 16;
            var tzx = offsX;
            var tzy = offsY;
            ReadOnlySpan<byte> tile;
            tile = gfx3bpp.Slice(i * 24, 24);
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    var bp0 = tile[y * 2 + 0];
                    var bp1 = tile[y * 2 + 1];
                    var bp2 = tile[16 + y];
                    var bit = 7 - x;
                    var colour = ((bp0 >> bit) & 1) | (((bp1 >> bit) & 1) << 1) | (((bp2 >> bit) & 1) << 2);
                    pixels[tzx + tzy + (ty * 8 + y) * Width + tx * 8 + x] = new Pixel((byte)(colour * 32), (byte)(colour * 32), (byte)(colour * 32), 255);
                }
            }
        }
    }

    public ReadOnlySpan<Pixel> GetImageData(float seconds)
    {
        // Lets begin with area 0
        var mapIndex = 0u;
        var parentId = 0u;
        var overworldMapSizeTable00_7F = 0x12844u;
        var gameState = 1;  // 0 = ?? , 1 = LightWorld, 2 = DarkWorld

        var largeMap = _rom.ReadBytes(ReadKind.Rom, overworldMapSizeTable00_7F + mapIndex, 1)[0] != 0;


        // 3 sets of sprite gfx pages
        // 1 main gfx (how many pages?)

        var overworldSpriteset00_3F = 0x7A41u;
        var overworldSpriteset40_7F = overworldSpriteset00_3F + 0x40;
        var overworldSpriteset80_BF = overworldSpriteset40_7F + 0x40;
        var overworldGfx00_7F = 0x7C9Cu;
        var overworldMapPalette00_7F = 0x7D1Cu;
        var overworldSpritePalette00_3F = 0x7B41u;
        var overworldSpritePalette40_7F = overworldSpritePalette00_3F + 0x40;
        var overworldSpritePalette80_BF = overworldSpritePalette40_7F + 0x40;
        var overworldGfxGroups = 0x5D97u;
        var overworldGfxGroups2 = 0x6073u;

        var overworldMapPaletteGroup = 0x75504u;
        var overworldSpritePaletteGroup = 0x75580u;
        var hudPaletteBase = 0xDD660u;  // 32x2 colours
        var overworldPaletteMainBase = 0xDE6C8u;    // 7x5x6 colours
        var overworldPaletteAuxBase = 0xDE86Cu;     // 7x3x14 colours


        var spriteGFX0 = _rom.ReadBytes(ReadKind.Rom, overworldSpriteset00_3F + parentId, 1)[0];
        var spriteGFX1 = _rom.ReadBytes(ReadKind.Rom, overworldSpriteset40_7F + parentId, 1)[0];
        var spriteGFX2 = _rom.ReadBytes(ReadKind.Rom, overworldSpriteset80_BF + parentId, 1)[0];
        var gfx = _rom.ReadBytes(ReadKind.Rom, overworldGfx00_7F + parentId, 1)[0];
        var auxPalette = _rom.ReadBytes(ReadKind.Rom, overworldMapPalette00_7F + parentId, 1)[0];
        var spritePalette0 = _rom.ReadBytes(ReadKind.Rom, overworldSpritePalette00_3F + parentId, 1)[0];
        var spritePalette1 = _rom.ReadBytes(ReadKind.Rom, overworldSpritePalette40_7F + parentId, 1)[0];
        var spritePalette2 = _rom.ReadBytes(ReadKind.Rom, overworldSpritePalette80_BF + parentId, 1)[0];
        var spritePalette = new byte[] { spritePalette0, spritePalette1, spritePalette2 };

        var mainPalette = 0u;    // LightWorld

        var indexWorld = 0x20u;  // LightWorld

        var tileGFX = _rom.ReadBytes(ReadKind.Rom, overworldGfxGroups2 + (indexWorld*8u), 8).ToArray();

        // Variable tiles
        var variableTiles = _rom.ReadBytes(ReadKind.Rom, overworldGfxGroups + (gfx * 4u), 4);

        tileGFX[3] = variableTiles[0] != 0 ? variableTiles[0] : (byte)0xFFu;    // Why does it bother pulling 4567 above?
        tileGFX[4] = variableTiles[1] != 0 ? variableTiles[1] : (byte)0xFFu;
        tileGFX[5] = variableTiles[2] != 0 ? variableTiles[2] : (byte)0xFFu;
        tileGFX[6] = variableTiles[3] != 0 ? variableTiles[3] : (byte)0xFFu;

        var animatedGfx = 0x5B;
        var subscreenOverlay = 0x9D;


        var mapPalGrp = _rom.ReadBytes(ReadKind.Rom, overworldMapPaletteGroup + (auxPalette * 4u), 3);

        var spritePalGrp = _rom.ReadBytes(ReadKind.Rom, overworldSpritePaletteGroup + (spritePalette[gameState] * 2u), 2);

        // CRAM

        // Palettes row 0,1 HUD colours
        var totalPalete = new Pixel[256];

        // Hud rows
        var row0 = _rom.ReadBytes(ReadKind.Rom, hudPaletteBase + (0 * 64u), 16*2);
        for (int i = 0; i < 16; i++)
        {
            var c = _rom.FetchMachineOrder16(i * 2, row0);
            totalPalete[i].FromSNES(c);
        }
        var row1 = _rom.ReadBytes(ReadKind.Rom, hudPaletteBase + (0 * 64u) + 32, 16*2);
        for (int i = 0; i < 16; i++)
        {
            var c = _rom.FetchMachineOrder16(i * 2, row1);
            totalPalete[16+i].FromSNES(c);
        }
        // Main rows
        for (uint i = 0; i < 5; i++)
        {
            var row = _rom.ReadBytes(ReadKind.Rom, overworldPaletteMainBase + (mainPalette * 7*5*2) + (i*7*2), 7*2);
            for (int j = 0; j < 7; j++)
            {
                var c = _rom.FetchMachineOrder16(j * 2, row);
                totalPalete[32 + i * 16 + j + 1].FromSNES(c);
            }
        }
        // Aux rows
        for (uint i = 0; i < 3; i++)
        {
            var row = _rom.ReadBytes(ReadKind.Rom, overworldPaletteAuxBase + (auxPalette * 7u*3*2) + (i*7*2), 7*2);
            for (int j = 0; j < 7; j++)
            {
                var c = _rom.FetchMachineOrder16(j * 2, row);
                totalPalete[80 + i * 16 + j + 1 + 8].FromSNES(c);
            }
        }






        var lorom = new LoRom(false, false);

        // Sheet 73 - 7E  (3bpp) (uncompressed)
        //var gfx3bppAddress = 0x87000u;
        //var length = 0x8B800u - gfx3bppAddress;
        //       var gfx3bpp = _rom.ReadBytes(ReadKind.Rom, gfx3bppAddress, length);

        // 0-112 COMP 3BPP
        // 113-114 2BPP sprites
        // 115-126 3BPP sprites
        // 127-217 COMP 3BPP sprites

        var pixels = new Pixel[Width * Height];


        pixels = new Pixel[Width * Height];

        for (int py = 0; py < 16; py++)
        {
            for (int px = 0; px < 16; px++)
            {
                var c = totalPalete[py * 16 + px];
                for (int y = 0; y < 16; y++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        pixels[(py * 16 + y) * Width + px * 16 + x] = c;
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

