using System;
using RetroEditor.Plugins;

using SuperNintendoEntertainmentSystem.Memory;

namespace RetroEditorPlugin_SuperMarioWorld
{
    public class SuperMarioWorldVramImage : IImage, IUserWindow
    {
        public uint Width => 128 * 2;

        public uint Height => 128 * 3;

        public float ScaleX => 2.0f;

        public float ScaleY => 2.0f;

        public float UpdateInterval => 1 / 60.0f;

        private const uint NumberOfTiles = 16 * 16 * 6;

        private Pixel[] pixels;

        private IMemoryAccess _rom;
        private AddressTranslation _addressTranslation;
        private IWidgetRanged temp_levelSelect;
        private bool runOnce = false;

        public SuperMarioWorldVramImage(IEditor editorInterface, IMemoryAccess rom)
        {
            _rom = rom;
            _addressTranslation = new LoRom(false, false);
        }

        public void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls)
        {
            widget.AddImageView(this);
            temp_levelSelect = widget.AddSlider("Level", SomeConstants.DefaultLevel, 0, 511, () => { runOnce = false; });
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
                pixels = new Pixel[Width * Height];

                for (int tiles = 0; tiles < NumberOfTiles; tiles++)
                {
                    var gfx = smwVRam.Tile(tiles);
                    int g = 0;
                    var tx = tiles % 16;
                    var ty = (tiles % (NumberOfTiles / 2)) / 16;
                    var tz = (tiles / (NumberOfTiles / 2)) * 128;
                    for (int y = 0; y < 8; y++)
                    {
                        for (int x = 0; x < 8; x++)
                        {
                            var colour = gfx[g++];
                            pixels[(ty * 8 + y) * Width + tx * 8 + x + tz] = new Pixel((byte)(colour * 16), (byte)(colour * 16), (byte)(colour * 16), 255);
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