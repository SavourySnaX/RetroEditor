
public class Rollercoaster : IRetroPlugin, IImages
{
    // This is MD5 of a ram dump of the game, will update with tap/tzx version later 
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

    public byte[] GetBytes(uint address, uint length)
    {
        return rom.ReadBytes(address, length);
    }
}

public class RollercoasterImage : IImage
{
    Rollercoaster main;
    byte[] mapData;
    string mapName;
    int mapIndex;
    int frameCounter;

    ZXSpectrum48ImageHelper imageHelper;

    public RollercoasterImage(Rollercoaster main, int mapIndex, byte[] data)
    {
        this.main = main;
        this.mapData = data;
        this.mapIndex = mapIndex;
        this.mapName = GetMapName();
        imageHelper = new ZXSpectrum48ImageHelper(Width, Height);

        var cnt = mapData[(int)RollerCoaster_MapDataOffsets.NumberOfSprites];
        uint spriteData = mapData[(int)RollerCoaster_MapDataOffsets.SpriteDataHigh];
        spriteData <<= 8;
        spriteData |= mapData[(int)RollerCoaster_MapDataOffsets.SpriteDataLow];
    }

    public string GetMapName()
    {
        if (mapIndex < 12)
        {
            return $"Level 0 Room {mapIndex}";
        }
        if (mapIndex < 12 + 16)
        {
            return $"Level 1 Room {mapIndex - 12}";
        }
        if (mapIndex < 12 + 16 + 16)
        {
            return $"Level 2 Room {mapIndex - 12 - 16}";
        }
        else
        {
            return $"Level 3 Room {mapIndex - 12 - 16 - 16}";
        }
    }


    public uint Width => 256;

    public uint Height => 18 * 8;

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
        frameCounter = (int)(seconds * 25);
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
        levelDataOffset = RenderSign(levelDataOffset);

        RenderPickups();

        RenderMapSprites();

        return imageHelper.Render(seconds);
    }

    private byte ComputePositionPos(byte start, byte limitA, byte limitB)
    {
        var remainA = limitA - start;
        var B = limitA - limitB;
        var remainC = start - limitB;

        remainA /= 2;
        B /= 2;
        remainC /= 2;

        var t = frameCounter % (remainA + B + remainC);
        if (t < remainA)
        {
            return (byte)(start + t * 2);
        }
        else if (t < (remainA + B))
        {
            return (byte)(limitA - (t - remainA) * 2);
        }
        else
        {
            return (byte)(limitB + (t - remainA - B) * 2);
        }
    }
    
    private byte ComputePositionNeg(byte start, byte limitA, byte limitB)
    {
        var remainA = start - limitA;
        var B = limitB - limitA;
        var remainC = limitB - start;

        var t = frameCounter % (remainA + B + remainC);
        if (t < remainA)
        {
            return (byte)(start - t);
        }
        else if (t < (remainA + B))
        {
            return (byte)(limitA + (t - remainA));
        }
        else
        {
            return (byte)(limitB - (t - remainA - B));
        }
    }

    private void RenderMapSprites()
    {
        var cnt = mapData[(int)RollerCoaster_MapDataOffsets.NumberOfSprites];
        uint spriteData=mapData[(int)RollerCoaster_MapDataOffsets.SpriteDataHigh];
        spriteData <<= 8;
        spriteData |= mapData[(int)RollerCoaster_MapDataOffsets.SpriteDataLow];

        for (int a=0;a<cnt;a++)
        {
            var sprite = main.GetBytes(spriteData, 15);
            spriteData += 15;
            var spriteFlags = sprite[0];
            uint spriteGfx = sprite[2];
            spriteGfx <<= 8;
            spriteGfx |= sprite[1];
            var x = sprite[3];
            var y = sprite[4];
            var attr = sprite[5];
            var spriteAnim = sprite[6];
            var frame = sprite[7];
            var width = sprite[8];
            var height = sprite[9];
            uint frameOffset = sprite[11];
            frameOffset <<= 8;
            frameOffset |= sprite[10];
            var frameDelay = sprite[12];
            var limitA = sprite[13];
            var limitB = sprite[14];

            // compute path of sprite
            if (limitA!=0 || limitB!=0)
            {
                if ((spriteFlags & 0x10)==0x10) // Left-Right
                {
                    x = ComputePositionPos(x, (byte)(limitA - width * 8), limitB);
                }
                else if ((spriteFlags & 0x20)==0x20) // Right-Left
                {
                    x = ComputePositionNeg(x, limitB, (byte)(limitA - width * 8));
                }
                else if ((spriteFlags & 0x40)==0x40)
                {
                    y = ComputePositionPos(y, limitB, limitA);
                }
                else
                {
                    y = ComputePositionNeg(y, limitA, limitB);
                }
            }


            uint gfxToRender = (uint)(frame * frameOffset) + spriteGfx;

            DrawBigTileXor(y, x, height, width, gfxToRender, attr, false, false);

            if (attr != 0x63)
            {
                PaintAttributes(y, x, height, width, attr);
            }
        }

    }

    private void RenderPickups()
    {
        uint pickupOffset = (int)RollerCoaster_MapDataOffsets.PickupsStart;
        for (int a=0;a<4;a++)
        {
            var pickupX = mapData[pickupOffset++];
            if ((pickupX&0xC0)==0)
            {
                pickupX &= 0x3F;
                var pickupY = mapData[pickupOffset++];
                RenderTile(pickupX, pickupY, GetTile(24), mapData[(int)RollerCoaster_MapDataOffsets.CoinBagColour]);
            }
        }
    }

    private uint RenderSign(uint mapOffset)
    {
        mapOffset++;
        var width = mapData[mapOffset];

        if (width==0)
        {
            return mapOffset;
        }

        if ((width&0x80)==0x80)
        {
            width&=0x7F;
            var attr=mapData[++mapOffset];
            var height = mapData[++mapOffset];
            var column = mapData[++mapOffset];
            var row = mapData[++mapOffset];

            for (uint y=0;y<height;y++)
            {
                for (uint x=0;x<width;x++)
                {
                    var e = mapData[++mapOffset];

                    RenderTile(column+x,row+y,GetTile(e),attr);
                }
            }
        }
        else
        {
            width &= 0x7F;
            var attr = mapData[++mapOffset];
            width += 4;
            byte x = (byte)(16 - width / 2);
            byte y = 17;

            RenderTile(x++, y, GetTile(0x54), attr);        // T
            RenderTile(x++, y, GetTile(0x48), attr);        // H
            RenderTile(x++, y, GetTile(0x45), attr);        // E
            RenderTile(x++, y, GetTile(0x20), attr);        //  
            for (uint a=4;a<width;a++)
            {
                var e = mapData[++mapOffset];
                RenderTile(x++, y, GetTile(e), attr);
            }
        }
        return ++mapOffset;
    }

    private uint RenderCoasterTrack(uint mapOffset)
    {
        var numTracks = mapData[mapOffset];
        mapOffset += 2;
        for (int t = 0; t < numTracks; t++)
        {
            var colour = mapData[(int)RollerCoaster_MapDataOffsets.TrackColour];
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
                RenderTile(a, 16, tileData, mapData[(int)RollerCoaster_MapDataOffsets.Row16Attr]);
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
                    uint cx = (xx + x)&0x1F;
                    uint cy = yy + y;
                    var curAttribute = imageHelper.GetAttribute(cx, cy);
                    if (curAttribute != mapData[(int)RollerCoaster_MapDataOffsets.PaintIgnoreAttr1] && curAttribute != mapData[(int)RollerCoaster_MapDataOffsets.PaintIgnoreAttr2])
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

    // Its sort of a weird boundary fill
    private void PaintAttributes(byte y,byte x,byte numLines,byte numTiles,byte attribute)
    {
        var ax = imageHelper.ConvertXBitmapPosToYAttribute(x);
        var ay = imageHelper.ConvertYBitmapPosToYAttribute(y);

        var yTiles = numLines / 8;

        if ((x&7)!=0)
        {
            numTiles++;
        }

        if ((y & 0x07) != 0)
        {
            yTiles++;
        }
        if ((numLines&7)!=0)
        {
            if (ay>0)
                ay--;
            yTiles++;
        }

        for (y=0;y<yTiles;y++)
        {
            for (x=0;x<numTiles;x++)
            {
                var curAttribute = imageHelper.GetAttribute(ax + x, ay + y);
                if (curAttribute != mapData[(int)RollerCoaster_MapDataOffsets.PaintIgnoreAttr1] && curAttribute != mapData[(int)RollerCoaster_MapDataOffsets.PaintIgnoreAttr2])
                {
                    imageHelper.SetAttribute(ax + x, ay + y, attribute);
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

    // Wrapping in Y is not supported (in fact writing to 18 to 22 will overwrite the score area)
    private void RenderTile(uint xpos, uint ypos, byte[] tile, byte attribute)
    {
        xpos &= 31;
        xpos *= 8;
        ypos *= 8;
        for (uint ty = 0; ty < 8; ty++)
        {
            imageHelper.Draw8Bits(xpos, ypos + ty, tile[ty], attribute, false);
        }
    }


    enum RollerCoaster_MapDataOffsets
    {
        NumberOfSprites = 0x09,
        SpriteDataLow = 0x0A,
        SpriteDataHigh = 0x0B,
        CoinBagColour = 0x0C,
        PaintIgnoreAttr1 = 0x12,
        PaintIgnoreAttr2 = 0x13,
        Row16Attr = 0x14,
        FillColour = 0x1D,
        TrackColour = 0x26,
        BorderColour = 0x30,
        PickupsStart = 0x31,
        Pickup1X = 0x31,
        Pickup1Y = 0x32,
        Pickup2X = 0x33,
        Pickup2Y = 0x34,
        Pickup3X = 0x35,
        Pickup3Y = 0x36,
        Pickup4X = 0x37,
        Pickup4Y = 0x38,
        PickupsLast = 0x38,
        StartOfLevelData = 0x7F,
    }

}