
public class Rollercoaster : IRetroPlugin, IImages
{
    private byte[] RollercoasterInMem = new byte[] { 239, 164, 65, 60, 2, 160, 26, 129, 136, 128, 247, 75, 160, 47, 7, 9 };

    public string Name => "Rollercoaster";
    IRomPlugin rom;


    public Rollercoaster()
    {
        rom = new NullRomPlugin();
    }

    public bool CanHandle(byte[] md5, byte[] bytes, string filename)
    {
        // One issue with this approach, is we can't generically load hacks of the game..
        return RollercoasterInMem.SequenceEqual(md5);
    }

    public bool Init(IEditor editorInterface, byte[] md5, byte[] bytes, string filename)
    {
        var spectrumRomInterface = editorInterface.GetRomInstance("ZXSpectrum");
        if (spectrumRomInterface==null)
        {
            return false;
        }
        rom = spectrumRomInterface;
        if (rom.Load(bytes,"MEM"))
        {
            return true;
        }
        return false;
    }

    public int GetImageCount()
    {
        return 16+16+16+12;
    }

    public void Close()
    {
    }

    public IImages GetImageInterface()
    {
        return this;
    }

    public IImage GetImage(int mapIndex)
    {
        if (mapIndex<12)
        {
            int roomAddress = 0x7100 + (mapIndex * 256);
            return new RollercoasterImage(this, mapIndex, rom.ReadBytes((uint)roomAddress, 256));
        }
        if (mapIndex<12+16)
        {
            int roomAddress = 0x6100 + ((mapIndex-12) * 256);
            return new RollercoasterImage(this, mapIndex, rom.ReadBytes((uint)roomAddress, 256));
        }
        if (mapIndex<12+16+16)
        {
            int roomAddress = 0xE679 + ((mapIndex-12-16) * 256);
            return new RollercoasterImage(this, mapIndex, rom.ReadBytes((uint)roomAddress, 256));
        }
        else
        {
            int roomAddress = 0xC300 + ((mapIndex-12-16-16) * 256);
            return new RollercoasterImage(this, mapIndex, rom.ReadBytes((uint)roomAddress, 256));
        }
    }

    public byte[] GetLevelTile(int tileIndex)
    {
        return rom.ReadBytes((uint)(0x7D00 + (tileIndex * 8)), 8);
    }

    public byte GetByte(uint address)
    {
        return rom.ReadByte(address);
    }
}

public class RollercoasterImage : IImage
{
    Rollercoaster main;
    byte[] mapData;
    string mapName;
    int mapIndex;

    ZXSpectrum48ImageHelper imageHelper;

    public RollercoasterImage(Rollercoaster main, int mapIndex, byte[] data)
    {
        this.main = main;
        this.mapData = data;
        this.mapIndex = mapIndex;
        this.mapName = GetMapName();
        imageHelper=new ZXSpectrum48ImageHelper(Width, Height);
    }

    public string GetMapName()
    {
        if (mapIndex<12)
        {
            return $"Level 0 Room {mapIndex}";
        }
        if (mapIndex<12+16)
        {
            return $"Level 1 Room {mapIndex-12}";
        }
        if (mapIndex<12+16+16)
        {
            return $"Level 2 Room {mapIndex-12-16}";
        }
        else
        {
            return $"Level 3 Room {mapIndex-12-16-16}";
        }
    }


    public uint Width => 256;

    public uint Height => 18*8;

    public string Name => mapName;

    public Pixel[] GetImageData(float seconds)
    {
        return RenderMap(seconds);
    }

    private byte[] GetTile(int code)
    {
        return main.GetLevelTile(code);
    }

    private Pixel[] RenderMap(float seconds)
    {
        uint levelDataOffset = 0x7F;

        imageHelper.Clear(0x40);
        levelDataOffset = RenderCoasterTrack(levelDataOffset);
        levelDataOffset = RenderTileRow16(levelDataOffset);
        levelDataOffset = RenderPlatforms(levelDataOffset, 0, 1);
        levelDataOffset = RenderPlatforms(levelDataOffset, 1, 0);
        levelDataOffset = RenderPlatforms(levelDataOffset, 1, 1);
        levelDataOffset = RenderPlatforms(levelDataOffset, 1, -1);
        levelDataOffset = RenderLargeTiles(levelDataOffset, false);
        levelDataOffset = RenderLargeTiles(levelDataOffset, true);
        levelDataOffset = PaintAttributeBlocks(levelDataOffset);

        return imageHelper.Render(seconds);
    }

    private uint RenderCoasterTrack(uint mapOffset)
    {
        var numTracks = mapData[mapOffset];
        mapOffset += 2;
        for (int t = 0; t < numTracks; t++)
        {
            var colour = mapData[0x26];
            var x = mapData[mapOffset++];
            var y = mapData[mapOffset++];
            var numSegments = mapData[mapOffset++];
            for (int a = 0; a < numSegments; a++)
            {
                var c = mapData[mapOffset++];
                var b = mapData[mapOffset++];
                RenderTrackSegment(b, c, ref x, ref y, colour);
            }
        };
        return mapOffset;
    }

    private void RenderTrackSegment(byte deltaY, byte deltaX, ref byte x, ref byte y, byte colour)
    {
        var a = 0;
        if (a != deltaY)
        {
            if (a != deltaX)
            {
                if ((deltaY & 0x80) == 0x80)
                {
                    RenderCoasterTrackSegmentHorizontal(ref x, ref y, deltaY & 0x7F, 0xFF, colour);
                }
                else
                {
                    RenderCoasterTrackSegmentHorizontal(ref x, ref y, deltaY & 0x7F, 0x01, colour);
                }
                return;
            }
            else
            {
                if ((deltaY & 0x80) == 0x80)
                {
                    RenderCoasterTrackSegmentVertical(ref x, ref y, deltaY & 0x7F, 0xFF, colour);
                }
                else
                {
                    RenderCoasterTrackSegmentVertical(ref x, ref y, deltaY & 0x7F, 0x01, colour);
                }
            }
        }
        else
        {
            RenderCoasterTrackSegmentHorizontal(ref x, ref y, deltaX, 0x00, colour);
        }
    }

    private void RenderCoasterTrackSegmentHorizontal(ref byte x, ref byte y, int length, byte dy, byte colour)
    {
        for (int a = 0; a < length; a++)
        {
            Render4PixelWideVertical(x, y, colour);
            x++;
            y+=dy;
        }
    }
    
    private void RenderCoasterTrackSegmentVertical(ref byte x, ref byte y, int length, byte dy, byte colour)
    {
        for (int a = 0; a < length; a++)
        {
            Render4PixelWideHorizontal(x, y, colour);
            y+=dy;
        }
    }


    private void Render4PixelWideVertical(uint x, uint y, byte colour)
    {
        for (uint yy = 0; yy < 4; yy++)
        {
            imageHelper.DrawBit(x, y + yy, true, colour);
        }
    }
    
    private void Render4PixelWideHorizontal(uint x, uint y, byte colour)
    {
        for (uint xx = 0; xx < 4; xx++)
        {
            imageHelper.DrawBit(x + xx, y, true, colour);
        }
    }

    private uint RenderTileRow16(uint mapOffset)
    {
        int tile = mapData[mapOffset];
        if (tile != 0)
        {
            var tileData = GetTile(tile);
            for (uint a = 0; a < 32; a++)
            {
                RenderTile(a, 16, tileData, mapData[0x14]);
            }
        }

        return mapOffset;
    }

    private uint RenderLargeTiles(uint mapOffset, bool vertical)
    {
        mapOffset++;
        uint cnt = mapData[mapOffset];  //Number of sets of large tiles to draw

        for (uint a = 0; a < cnt; a++)
        {
            // 8 bytes :     [0x21=lowbyte of graphic address][0x22=highbyte of graphic address][0x23=FlipY|FlipX|NumCopies][0x24=NumTilesX]
            //               [0x25=NumLinesY][0x26=PixelsX][0x27=PixelsY][0x28=Attribute]
            uint srcGfx = mapData[++mapOffset];
            srcGfx |= (uint)(mapData[++mapOffset] << 8);
            byte numCopies = mapData[++mapOffset];
            bool flipX = (numCopies & 0x40) != 0;
            bool flipY = (numCopies & 0x80) != 0;
            numCopies &= 0x3F;
            byte numTilesX = mapData[++mapOffset];
            byte numLinesY = mapData[++mapOffset];
            byte pixelsX = mapData[++mapOffset];
            byte pixelsY = mapData[++mapOffset];
            byte attribute = mapData[++mapOffset];

            for (uint b = 0; b < numCopies; b++)
            {
                DrawBigTileXor(pixelsY, pixelsX, numLinesY, numTilesX, srcGfx, attribute, flipX, flipY);

                PaintAttributes(pixelsY, pixelsX, numLinesY, numTilesX, attribute);

                if (vertical)
                {
                    pixelsY += numLinesY;
                }
                else
                {
                    pixelsX += (byte)(numTilesX*8);
                }
            }
        }

        return mapOffset;
    }

    private uint PaintAttributeBlocks(uint mapOffset)
    {
        mapOffset++;
        uint cnt = mapData[mapOffset];

        for (uint a = 0; a < cnt; a++)
        {
            uint attrPos = mapData[++mapOffset];
            attrPos |= (uint)(mapData[++mapOffset] << 8);
            var innercnt = mapData[++mapOffset];
            var outercnt = mapData[++mapOffset];
            var x = mapData[++mapOffset];
            var y = mapData[++mapOffset];

            for (uint yy = 0; yy < outercnt; yy++)
            {
                for (uint xx = 0; xx < innercnt; xx++)
                {
                    uint cx = (xx + x) * 8;
                    uint cy = (yy + y) * 8;
                    var curAttribute = imageHelper.GetAttribute(cx, cy);
                    if (curAttribute != mapData[0x12] && curAttribute != mapData[0x13])
                    {
                        var newAttr = main.GetByte(attrPos++);
                        imageHelper.SetAttribute(cx, cy, newAttr);
                    }
                }
            }
        }
        return mapOffset;
    }

    private void DrawBigTileXor(byte y,byte x,byte numLines,byte numTiles,uint srcGfx, byte attribute,bool flipX,bool flipY)
    {
        for (uint yy = 0; yy < numLines; yy++)
        {
            uint yyy = (uint)(flipY ? (numLines - 1) - yy : yy);
            for (uint xx = 0; xx < numTiles; xx++)
            {
                uint xxx = (uint)(flipX ? (numTiles - 1) - xx : xx);
                var tileByte = main.GetByte(srcGfx++);

                imageHelper.Xor8BitsNoAttribute(xxx*8+x,yyy+y,tileByte,flipX);
            }
        }
    }

    private void PaintAttributes(byte y,byte x,byte numLines,byte numTiles,byte attribute)
    {
        for (uint yy = 0; yy < numLines; yy+=8)
        {
            for (uint xx = 0; xx < numTiles; xx++)
            {
                var curAttribute = imageHelper.GetAttribute(xx * 8 + x, yy + y);
                if (curAttribute != mapData[0x12] && curAttribute != mapData[0x13])
                {
                    imageHelper.SetAttribute(xx * 8 + x, yy + y, attribute);
                }
            }
        }
    }


    private uint RenderPlatforms(uint mapOffset, int colDelta,int rowDelta)
    {
        mapOffset++;
        uint cnt = mapData[mapOffset];
        while (cnt!=0)
        {
            var tile = mapData[++mapOffset];
            var colour = mapData[++mapOffset];
            var tileData = GetTile(tile);
            var len = mapData[++mapOffset];
            var row = mapData[++mapOffset];
            var col = mapData[++mapOffset];

            int x = col;
            int y = row;
            for (int a=0;a<len;a++)
            {
                RenderTile((uint)x, (uint)y, tileData, colour);
                x += colDelta;
                y += rowDelta;
            }
            cnt--;
        }
        return mapOffset;
    }

    private void RenderTile(uint xpos, uint ypos, byte[] tile, byte attribute)
    {
        xpos &= 31;
        //ypos = Math.Abs(ypos);
        //ypos %= 18;
        xpos *= 8;
        ypos *= 8;
        for (uint ty = 0; ty < 8; ty++)
        {
            imageHelper.Draw8Bits(xpos, ypos + ty, tile[ty], attribute, false);
        }
    }
    
}
