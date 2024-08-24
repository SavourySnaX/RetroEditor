using System;
using System.Collections.Generic;
using RetroEditor.Plugins;

namespace RetroEditorPlugin_SuperMarioWorld
{
    public class SuperMarioWorldLevelEditor : IUserWindow
    {
        public float UpdateInterval => 1/60.0f;

        public SuperMarioWorldLevelEditor(IEditor editorInterface, IMemoryAccess rom)
        {
        }

        public void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls)
        {
            widget.AddObjectMapWidget(new SuperMarioWorldObjectMap(rom));
        }

        public void OnClose()
        {
        }
    }

    class SMWTile : ITile
    {
        public uint Width => 16;

        public uint Height => 16;

        public string Name => _name;

        public SMWTile(string name, Tile16x16 map16, SuperMarioPalette palette, SuperMarioVRam vram)
        {
            _name = name;
            _imageData = new Pixel[16*16];

            // Rasterise the tiles
            SMWRenderHelpers.DrawGfxTile(0, 0, map16, vram, ref _imageData, 16, 16, palette);
        }

        public Pixel[] GetImageData()
        {
            return _imageData;
        }

        private string _name;
        private Pixel[] _imageData;
    }

    public class StandardObject : IObject
    {
        public uint Width => _width;

        public uint Height => _height;

        public uint X => _x;

        public uint Y => _y;

        public string Name => _name;

        public StandardObject(uint x,uint y, uint width, uint height, string name, uint[] mapData)
        {
            _x = x;
            _y = y;
            _width = width;
            _height = height;
            _name = name;
            _mapData = mapData;
        }

        public ReadOnlySpan<uint> GetMapData()
        {
            return _mapData;
        }

        private uint _x, _y, _width, _height;
        private string _name;
        private uint[] _mapData;
    }

    public class SuperMarioWorldObjectMap : IObjectMap, ITilePalette
    {
        public uint Width => 16*16*32;

        public uint Height => 416;

        public float ScaleX => 1.0f;

        public float ScaleY => 1.0f;

        public IEnumerable<IObject> FetchObjects => _objects;

        public uint MaxTiles => 512;

        public int SelectedTile { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public uint TilesPerRow => throw new NotImplementedException();

        public SuperMarioWorldObjectMap(IMemoryAccess rom)
        {
            _tiles = new SMWTile[512];

            var levelSelect = 199u;
            var addressTranslation = new LoRom();
            var smwRom = new SuperMarioWorldRomHelpers(rom, addressTranslation, levelSelect);
            var smwLevelHeader = smwRom.Header;
            var palette = new SuperMarioPalette(rom, smwLevelHeader);
            var map16ToTile = new SMWMap16(rom, addressTranslation, smwLevelHeader);
            var vram = new SuperMarioVRam(rom, smwLevelHeader);

            for (int i = 0; i < 512; i++)
            {
                var tile = new SMWTile($"Tile {i}", map16ToTile[i], palette, vram);
                _tiles[i] = tile;
            }

            _palette = new TilePaletteStore(this);

            // Test - add a Yoshi Coin to test
            _objects.Add(new StandardObject(8, 8, 1, 2, "Yoshi Coin", new uint[] { 0x2D, 0x2E }));
        }

        public TilePaletteStore FetchPalette()
        {
            return _palette;
        }

        public void Update(float seconds)
        {
        }

        public ReadOnlySpan<ITile> FetchTiles()
        {
            return _tiles;
        }

        private List<IObject> _objects = new List<IObject>();
        private TilePaletteStore _palette;
        private SMWTile[] _tiles;
    }

}