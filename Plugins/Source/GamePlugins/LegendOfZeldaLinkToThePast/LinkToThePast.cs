using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using RetroEditor.Plugins;
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

    public void ConfigureMenu(IMemoryAccess rom, IMenu menu)
    {
        menu.AddItem("Testing",
            (editorInterface, menuItem) =>
            {
                editorInterface.OpenUserWindow($"Testing", GetTesting(editorInterface, rom));
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

    public void DrawPage(ref Pixel[] pixels, ReadOnlySpan<byte> gfx3bpp, int offsX, int offsY)
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
        // 115-126 3BPP sprites
        // 127-217 COMP 3BPP sprites

        var pixels = new Pixel[Width * Height];

        uint start = 128+64;
        uint end = Math.Min(128+64+64u,0xCE);

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
            if (numTiles != 64)
            {
                continue;
            }

            var offsX = ((a - start) % 4) * 128;
            var offsY = ((a - start) / 4) * 32 * Width;

            DrawPage(ref pixels, gfx3bpp, (int)offsX, (int)offsY); // Draw first page at (0,0)
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
