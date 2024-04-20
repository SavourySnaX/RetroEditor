using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

public class Rollercoaster : IRetroPlugin, IImages, IMenuProvider
{
    private byte[][] supportedMD5s = new byte[][] {
        new byte[] { 0xd0, 0x64, 0xb9, 0x2f, 0x76, 0xcb, 0xd3, 0x35, 0xad, 0xc9, 0x84, 0x0f, 0x69, 0x2a, 0x1a, 0xf6 },  // ./Roller Coaster (1986)(Elite Systems).tap
        new byte[] { 0x2f, 0x54, 0x54, 0xf9, 0x0a, 0x37, 0x38, 0xa6, 0xcf, 0x2c, 0x79, 0xe9, 0x61, 0xbf, 0x0c, 0x75 },  // ./Roller Coaster (1986)(Elite Systems).tzx
        new byte[] { 0xda, 0x54, 0x51, 0x5f, 0x31, 0xb7, 0xca, 0x71, 0x61, 0xb9, 0xb5, 0xc8, 0x2d, 0x6f, 0x8f, 0xf7 },  // ./Roller Coaster (1986)(Elite Systems)[a].tzx
        new byte[] { 0x13, 0xcb, 0xdb, 0x06, 0x9f, 0x86, 0xbd, 0x7e, 0xfd, 0x08, 0x04, 0x12, 0xee, 0x15, 0x61, 0x1d },  // ./Roller Coaster (1989)(Encore).tzx
/* 
  For now ignore these, some are hacks, some are password protected cover tapes

        new byte[] { 0xe2, 0xcd, 0xeb, 0xa4, 0x4d, 0x01, 0x04, 0xf7, 0x95, 0x74, 0xad, 0xf2, 0xb1, 0x89, 0xca, 0x37 },  // ./Roller Coaster (1986)(Elite Systems)[a][Sinclair User Covertape][password WINDOW].tzx
        new byte[] { 0x3b, 0x6b, 0xf8, 0x85, 0x0b, 0xc6, 0x09, 0x79, 0x99, 0xe5, 0xc8, 0xae, 0xc8, 0x5f, 0x1a, 0xef },  // ./Roller Coaster (1986)(Elite Systems)[cr JanSoft].tzx
        new byte[] { 0xf9, 0xd1, 0x4c, 0xcc, 0x7d, 0xa9, 0x22, 0x88, 0x46, 0x95, 0x06, 0xb3, 0x02, 0x09, 0x1d, 0x7d },  // ./Roller Coaster (1986)(Elite Systems)[Sinclair User Covertape][password AMAAAA].tzx
        new byte[] { 0x7e, 0xd0, 0x0e, 0x27, 0xdb, 0x1b, 0x5d, 0xda, 0xc9, 0x66, 0x7f, 0x69, 0x01, 0xd3, 0x48, 0xe4 },  // ./Roller Coaster (1986)(Elite Systems)[Sinclair User Covertape][password WINDOW].tzx
        new byte[] { 0xf0, 0xca, 0x9f, 0xe8, 0x5b, 0xa8, 0x82, 0xd8, 0x57, 0x2b, 0x55, 0x07, 0x70, 0x12, 0xfd, 0x86 },  // ./Roller Coaster (1986)(Elite Systems)[h Jansoft].tap
        new byte[] { 0x75, 0xd0, 0x87, 0xdf, 0x00, 0x3c, 0x34, 0x69, 0x2f, 0x1e, 0xe2, 0x88, 0xae, 0x6e, 0x3e, 0x1c },  // ./Roller Coaster (1986)(Elite Systems)[h Jansoft][a].tap
        new byte[] { 0xcc, 0x4d, 0x11, 0xa7, 0x58, 0xcb, 0x09, 0x7f, 0xe1, 0xe4, 0x6a, 0x9f, 0xb9, 0xde, 0x81, 0xb6 }   // ./Roller Coaster (1989)(Encore)[h Drj].tap
*/
    };

    public static string Name => "Rollercoaster";

    public string RomPluginName => "ZXSpectrum";

    public bool RequiresAutoLoad => true;

    public bool CanHandle(string filename)
    {
        // One issue with this approach, is we can't generically load hacks of the game..
        if (!File.Exists(filename))
        {
            return false;
        }
        var md5 = MD5.Create().ComputeHash(File.ReadAllBytes(filename));

        foreach (var supported in supportedMD5s)
        {
            if (supported.SequenceEqual(md5))
            {
                return true;
            }
        }
        return false;
    }


    public int GetImageCount(IRomAccess rom)
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

    public IImage GetImage(IRomAccess rom,int mapIndex)
    {
        return new RollercoasterImage(rom,mapIndex);
    }

    public static ReadOnlySpan<byte> GetLevelTile(IRomAccess rom, int tileIndex)
    {
        return rom.ReadBytes(ReadKind.Ram, (uint)(0x7D00 + (tileIndex * 8)), 8);
    }

    public void ConfigureMenu(IRomAccess rom, IMenu menu)
    {
        var imageMenu = menu.AddItem("Images");
        for (int a = 0; a < GetImageCount(rom); a++)
        {
            var idx = a;    // Otherwise lambda captures last value of a
            var map = GetImage(rom, idx);
            var mapName = map.Name;
            menu.AddItem(imageMenu, mapName, 
                (editorInterface,menuItem) => {
                    var editor = new ImageWindow(this, GetImage(rom, idx));
                    editorInterface.OpenWindow(editor, $"Image {{{mapName}}}");
                });
        }
    }

    public bool AutoLoadCondition(IRomAccess romAccess)
    {
        var memory = romAccess.ReadBytes(ReadKind.Ram, 0x4000, 0x800);  // Load until first third of screen contains title bitmap
        var hash = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        hash.AppendData(memory);
        return hash.GetCurrentHash().SequenceEqual(rollerCoasterAutoLoadHash);
    }

    private readonly byte[] rollerCoasterAutoLoadHash = { 127, 247, 134, 25, 56, 220, 59, 199, 13, 96, 96, 187, 253, 12, 28, 200 };

    public void SetupGameTemporaryPatches(IRomAccess romAccess)
    {
        
    }

    public ISave Export(IRomAccess romAcess)
    {
        // Blankety blank tape for now?
        var tape = new ZXSpectrumTape.Tape();
        return tape;
    }
}

public class RollercoasterImage : IImage
{
    byte[] mapData;
    string mapName;
    int mapIndex;
    int frameCounter;

    ZXSpectrum48ImageHelper imageHelper;

    IRomAccess rom;

    public RollercoasterImage(IRomAccess rom, int mapIndex)
    {
        this.mapIndex = mapIndex;
        this.rom = rom;
        this.mapData = rom.ReadBytes(ReadKind.Ram, GetImageAddress(), 256).ToArray();
        this.mapName = GetMapName();
        imageHelper = new ZXSpectrum48ImageHelper(Width, Height);

        var cnt = mapData[(int)RollerCoaster_MapDataOffsets.NumberOfSprites];
        uint spriteData = mapData[(int)RollerCoaster_MapDataOffsets.SpriteDataHigh];
        spriteData <<= 8;
        spriteData |= mapData[(int)RollerCoaster_MapDataOffsets.SpriteDataLow];
    }

    private uint GetImageAddress()
    {
        if (mapIndex<12)
        {
            return (uint)(0x7100 + (mapIndex * 256));
        }
        if (mapIndex<12+16)
        {
            return (uint)(0x6100 + ((mapIndex - 12) * 256));
        }
        if (mapIndex<12+16+16)
        {
            return (uint)(0xE679 + ((mapIndex-12-16) * 256));
        }
        else
        {
            return (uint)(0xC300 + ((mapIndex - 12 - 16 - 16) * 256));
        }

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

    private ReadOnlySpan<byte> GetTile(int code)
    {
        return Rollercoaster.GetLevelTile(rom,code);
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
            var sprite = rom.ReadBytes(ReadKind.Ram, spriteData, 15);
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

            var attrBytes = rom.ReadBytes(ReadKind.Ram, attrPos, (uint)(innercnt * outercnt));
            var attrOffs = 0;
            for (uint yy = 0; yy < outercnt; yy++)
            {
                for (uint xx = 0; xx < innercnt; xx++)
                {
                    uint cx = (xx + x)&0x1F;
                    uint cy = yy + y;
                    var curAttribute = imageHelper.GetAttribute(cx, cy);
                    if (curAttribute != mapData[(int)RollerCoaster_MapDataOffsets.PaintIgnoreAttr1] && curAttribute != mapData[(int)RollerCoaster_MapDataOffsets.PaintIgnoreAttr2])
                    {
                        var newAttr = attrBytes[attrOffs++];
                        imageHelper.SetAttribute(cx, cy, newAttr);
                    }
                }
            }
        }
        return mapOffset;
    }

    private void DrawBigTileXor(byte y,byte x,byte numLines,byte numTiles,uint srcGfx, byte attribute,bool flipX,bool flipY)
    {
        var bytes = rom.ReadBytes(ReadKind.Ram, srcGfx, (uint)(numLines * numTiles));
        int offs = 0;
        for (uint yy = 0; yy < numLines; yy++)
        {
            uint yyy = (uint)(flipY ? (numLines - 1) - yy : yy);
            for (uint xx = 0; xx < numTiles; xx++)
            {
                uint xxx = (uint)(flipX ? (numTiles - 1) - xx : xx);
                var tileByte = bytes[offs++];

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
    private void RenderTile(uint xpos, uint ypos, ReadOnlySpan<byte> tile, byte attribute)
    {
        xpos &= 31;
        xpos *= 8;
        ypos *= 8;
        for (uint ty = 0; ty < 8; ty++)
        {
            imageHelper.Draw8Bits(xpos, ypos + ty, tile[(int)ty], attribute, false);
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