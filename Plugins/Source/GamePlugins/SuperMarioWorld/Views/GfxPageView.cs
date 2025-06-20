using System;
using RetroEditor.Plugins;

using SuperNintendoEntertainmentSystem.Memory;
using SuperNintendoEntertainmentSystem.Compression;

namespace RetroEditorPlugin_SuperMarioWorld
{
    public class SuperMarioWorldGFXPageImage : IImage, IUserWindow
    {
        public uint Width => 256;

        public uint Height => 256;

        public float ScaleX => 2.0f;

        public float ScaleY => 2.0f;

        public float UpdateInterval => 1 / 60.0f;

        private IMemoryAccess _rom;
        private AddressTranslation _addressTranslation;
        private IWidgetRanged temp_pageSelect;
        private IWidgetLabel temp_pageInfo;
        private IEditor _editorInterface;

        public SuperMarioWorldGFXPageImage(IEditor editorInterface, IMemoryAccess rom)
        {
            _rom = rom;
            _addressTranslation = new LoRom(false,false);
            _editorInterface = editorInterface;

            for (int i = 0; i <= 0x33; i++)
            {
                GFXPageKind[i] = SNESTileKind.Tile_3bpp;
            }
            GFXPageKind[0x27] = SNESTileKind.Tile_3bppMode7;
            for (int i = 0x28; i <= 0x2B; i++)
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
                else if (temp_pageSelect.Value == 0x32)
                {
                    gfxPtr = _addressTranslation.ToImage(SMWAddresses.GFX32);
                }
                else
                {
                    gfxPtr = _addressTranslation.ToImage(SMWAddresses.GFX33);
                }


                var decomp = new byte[32768];
                var GFX = new ReadOnlySpan<byte>(decomp);

                var size = LC_LZ2.Decompress(ref decomp, _rom.ReadBytes(ReadKind.Rom, gfxPtr, 32768),out _);  // Overread but should be ok
                temp_pageInfo.Name = $"GFX Page {temp_pageSelect.Value:X2} {GFXPageKind[temp_pageSelect.Value]} Decoded Size {size}";

                var rasteriseAs = GFXPageKind[temp_pageSelect.Value];
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
                var tileCount = size / tileSize;
                for (int i = 0; i < tileCount; i++)
                {
                    var tx = (i % 256) % 16;
                    var ty = (i % 256) / 16;
                    var tzx = ((i / 256) % 2) * Width / 2;
                    var tzy = ((i / 256) / 2) * (Height / 2) * Width;
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
                                        pixels[tzx + tzy + (ty * 8 + y) * Width + tx * 8 + x] = new Pixel((byte)(colour * 64), (byte)(colour * 64), (byte)(colour * 64), 255);
                                    }
                                    break;
                                case SNESTileKind.Tile_3bpp:
                                    {
                                        var bp0 = tile[y * 2 + 0];
                                        var bp1 = tile[y * 2 + 1];
                                        var bp2 = tile[16 + y];
                                        var bit = 7 - x;
                                        var colour = ((bp0 >> bit) & 1) | (((bp1 >> bit) & 1) << 1) | (((bp2 >> bit) & 1) << 2);
                                        pixels[tzx + tzy + (ty * 8 + y) * Width + tx * 8 + x] = new Pixel((byte)(colour * 32), (byte)(colour * 32), (byte)(colour * 32), 255);
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
                                        pixels[tzx + tzy + (ty * 8 + y) * Width + tx * 8 + x] = new Pixel((byte)(colour * 16), (byte)(colour * 16), (byte)(colour * 16), 255);
                                    }
                                    break;
                                case SNESTileKind.Tile_3bppMode7:
                                    {
                                        var row = tile[y * 3 + 0] << 16 | tile[y * 3 + 1] << 8 | tile[y * 3 + 2];
                                        var colour = (byte)((row >> (21 - x * 3)) & 0x07);
                                        pixels[tzx + tzy + (ty * 8 + y) * Width + tx * 8 + x] = new Pixel((byte)(colour * 32), (byte)(colour * 32), (byte)(colour * 32), 255);
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
}