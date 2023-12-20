
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
        levelDataOffset = RenderSign(levelDataOffset);

        RenderPickups();

        return imageHelper.Render(seconds);
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
        var cnt = mapData[mapOffset];

        if (cnt==0)
        {
            return mapOffset;
        }

        if ((cnt&0x80)==0x80)
        {
            cnt&=0x7F;
            var attr=mapData[++mapOffset];
            var outer = mapData[++mapOffset];
            var c = mapData[++mapOffset];
            var a = mapData[++mapOffset];

            for (uint y=0;y<outer;y++)
            {
                for (uint x=0;x<cnt;x++)
                {
                    var e = mapData[++mapOffset];

                    RenderTile(c+x,a+y,GetTile(e),attr);
                }
            }
        }
        else
        {
            throw new Exception("Not implemented - not used in levels - cut ? looks like it might display T H E  ");

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
                    uint cx = xx + x;
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

    private void RenderTile(uint xpos, uint ypos, byte[] tile, byte attribute)
    {
        xpos &= 31;
        //ypos = Math.Abs(ypos);    // To check, is this built in, need to modify the map data`
        //ypos %= 18;
        xpos *= 8;
        ypos *= 8;
        for (uint ty = 0; ty < 8; ty++)
        {
            imageHelper.Draw8Bits(xpos, ypos + ty, tile[ty], attribute, false);
        }
    }


    enum RollerCoaster_MapDataOffsets
    {
        CoinBagColour = 0x0C,
        PaintIgnoreAttr1 = 0x12,
        PaintIgnoreAttr2 = 0x13,
        Row16Attr = 0x14,
        FillColour = 0x1D,
        TrackColour = 0x26,
        BorderColour = 0x30,
        PickupsStart = 0x31,
        StartOfLevelData = 0x7F,
    }

}


// Roller Coaster MapData (per room 256 bytes)

// Start|End   | Description

// 0x00 | 0x04 | Overwritten by the game with data from AFF5
// 0x0C | 0x0C | Coin Bag Colour
// 0x12 | 0x12 | Attribute to ignore when painting attributes
// 0x13 | 0x13 | Attribute to ignore when painting attributes
// 0x14 | 0x14 | Attribute for tile row 16
// 0x15 | 0x19 | Overwritten by the game with data from AFFA
// 0x1A | 0x1A | Level flags of some sort,   Level1Room1 bits  0=clear, 1=clear, 4=clear
// 0x1C | 0x1C | Level flags of some sort,   Level1Room1 bits  3=set (draw ferris wheel thing) 
// 0x1D | 0x1D | Fill Colour for empty tiles in the room
// 0x26 | 0x26 | Colour for the track
// 0x30 | 0x30 | Border Colour for the room
// 0x31 | 0x38 | Pickups (space for 4, if bit 6/7 set, then pickup skipped? otherwise, its the y co-ordinate, x follows in next byte (tile coords))
// 0x43 | 0x43 | Level flags of some sort,   Level1Room1 bits  7=clear, 6=set (possibly indicates start room - initialise stuff)
// 0x44 | 0x45 | used in routine 9024  ()
// 0x7F | 0xFF | Platforms/Tracks/Signs/BigTiles/Attributes
//


// 0x7DC0 | 0x7DC7 | Money Bag Graphic  (index 24)

// 0x8272 - low byte of score to trigger end   (original game has 24000 as the trigger score  (93 coin bags) )
// 0x8276 - hi byte of score to trigger end

// end condition adds 10000 to score, then restarts the game.. (this restart only triggers on an exact score of 24000 so reset can only happen once)