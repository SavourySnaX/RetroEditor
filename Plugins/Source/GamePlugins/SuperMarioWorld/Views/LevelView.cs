using System;
using RetroEditor.Plugins;

using RetroEditorPlugin_SuperMarioWorld;

public class SuperMarioWorldLevelViewImage : IImage, IUserWindow
{
    public uint Width => 16*16*32;

    public uint Height => 416;

    public float ScaleX => 1.0f;

    public float ScaleY => 1.0f;

    public float UpdateInterval => 1/60.0f;

    private IMemoryAccess _rom;
    private AddressTranslation _addressTranslation;
    private IWidgetRanged temp_levelSelect;
    private IWidgetLabel widgetLabel;
    private IEditor _editorInterface;

    private SuperMarioPalette _palette;
    private SMWMap16 _map16ToTile;


    public SuperMarioWorldLevelViewImage(IEditor editorInterface, IMemoryAccess rom)
    {
        _rom = rom;
        _addressTranslation = new LoRom();
        _editorInterface = editorInterface;
    }

    public void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls)
    {
        widget.AddImageView(this);
        temp_levelSelect = widget.AddSlider("Level", SomeConstants.DefaultLevel, 0, 511, () => { runOnce = false; });
        widgetLabel = widget.AddLabel("");
    }

    private bool runOnce = false;

    enum SpriteObject
    {

        RedKoopa = 0x05,

    }


    Pixel[] pixels;

    void Draw16x16Tile(int tx,int ty, Pixel colour)
    {
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                int px = tx * 16 + x;
                int py = ty * 16 + y;
                if (px >= 0 && px < Width && py >= 0 && py < Height)
                {
                    pixels[py * Width + px] = colour;
                }
            }
        }
    }

    void DrawGfxTile(int tx,int ty, int tile, SuperMarioVRam vram)
    {
        var tile16 = _map16ToTile[tile];

        SMWRenderHelpers.DrawGfxTile(tx, ty, tile16, vram, ref pixels, Width, Height, _palette); 
    }

    public ReadOnlySpan<Pixel> GetImageData(float seconds)
    {
        if (!runOnce)
        {
            runOnce = true;

            pixels = new Pixel[Width * Height];

            // Get the level select value, index into the layer 1 data etc
            var levelSelect = (uint)temp_levelSelect.Value;
            var smwRom = new SuperMarioWorldRomHelpers(_rom, _addressTranslation, levelSelect);
            var smwLevelHeader = smwRom.Header;
            _palette = new SuperMarioPalette(_rom, smwLevelHeader);
            _map16ToTile = new SMWMap16(_rom, _addressTranslation, smwLevelHeader);
            var vram = new SuperMarioVRam(_rom, smwLevelHeader);

            widgetLabel.Name = $"Layer 1 : {smwRom.Layer1SnesAddress:X6} Layer 2 : {smwRom.Layer2SnesAddress:X6} Sprite : {smwRom.SpriteSnesAddress:X6} - {smwLevelHeader.GetTileset()}";

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = _palette.BackgroundColour;
            }

            if (smwRom.Layer2IsImage)
            {
                RenderLayer2Image(ref smwRom, smwLevelHeader, vram, smwRom.Layer2Data, smwRom.Layer2ImagePage01);
            }
            else
            {
                RenderObjectLayer(ref smwRom, smwLevelHeader, vram, smwRom.Layer2Data);
            }

            RenderObjectLayer(ref smwRom, smwLevelHeader, vram, smwRom.Layer1Data);

            RenderSpriteLayer(ref smwRom, smwLevelHeader, vram);
        }

        return pixels;
    }

    private void RenderLayer2Image(ref SuperMarioWorldRomHelpers smwRom, SMWLevelHeader smwLevelHeader, SuperMarioVRam vram, uint layerAddress, bool useUpperPage)
    {
        var decompBuffer = new byte [32768];
        var writeOffs = 0;
        // First off, decompress the layer 2 data, then splat it across the level
        while (true)
        {
            var bytes = _rom.ReadBytes(ReadKind.Rom, layerAddress, 2);
            layerAddress += 2;
            var b0 = bytes[0];
            var b1 = bytes[1];
            if (b0==b1 && b0==0xFF)
            {
                break;
            }
            var length = (b0 & 0x7F) + 1;
            if ((b0&0x80)==0)
            {
                // copy length bytes
                decompBuffer[writeOffs++] = bytes[1];
                length--;
                if (length>0)
                {
                    var data = _rom.ReadBytes(ReadKind.Rom, layerAddress, (uint)length);
                    for (int i=0;i<length;i++)
                    {
                        decompBuffer[writeOffs++] = data[i];
                    }
                }
                layerAddress += (uint)length;
            }
            else
            {
                // copy b1 length times
                for (int i=0;i<length;i++)
                {
                    decompBuffer[writeOffs++] = b1;
                }
            }
        }

        // The decompBuffer is data to directly index the map16 second half data.
        var offset = 512 + (useUpperPage ? 256 : 0);
        for (int a=0;a<Width/256;a++)
        {
            var wOffset=a*16;
            var decompBufferOffset = (a&1)*16*27;
            for (int y = 0; y < 27; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    var tile = decompBuffer[decompBufferOffset + y * 16 + x] + offset;
                    DrawGfxTile(wOffset+x, y, tile, vram);
                }
            }
        }

    }

    private void RenderObjectLayer(ref SuperMarioWorldRomHelpers smwRom, SMWLevelHeader smwLevelHeader, SuperMarioVRam vram, uint layerAddress)
    {
        var levelHelpers = new LevelHelpers(_rom,_editorInterface);
        var objects = levelHelpers.FetchObjectLayer(ref smwRom, smwLevelHeader, vram, smwRom.Layer1Data);
        foreach (var o in objects)
        {
            var i = 0;
            var mData = o.GetMapData();
            for (int y = 0; y < o.Height; y++)
            {
                for (int x = 0; x < o.Width; x++)
                {
                    DrawGfxTile((int)o.X/16 + x, (int)o.Y/16 + y, (int)mData[i++] , vram);
                }
            }
        }
    }

    private void RenderSpriteLayer(ref SuperMarioWorldRomHelpers smwRom, SMWLevelHeader smwLevelHeader, SuperMarioVRam vram)
    {
        var spriteData = smwRom.SpriteData;
        bool layerDone = false;
        uint offset = 0;
        _editorInterface.Log(LogType.Info, $"Sprites:");
        while (!layerDone)
        {
            var triple = _rom.ReadBytes(ReadKind.Rom, spriteData + offset, 3);
            if (triple[0] == 0xFF)
            {
                return;
            }
            // Check if Standard Object / Extended Object

            var t0 = triple[0];  // yyyyEESY
            var t1 = triple[1];  // XXXXssss
            var t2 = triple[2];  // NNNNNNNN

            offset += 3;

            var spriteY = ((t0 & 1) << 4) | (t0 >> 4);
            var spriteX = (t1 & 0xF0) >> 4;
            var screenNumber = ((t0 & 0x02) << 3) | (t1 & 0x0F);
            var spriteId = t2;
            var spriteExtra = (t0 & 0x0C) >> 2;
            
            var yPos = spriteY;
            var xPos = screenNumber * 16 + spriteX;

            switch ((SpriteObject)spriteId)
            {
                default:
                case SpriteObject.RedKoopa:
                    Draw16x16Tile(xPos, yPos, new Pixel(255, 255, 128, 255));
                    _editorInterface.Log(LogType.Info, $"{screenNumber:X2} | {spriteId:X2} {(SpriteObject)spriteId} @{xPos:X2},{yPos:X2} - Extra {spriteExtra:X2}");
                    break;
            }

        }
    }


    public void OnClose()
    {
    }
}
