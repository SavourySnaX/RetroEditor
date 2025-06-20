using System;
using System.Collections.Generic;
using RetroEditor.Plugins;

using SuperNintendoEntertainmentSystem.Memory;

namespace RetroEditorPlugin_SuperMarioWorld
{
    public class SuperMarioWorldLevelEditor : IUserWindow
    {
        public float UpdateInterval => 1 / 60.0f;

        private IEditor _editorInterface;
        public SuperMarioWorldLevelEditor(IEditor editorInterface, IMemoryAccess rom)
        {
            _editorInterface = editorInterface;
        }

        public void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls)
        {
            widget.AddObjectMapWidget(new SuperMarioWorldObjectMap(rom, _editorInterface));
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
            _imageData = new Pixel[16 * 16];

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

    public class SuperMarioWorldObjectMap : IObjectMap, ITilePalette
    {
        public uint Width => 16 * 16 * 32;

        public uint Height => 416;

        public float ScaleX => 1.0f;

        public float ScaleY => 1.0f;

        public IEnumerable<IObject> FetchObjects => _objects;

        public uint MaxTiles => 512;

        public int SelectedTile { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public uint TilesPerRow => throw new NotImplementedException();

        private IEditor _editorInterface;
        public SuperMarioWorldObjectMap(IMemoryAccess rom, IEditor editorInterface)
        {
            _editorInterface = editorInterface;
            _tiles = new SMWTile[512];

            var levelSelect = 199u;
            var addressTranslation = new LoRom(false, false);
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

            var levelHelpers = new LevelHelpers(rom, editorInterface);
            _objects = levelHelpers.FetchObjectLayer(ref smwRom, smwLevelHeader, vram, smwRom.Layer1Data);
            _dirty = true;
        }

        private bool _dirty;
        public bool IsDirty => _dirty;

        public TilePaletteStore FetchPalette()
        {
            return _palette;
        }

        public void Update(float seconds)
        {
        }

        public ReadOnlySpan<ITile> FetchTiles()
        {
            _dirty = false;
            return _tiles;
        }

        public void ObjectMove(IObject obj, uint x, uint y)
        {
            var standardObject = obj as StandardObject;
            if (standardObject != null)
            {
                standardObject.Move(x, y);
            }
        }

        public void ObjectDelete(IObject obj)
        {
            _objects.Remove(obj);
        }

        private List<IObject> _objects = new List<IObject>();
        private TilePaletteStore _palette;
        private SMWTile[] _tiles;
    }

}