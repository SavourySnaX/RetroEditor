

public class JetSetWilly48 : IRetroPlugin, IImages, ITileMaps
{
    private byte[] JetSetWilly48Tap = new byte[] { 78, 94, 213, 56, 235, 159, 86, 89, 143, 175, 248, 41, 6, 68, 201, 215 };

    public string Name => "Jet Set Willy 48K";
    IRomPlugin rom;

    public JetSetWilly48()
    {
        rom = new NullRomPlugin();
    }

    public bool CanHandle(byte[] md5, byte[] bytes, string filename)
    {
        // One issue with this approach, is we can't generically load hacks of the game..
        //But perhaps that doesn't matter...
        // MD5 Of Jet Set Willy 48K
        
        return JetSetWilly48Tap.SequenceEqual(md5);
    }

    public bool Init(IEditor editorInterface, byte[] md5, byte[] bytes, string filename)
    {
        var spectrumRomInterface = editorInterface.GetRomInstance("ZXSpectrum");
        if (spectrumRomInterface==null)
        {
            return false;
        }
        rom = spectrumRomInterface;
        if (rom.Load(bytes,"TAP"))
        {
            return true;
        }
        return false;
    }

    public int GetImageCount()
    {
        return 61;
    }

    public void Close()
    {
        rom.Save("C:\\work\\editor\\jsw_patched.tap","TAP");
    }

    public IImages GetImageInterface()
    {
        return this;
    }

    public ITileMaps GetTileMapInterface()
    {
        return this;
    }

    public IImage GetImage(int mapIndex)
    {
        int roomAddress = 0xC000 + (mapIndex * 256);
        return new JetSetWillyMap(this, mapIndex, rom.ReadBytes((uint)roomAddress, 256));
    }
    
    public int GetMapCount()
    {
        return GetImageCount();
    }

    public ITileMap GetMap(int mapIndex)
    {
        int roomAddress = 0xC000 + (mapIndex * 256);
        return new JetSetWilly48TileMap(this, mapIndex, rom.ReadBytes((uint)roomAddress, 256));
    }

    public void SetMap(int mapIndex, byte[] modified)
    {
        int roomAddress = 0xC000 + (mapIndex * 256);
        rom.WriteBytes((uint)roomAddress, modified);
    }

    public ushort GetItemCode(byte itemIndex)
    {
        uint itemTable = 41984;
        var hicode=rom.ReadByte(itemTable + itemIndex);
        var locode=rom.ReadByte(itemTable + itemIndex + 256);
        return (ushort)((hicode<<8) + locode);
    }

    public int GetInitialItemIndex()
    {
        return rom.ReadByte(41983);
    }

    public enum GuardianKind
    {
        None=0,
        Horizontal = 1,
        Vertical = 2,
        Rope = 3,
        Arrow = 4,
    }

    public struct GuardianData
    {
        public byte []bytes;

        public GuardianKind Kind => (GuardianKind)(bytes[0] & 7);
        public bool UpdateEveryPass => ((bytes[0] >> 4) & 1) == 1;
        public int InitialFrame => (bytes[0] >> 5) & 3;
        public bool InverseInitialDirection => ((bytes[0] >> 7) & 1) == 1;
        public int Ink => bytes[1] & 7;
        public bool Bright => ((bytes[1] >> 3) & 1) == 1;
        public int AnimMask => (bytes[1] >> 5) & 7;
        public int SpriteBaseIndex => bytes[2] >> 5;
        public int XCoord => bytes[2] & 0x1F;
        public int PixelYCoord => bytes[3];
        public int GraphicPage => bytes[5];
        public int MinCoord => bytes[6];
        public int MaxCoord => bytes[7];

        public byte RopeInitialFrame => bytes[1];
        public byte RopeLength => bytes[4];
        public byte RopeFrameDirectionChange => bytes[7];

        public bool ArrowRight => ((bytes[0] >> 7) & 1) == 1;
        public byte ArrowYPos => bytes[2];
        public byte ArrowInitialXPos => bytes[4];
        public byte ArrowBitPattern => bytes[6];
    }

    public GuardianData GetGuardianData(uint guardianIndex,byte roomByte)
    {
        // Guardian data is 8 bytes long and starts at 40960 - There is space 15 extra guardians - 0-127 (127 reservered, 112-126 empty)
        var guardianData = rom.ReadBytes(40960 + guardianIndex * 8, 8);

        var guardian = new GuardianData();
        guardian.bytes = guardianData;
        guardian.bytes[2]=roomByte;
        return guardian;
    }

    public byte[] GetSpriteData(byte page, byte spriteIndex)
    {
        var spriteData = rom.ReadBytes((uint)(page * 256 + spriteIndex * 32), 32);
        return spriteData;
    }

    public byte[] GetRopeTable()
    {
        var ropeTable = rom.ReadBytes(33536, 256);
        return ropeTable;
    }

}

public class JetSetWillyMap : IImage
{
    JetSetWilly48 main;
    byte[] mapData;
    string mapName;
    int mapIndex;

    bool flashSwap;
    int conveyorOffset;

    int frameCounter;
    int animFrameCounter;

    public JetSetWillyMap(JetSetWilly48 main, int mapIndex, byte[] data)
    {
        this.main = main;
        this.mapData = data;
        this.mapName = GetMapName();
        this.mapIndex = mapIndex;
    }

    public string GetMapName()
    {
        return System.Text.Encoding.ASCII.GetString(mapData.AsSpan(128, 32)).Trim(' ');
    }


    public uint Width => 256;

    public uint Height => 16*8;

    public string Name => mapName;

    public Pixel[] GetImageData(float seconds)
    {
        return RenderMap(seconds);
    }

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

    private Pixel[] GetTile(int code, bool overrideColour=false, byte colour=0)
    {
        var tile = new Pixel[8 * 8];

        var offset = 128 + 32;
        if (code>5)
        {
            offset += 6 * 9 + 4 + 4 + 1 + 2;    // Item graphic offset
        }
        else
        {
            offset += code * 9;
        }
        var tileData = mapData.AsSpan(offset, 9);

        if (!overrideColour)
        {
            colour = tileData[0];
        }

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

        var tileBytes = overrideColour ? tileData : tileData.Slice(1, 8);

        var paperColour = palette[paper + (bright ? 8 : 0)];
        var inkColour = palette[ink + (bright ? 8 : 0)];

        for (int y = 0; y < 8; y++)
        {
            var row = tileBytes[y];
            for (int x = 0; x < 8; x++)
            {
                var bit = (row >> (7 - x)) & 1;
                tile[y * 8 + x] = bit == 1 ? inkColour : paperColour;
            }
        }

        return tile;
    }

    private Pixel[] RenderMap(float seconds)
    {
        // We should probably convert things to a common editable format - At present I render to bitmap, but thats dumb
        flashSwap = ((int)(seconds*2.0f)&1)==1;
        conveyorOffset=(int)(seconds*20);
        frameCounter=(int)(seconds*25);
        animFrameCounter=(int)(seconds*10);

        // for now, lets just try to render the bitmap -- 
        int ySize = 16;
        int xSize = 8;

        var bitmap = new Pixel[xSize * 8 * 4 * ySize * 8];

        for (int y = 0; y < ySize; y++)
        {
            for (int x = 0; x < xSize; x++)
            {
                byte mapByte = mapData[y * xSize + x];
                for (int b = 0; b < 4; b++)
                {
                    int code = (mapByte >> ((3 - b) * 2)) & 3;

                    // code = 00 background, 01 floor, 10 wall, 11 hazard
                    var tile = GetTile(code);

                    int xpos = x * 8 * 4 + b * 8;
                    int ypos = y * 8;

                    RenderTile(xpos, ypos, bitmap, tile);
                }
            }
        }

        // Unpack conveyor..
        int conveyorDataOffset = 128 + 32 + 6 * 9;
        RenderStairConveyor(5, conveyorDataOffset, bitmap, true);

        // Unpack stairs..
        int stairDataOffset = 128 + 32 + 6 * 9 + 4;
        RenderStairConveyor(4, stairDataOffset, bitmap, false);

        // Unpack items..
        var baseColour = 1+(int)(seconds*6);
        for (int a=main.GetInitialItemIndex();a<256;a++)
        {
            var itemCode = main.GetItemCode((byte)a);
            var xpos = itemCode & 0x1F;
            var ypos = (itemCode >> 5) & 7;
            ypos |= (itemCode & 0x8000) >> 12;
            var room = (itemCode >> 8) & 0x3F;
            if (room == mapIndex)
            {
                var itemTile = GetTile(6, true, (byte)(0x40 | (baseColour & 7)));
                baseColour++;
                if ((baseColour&7)==0)
                {
                    baseColour++;
                }
                RenderTileIgnoreBlack(xpos * 8, ypos * 8, bitmap, itemTile);
            }
        }

        // Unpack Guardians..
        int guardianTableStart = 128 + 32 + 6 * 9 + 4 + 4 + 1 + 2 + 8 + 7;
        for (int a = 0; a < 8; a++)
        {
            var guardianA = mapData[guardianTableStart + a*2];
            var guardianB = mapData[guardianTableStart + a*2+1];

            if (guardianA == 255)
                break;
            var guardianNo=guardianA;

            // Guardian data is 8 bytes long and starts at 40960 - There is space 15 extra guardians - 0-127 (127 reservered, 112-126 empty)
            var data = main.GetGuardianData(guardianNo, guardianB);

            if (data.Kind == JetSetWilly48.GuardianKind.Horizontal || data.Kind == JetSetWilly48.GuardianKind.Vertical)
            {
                RenderSprite(bitmap, data);
            }
            // Todo - Rope / Arrow
            if (data.Kind == JetSetWilly48.GuardianKind.Rope)
            {
                RenderRope(bitmap,data);
            }
            if (data.Kind == JetSetWilly48.GuardianKind.Arrow)
            {
                RenderArrow(bitmap,data);
            }
        }

        return bitmap;
    }

    private void RenderRope(Pixel[] bitmap, JetSetWilly48.GuardianData data)
    {
        int currentFrame = frameCounter % (2*data.RopeFrameDirectionChange);
        int ropeOffset = currentFrame % data.RopeFrameDirectionChange;
        int direction = currentFrame < data.RopeFrameDirectionChange ? 1 : -1;
        direction*=data.InverseInitialDirection ? -1 : 1;
        var ropeTable = main.GetRopeTable();
        int y = 0;
        int x = data.XCoord * 8;
        for (int a=0;a<data.RopeLength;a++)
        {
            bitmap[y * 256 + x] = palette[15];
            y += ropeTable[128 + a + ropeOffset]/2;
            x += direction*ropeTable[a + ropeOffset];
        }
    }

    private void RenderArrow(Pixel[] bitmap, JetSetWilly48.GuardianData data)
    {
        int xPos = data.ArrowInitialXPos;
        var rowPattern = data.ArrowBitPattern;
        if (data.ArrowRight)
        {
            xPos = (xPos + frameCounter) & 255;
        }
        else
        {
            xPos = (xPos - frameCounter) & 255;
        }
        var onScreen = (xPos & 0xE0) == 0;

        if (onScreen)
        {
            var xpos = (xPos & 0x1F)*8;
            var ypos = data.ArrowYPos/2;

            for (int y=0;y<3;y++)
            {
                var row = rowPattern;
                if (y==1)
                {
                    row = 0xFF;
                }
                for (int x=0;x<8;x++)
                {
                    int bit = (row >> (7 - x)) & 1;
                    if (bit!=0)
                    {
                        bitmap[(ypos + y) * 256 + (xpos + x)] = palette[15];
                    }
                }
            }
        }
    }

    private void RenderSprite(Pixel[] bitmap, JetSetWilly48.GuardianData data)
    {
        var animFrame = data.InitialFrame + animFrameCounter;
        animFrame&=data.AnimMask;

        var spriteData = main.GetSpriteData((byte)data.GraphicPage, (byte)(data.SpriteBaseIndex+animFrame));

        for (int y=0;y<16;y++)
        {
            var row = (spriteData[y*2+0]<<8) | spriteData[y*2+1];
            for (int x=0;x<16;x++)
            {
                var bit = (row >> (15 - x)) & 1;
                if (bit!=0)
                {
                    var xpos = data.XCoord*8 + x;
                    var ypos = (data.PixelYCoord/2) + y;
                    bitmap[ypos * 256 + xpos] = palette[data.Ink + (data.Bright ? 8 : 0)];
                }
            }
        }   
    }

    private void RenderStairConveyor(int tileNum, int conveyorDataOffset, Pixel[] bitmap, bool conveyorKind)
    {
        var conveyor = GetTile(tileNum);
        var conveyorDirection = mapData[conveyorDataOffset];
        var conveyorPosA = mapData[conveyorDataOffset + 1];
        var conveyorPosB = mapData[conveyorDataOffset + 2];
        var conveyorLength = mapData[conveyorDataOffset + 3]; 
        var conveyorPos = conveyorPosA + (conveyorPosB << 8);

        conveyorPos -= 24064;
        var conveyorPosX = conveyorPos % 32;
        var conveyorPosY = conveyorPos / 32;

        for (int a=0;a<conveyorLength;a++)
        {
            int xpos = conveyorPosX * 8;
            int ypos = conveyorPosY * 8;
            if (conveyorKind)
            {
                RenderConveyorTile(xpos, ypos, bitmap, conveyor, conveyorDirection == 1, conveyorOffset);
                conveyorPosX++;
            }
            else
            {
                RenderTile(xpos,ypos,bitmap,conveyor);
                switch (conveyorDirection)
                {
                    case 0:
                        conveyorPosY--;
                        conveyorPosX--;
                        break;
                    case 1:
                        conveyorPosY--;
                        conveyorPosX++;
                        break;
                }

            }
        }
    }

    private void RenderTile(int xpos, int ypos, Pixel[] bitmap, Pixel[] tile)
    {
        for (int ty = 0; ty < 8; ty++)
        {
            for (int tx = 0; tx < 8; tx++)
            {
                bitmap[(ypos + ty) * 256 + (xpos + tx)] = tile[ty * 8 + tx];
            }
        }
    }
    
    private void RenderTileIgnoreBlack(int xpos, int ypos, Pixel[] bitmap, Pixel[] tile)
    {
        for (int ty = 0; ty < 8; ty++)
        {
            for (int tx = 0; tx < 8; tx++)
            {
                var pixel = tile[ty * 8 + tx];
                if (pixel.Red != 0 || pixel.Green != 0 || pixel.Blue != 0)
                {
                    bitmap[(ypos + ty) * 256 + (xpos + tx)] = pixel;
                }
            }
        }
    }


    private void RenderConveyorTile(int xpos, int ypos, Pixel[] bitmap, Pixel[] tile, bool rightWard, int offs)
    {
        for (int ty = 0; ty < 8; ty++)
        {
            for (int tx = 0; tx < 8; tx++)
            {
                int x = tx;
                if (ty==0)
                {
                    if (rightWard)
                    {
                        x = (tx + offs) & 7;
                    }
                    else
                    {
                        x = (tx - offs) & 7;
                    }
                }
                if (ty==2)
                {
                    if (rightWard)
                    {
                        x = (tx - offs) & 7;
                    }
                    else
                    {
                        x = (tx + offs) & 7;
                    }
                }
                bitmap[(ypos + ty) * 256 + (xpos + tx)] = tile[ty * 8 + x];
            }
        }
    }
}

public class JetSetWilly48TileMap : ITileMap
{
    JetSetWilly48 main;
    byte[] mapData;
    string mapName;
    int mapIndex;

    bool flashSwap;
    int conveyorOffset;

    int frameCounter;
    int animFrameCounter;

    JetSetWilly48Tile[] tiles;
    JetSetWilly48Layer layer;

    public class JetSetWilly48Tile : ITile
    {
        Pixel[] imageData;
        string name;

        public JetSetWilly48Tile(Pixel[] imageData, string name)
        {
            this.imageData = imageData;
            this.name = name;
        }
    
        public uint Width => 8;

        public uint Height => 8;

        public string Name => name;

        public void Update(Pixel[] imageData)
        {
            this.imageData = imageData;
        }

        public Pixel[] GetImageData()
        {
            return imageData;
        }
    }

    public class JetSetWilly48Layer : ILayer
    {
        JetSetWilly48TileMap map;
        uint[] mapData;

        public JetSetWilly48Layer(JetSetWilly48TileMap map)
        {
            this.map = map;
            mapData = UnpackMapData();
        }

        public uint Width => 32;

        public uint Height => 16;

        public uint[] UnpackMapData()
        {
            int ySize = 16;
            int xSize = 8;
            var mapData = new uint[32 * 16];
            for (int y = 0; y < ySize; y++)
            {
                for (int x = 0; x < xSize; x++)
                {
                    byte mapByte = map.mapData[y * xSize + x];
                    for (int b = 0; b < 4; b++)
                    {
                        mapData[y * 32 + x * 4 + b] = (uint)(mapByte >> ((3 - b) * 2)) & 3;
                    }
                }
            }
            return mapData;
        }

        public void SetTile(uint x, uint y, uint tile)
        {
            mapData[y * 32 + x] = tile;
        }

        public byte[] GetModifiedMap()
        {
            int ySize = 16;
            int xSize = 8;

            byte[] packedData = new byte[16 * 8];
            for (int y = 0; y < ySize; y++)
            {
                for (int x = 0; x < xSize; x++)
                {
                    byte mapByte = 0;
                    for (int b = 0; b < 4; b++)
                    {
                        uint code = mapData[y * 32 + x * 4 + b];
                        mapByte |= (byte)((code&3) << ((3 - b) * 2));
                    }
                    packedData[y * xSize + x] = mapByte;
                }
            }
            return packedData;
        }

        public uint[] GetMapData()
        {
            return this.mapData;
        }

        public Pixel[] GetMapImage()
        {
            int ySize = 16;
            int xSize = 8;

            var bitmap = new Pixel[xSize * 8 * 4 * ySize * 8];

            for (int y = 0; y < ySize; y++)
            {
                for (int x = 0; x < xSize; x++)
                {
                    byte mapByte = map.mapData[y * xSize + x];
                    for (int b = 0; b < 4; b++)
                    {
                        int code = (mapByte >> ((3 - b) * 2)) & 3;

                        // code = 00 background, 01 floor, 10 wall, 11 hazard
                        var tile = map.tiles[code].GetImageData();

                        int xpos = x * 8 * 4 + b * 8;
                        int ypos = y * 8;

                        for (int ty = 0; ty < 8; ty++)
                        {
                            for (int tx = 0; tx < 8; tx++)
                            {
                                bitmap[(ypos + ty) * 256 + (xpos + tx)] = tile[ty * 8 + tx];
                            }
                        }
                    }
                }
            }
            return bitmap;
        }
    }

    public JetSetWilly48TileMap(JetSetWilly48 main, int mapIndex, byte[] data)
    {
        this.main = main;
        this.mapData = data;
        this.mapName = GetMapName();
        this.mapIndex = mapIndex;
        this.tiles = new JetSetWilly48Tile[4];
        this.tiles[0] = new JetSetWilly48Tile(GetTile(0), $"Air");
        this.tiles[1] = new JetSetWilly48Tile(GetTile(1), $"Water");
        this.tiles[2] = new JetSetWilly48Tile(GetTile(2), $"Earth");
        this.tiles[3] = new JetSetWilly48Tile(GetTile(3), $"Fire");
        this.layer = new JetSetWilly48Layer(this);
    }

    public string GetMapName()
    {
        return System.Text.Encoding.ASCII.GetString(mapData.AsSpan(128, 32)).Trim(' ');
    }

    public uint Width => 32 * 8;

    public uint Height => 16 * 8;

    public string Name => mapName;

    public uint NumLayers => 1;

    public uint MaxTiles => 8;

    public ILayer FetchLayer(uint layer)
    {
        return this.layer;
    }

    public ITile[] FetchTiles(uint layer)
    {
        return this.tiles;
    }

    public void Update(float seconds)
    {
        flashSwap = ((int)(seconds*2.0f)&1)==1;
        conveyorOffset=(int)(seconds*20);
        frameCounter=(int)(seconds*25);
        animFrameCounter=(int)(seconds*10);

        for (int a=0;a<4;a++)
        {
            tiles[a].Update(GetTile(a));
        }
    }

    public void Close()
    {
        var data = layer.GetModifiedMap();
        main.SetMap(mapIndex, data);
    }

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

    private Pixel[] GetTile(int code, bool overrideColour=false, byte colour=0)
    {
        var tile = new Pixel[8 * 8];

        var offset = 128 + 32;
        if (code>5)
        {
            offset += 6 * 9 + 4 + 4 + 1 + 2;    // Item graphic offset
        }
        else
        {
            offset += code * 9;
        }
        var tileData = mapData.AsSpan(offset, 9);

        if (!overrideColour)
        {
            colour = tileData[0];
        }

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

        var tileBytes = overrideColour ? tileData : tileData.Slice(1, 8);

        var paperColour = palette[paper + (bright ? 8 : 0)];
        var inkColour = palette[ink + (bright ? 8 : 0)];

        for (int y = 0; y < 8; y++)
        {
            var row = tileBytes[y];
            for (int x = 0; x < 8; x++)
            {
                var bit = (row >> (7 - x)) & 1;
                tile[y * 8 + x] = bit == 1 ? inkColour : paperColour;
                tile[y * 8 + x].Alpha = 255;
            }
        }

        return tile;
    }

}
