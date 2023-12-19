
using SharpGen.Runtime.Win32;
using TextCopy;

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
        //rom.Save("C:\\work\\editor\\jsw_patched.tap","TAP");
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

    bool flashSwap;
    int conveyorOffset;

    int frameCounter;
    int animFrameCounter;

    public RollercoasterImage(Rollercoaster main, int mapIndex, byte[] data)
    {
        this.main = main;
        this.mapData = data;
        this.mapIndex = mapIndex;
        this.mapName = GetMapName();
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

    // Might Make sense to move some of the graphics functionality into a common class, 
    //where you can request a virtual screen size (ie to hold the scrollable screen for the platform)
    //and then have functions to draw tiles. This would allow for colour clash and flash and things
    //to be handled. Think on it...

    static readonly Pixel[] palette = new Pixel[]
    {
        new Pixel() { Red = 0, Green = 0, Blue = 0 },
        new Pixel() { Red = 0, Green = 0, Blue = 192 },
        new Pixel() { Red = 192, Green = 0, Blue = 0 },
        new Pixel() { Red = 192, Green = 0, Blue = 192 },
        new Pixel() { Red = 0, Green = 192, Blue = 0 },
        new Pixel() { Red = 0, Green = 192, Blue = 192 },
        new Pixel() { Red = 192, Green = 192, Blue = 0 },
        new Pixel() { Red = 192, Green = 192, Blue = 192 },
        new Pixel() { Red = 0, Green = 0, Blue = 0 },
        new Pixel() { Red = 0, Green = 0, Blue = 255 },
        new Pixel() { Red = 255, Green = 0, Blue = 0 },
        new Pixel() { Red = 255, Green = 0, Blue = 255 },
        new Pixel() { Red = 0, Green = 255, Blue = 0 },
        new Pixel() { Red = 0, Green = 255, Blue = 255 },
        new Pixel() { Red = 255, Green = 255, Blue = 0 },
        new Pixel() { Red = 255, Green = 255, Blue = 255 }
    };

    private Pixel[] GetTile(int code, byte colour=0)
    {
        var tile = new Pixel[8 * 8];

        var tileData = main.GetLevelTile(code);

        var ink = colour & 7;
        var paper = (colour >> 3) & 7;
        var flash = (colour & 128) != 0;
        var bright = (colour & 64) != 0;

        if (flash && flashSwap)
        {
            var temp = paper;
            paper = ink;
            ink = temp;
        }

        var tileBytes = tileData;

        var paperColour = palette[paper + (bright ? 8 : 0)];
        var inkColour = palette[ink + (bright ? 8 : 0)];

        for (int y = 0; y < 8; y++)
        {
            var row = tileBytes[y];
            for (int x = 0; x < 8; x++)
            {
                var bit = (row >> (7 - x)) & 1;
                tile[y * 8 + x] = bit == 0 ? paperColour : inkColour;
            }
        }

        return tile;
    }

    private Pixel[] RenderMap(float seconds)
    {
        var bitmap = new Pixel[Width * Height];

        flashSwap = ((int)(seconds*2.0f)&1)==1;
        conveyorOffset=(int)(seconds*20);
        frameCounter=(int)(seconds*25);
        animFrameCounter=(int)(seconds*10);

        uint levelDataOffset = 0x7F;

        levelDataOffset = RenderCoasterTrack(bitmap, levelDataOffset);
        levelDataOffset = RenderTileRow16(bitmap, levelDataOffset);
        levelDataOffset = RenderPlatforms(bitmap, levelDataOffset, 0, 1);
        levelDataOffset = RenderPlatforms(bitmap, levelDataOffset, 1, 0);
        levelDataOffset = RenderPlatforms(bitmap, levelDataOffset, 1, 1);
        levelDataOffset = RenderPlatforms(bitmap, levelDataOffset, 1, -1);
        levelDataOffset = RenderLargeTiles(bitmap, levelDataOffset, false);
        levelDataOffset = RenderLargeTiles(bitmap, levelDataOffset, true);

        return bitmap;
    }

    private uint RenderCoasterTrack(Pixel[] bitmap, uint mapOffset)
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
                RenderTrackSegment(bitmap, b, c, ref x, ref y, colour);
            }
        };
        return mapOffset;
    }

    private void RenderTrackSegment(Pixel[] bitmap, byte deltaY, byte deltaX, ref byte x, ref byte y, byte colour)
    {
        var a = 0;
        if (a != deltaY)
        {
            if (a != deltaX)
            {
                if ((deltaY & 0x80) == 0x80)
                {
                    RenderCoasterTrackSegmentHorizontal(bitmap, ref x, ref y, deltaY & 0x7F, 0xFF, colour);
                }
                else
                {
                    RenderCoasterTrackSegmentHorizontal(bitmap, ref x, ref y, deltaY & 0x7F, 0x01, colour);
                }
                return;
            }
            else
            {
                if ((deltaY & 0x80) == 0x80)
                {
                    RenderCoasterTrackSegmentVertical(bitmap, ref x, ref y, deltaY & 0x7F, 0xFF, colour);
                }
                else
                {
                    RenderCoasterTrackSegmentVertical(bitmap, ref x, ref y, deltaY & 0x7F, 0x01, colour);
                }
            }
        }
        else
        {
            RenderCoasterTrackSegmentHorizontal(bitmap, ref x, ref y, deltaX, 0x00, colour);
        }
    }

    private void RenderCoasterTrackSegmentHorizontal(Pixel[] bitmap, ref byte x, ref byte y, int length, byte dy, int colour)
    {
        for (int a = 0; a < length; a++)
        {
            Render4PixelWideVertical(bitmap, x, y, colour);
            x++;
            y+=dy;
        }
    }
    
    private void RenderCoasterTrackSegmentVertical(Pixel[] bitmap, ref byte x, ref byte y, int length, byte dy, int colour)
    {
        for (int a = 0; a < length; a++)
        {
            Render4PixelWideHorizontal(bitmap, x, y, colour);
            y+=dy;
        }
    }


    private void Render4PixelWideVertical(Pixel[] bitmap, int x, int y, int colour)
    {
        var ink = colour & 7;
        var bright = (colour & 64) != 0;
        var inkColour = palette[ink + (bright ? 8 : 0)];
        for (int yy = 0; yy < 4; yy++)
        {
            bitmap[x + (y + yy) * 256] = inkColour;
        }
    }
    
    private void Render4PixelWideHorizontal(Pixel[] bitmap, int x, int y, int colour)
    {
        var ink = colour & 7;
        var bright = (colour & 64) != 0;
        var inkColour = palette[ink + (bright ? 8 : 0)];
        for (int xx = 0; xx < 4; xx++)
        {
            bitmap[x + xx + y * 256] = inkColour;
        }
    }

    private uint RenderTileRow16(Pixel[] bitmap, uint mapOffset)
    {
        int tile = mapData[mapOffset];
        if (tile != 0)
        {
            var tileData = GetTile(tile, mapData[0x14]);
            for (int a = 0; a < 32; a++)
            {
                RenderTile(a, 16, bitmap, tileData);
            }
        }

        return mapOffset;
    }

    private uint RenderLargeTiles(Pixel[] bitmap, uint mapOffset, bool vertical)
    {
        mapOffset++;
        uint cnt = mapData[mapOffset];  //Number of sets of large tiles to draw

        while (true)
        {
            if (cnt==0)
                return mapOffset;
            cnt--;


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

            while (true)
            {
                DrawBigTileXor(bitmap, pixelsY, pixelsX, numLinesY, numTilesX, srcGfx, attribute, flipX, flipY);

                //func -- attribute application - todo has some sort of skip if colour matches logic.. ( we probably need spectrum rendering for this to be obvious )

                if (vertical)
                {
                    pixelsY += numLinesY;
                }
                else
                {
                    pixelsX += (byte)(numTilesX*8);
                }
                if ((--numCopies) == 0)
                    break;
            }
        }
    }

    private void DrawBigTileXor(Pixel[] bitmap,byte y,byte x,byte numLines,byte numTiles,uint srcGfx, byte attribute,bool flipX,bool flipY)
    {
        var ink = attribute & 7;
        var paper = (attribute >> 3) & 7;
        var flash = (attribute & 128) != 0;
        var bright = (attribute & 64) != 0;

        if (flash && flashSwap)
        {
            var temp = paper;
            paper = ink;
            ink = temp;
        }

        var paperColour = palette[paper + (bright ? 8 : 0)];
        var inkColour = palette[ink + (bright ? 8 : 0)];

        for (int yy = 0; yy < numLines; yy++)
        {
            int yyy = flipY ? (numLines - 1) - yy : yy;
            for (int xx = 0; xx < numTiles; xx++)
            {
                int xxx = flipX ? (numTiles - 1) - xx : xx;
                var tileByte = main.GetByte(srcGfx++);
                for (int bb = 0; bb < 8; bb++)
                {
                    int bbb = flipX ? 7 - bb : bb;
                    if ((tileByte & (1 << (7 - bb))) != 0)
                    {
                        bitmap[xxx * 8 + x + bbb + ((yyy + y) * 256)] = inkColour;
                    }
                    else
                    {
                        bitmap[xxx * 8 + x + bbb + ((yyy + y) * 256)] = paperColour;
                    }
                }
            }
        }
    }

    private uint RenderPlatforms(Pixel[] bitmap, uint mapOffset, int colDelta,int rowDelta)
    {
        mapOffset++;
        uint cnt = mapData[mapOffset];
        while (cnt!=0)
        {
            var tile = mapData[++mapOffset];
            var colour = mapData[++mapOffset];
            var tileData = GetTile(tile, colour);
            var len = mapData[++mapOffset];
            var row = mapData[++mapOffset];
            var col = mapData[++mapOffset];

            int x = col;
            int y = row;
            for (int a=0;a<len;a++)
            {
                RenderTile(x, y, bitmap, tileData);
                x+=colDelta;
                y+=rowDelta;
            }
            cnt--;
        }
        return mapOffset;
    }

    private void RenderTile(int xpos, int ypos, Pixel[] bitmap, Pixel[] tile)
    {
        xpos &= 31;
        ypos = Math.Abs(ypos);
        ypos %= 18;
        xpos *= 8;
        ypos *= 8;
        for (int ty = 0; ty < 8; ty++)
        {
            for (int tx = 0; tx < 8; tx++)
            {
                bitmap[(ypos + ty) * 256 + (xpos + tx)] = tile[ty * 8 + tx];
            }
        }
    }
    
}
