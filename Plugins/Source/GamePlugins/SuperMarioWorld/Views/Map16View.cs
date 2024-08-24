using System;
using RetroEditor.Plugins;

namespace RetroEditorPlugin_SuperMarioWorld
{
    public class SuperMarioWorldMap16Image : IImage, IUserWindow
    {
        public uint Width => 256;

        public uint Height => 256 * 2;

        public float ScaleX => 1.5f;

        public float ScaleY => 1.5f;

        public float UpdateInterval => 1 / 60.0f;

        private Pixel[] pixels;

        private IMemoryAccess _rom;
        private AddressTranslation _addressTranslation;
        private IWidgetRanged temp_levelSelect;
        private IWidgetCheckable temp_ShowBGMap;
        private bool runOnce = false;

        public SuperMarioWorldMap16Image(IEditor editorInterface, IMemoryAccess rom)
        {
            _rom = rom;
            _addressTranslation = new LoRom();
        }

        public void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls)
        {
            widget.AddImageView(this);
            temp_levelSelect = widget.AddSlider("Level", SomeConstants.DefaultLevel, 0, 511, () => { runOnce = false; });
            temp_ShowBGMap = widget.AddCheckbox("Show BG Map", false, () => { runOnce = false; });
        }

        private const uint NumberOfTiles = 0x200;

        void Draw8x8(int tx, int ty, int xo, int yo, SubTile tile, SuperMarioVRam vram, SuperMarioPalette palette)
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
}
