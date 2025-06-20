
using System;
using RetroEditor.Plugins;

using SuperNintendoEntertainmentSystem.Memory;

namespace RetroEditorPlugin_SuperMarioWorld
{
    public class SuperMarioWorldPaletteImage : IImage, IUserWindow
    {
        public uint Width => 256;

        public uint Height => 256;

        public float ScaleX => 1.0f;

        public float ScaleY => 1.0f;

        public float UpdateInterval => 1 / 60.0f;

        private Pixel[] pixels;

        private IMemoryAccess _rom;
        private AddressTranslation _addressTranslation;
        private IWidgetRanged temp_levelSelect;
        private bool runOnce = false;

        public SuperMarioWorldPaletteImage(IEditor editorInterface, IMemoryAccess rom)
        {
            _rom = rom;
            _addressTranslation = new LoRom(false,false);
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
                var smwColours = new SuperMarioPalette(_rom, smwLevelHeader);
                pixels = new Pixel[Width * Height];

                for (int py = 0; py < 16; py++)
                {
                    for (int px = 0; px < 16; px++)
                    {
                        var c = smwColours[py, px];
                        for (int y = 0; y < 16; y++)
                        {
                            for (int x = 0; x < 16; x++)
                            {
                                pixels[(py * 16 + y) * Width + px * 16 + x] = c;
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