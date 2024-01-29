
using System.Security.Cryptography;
using ImGuiNET;

public class JetSetWilly48 : IRetroPlugin, IImages, ITileMaps
{
    private byte[][] supportedMD5s = new byte[][] {
        new byte[] { 0x4E, 0x5E, 0xD5, 0x38, 0xEB, 0x9F, 0x56, 0x59, 0x8F, 0xAF, 0xF8, 0x29, 0x06, 0x44, 0xC9, 0xD7 },  // JetSetWillyTap
        new byte[] { 0x24, 0x0A, 0xE7, 0x91, 0x99, 0x6A, 0x57, 0x94, 0xF9, 0x7D, 0x90, 0xAA, 0x08, 0x00, 0x4E, 0x92 },  // JetSetWillyTzx
        new byte[] { 0x87, 0x9c, 0x75, 0xe6, 0xe6, 0xac, 0x95, 0x75, 0x22, 0xdf, 0x21, 0x18, 0xbf, 0x3c, 0xe3, 0xbe },  // JetSetWillyTzxA
        new byte[] { 0x0f, 0xdb, 0xe2, 0xba, 0x51, 0x8d, 0x70, 0x67, 0x7f, 0xd8, 0x93, 0x75, 0xae, 0x4c, 0x6f, 0xb3 },  // JetSetWillyTzxF
    };

    public static string? Name => "Jet Set Willy 48K";

    public string RomPluginName => "ZXSpectrum";

    public bool RequiresAutoLoad => true;

    public bool CanHandle(string filename)
    {
        // One issue with this approach, is we can't generically load hacks of the game..
        //But perhaps that doesn't matter...
        // MD5 Of Jet Set Willy 48K
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
        return 61;
    }
    
    public ISave Export(IRomAccess romAccess)
    {
        // Make this easier (e.g. add BASIC code helpers)
        // encode start address is spectrum basic format :
        ushort clear = 25000;
        var clearInteger = new byte [] { 0x0E,0x00,0x00, (byte)(clear & 0xFF), (byte)(clear >> 8), 0x00 };
        var clearAscii = System.Text.Encoding.ASCII.GetBytes($"{clear}");
        var start = 0x8400;
        var integer = new byte[] { 0x0E, 0x00, 0x00, (byte)(start & 0xFF), (byte)(start >> 8), 0x00 };
        var ascii = System.Text.Encoding.ASCII.GetBytes($"{start}");
        var clearCode = new byte[] { 0xFD };
        var loadCodeRandUsr = new byte[] { 0x3A, 0xEF, 0x22, 0x22, 0xAF, 0x3A, 0xF9, 0xC0 };
        var endBasic= new byte[] { 0x0D };
        ushort length = (ushort)(clearInteger.Length + clearAscii.Length + clearCode.Length + integer.Length + ascii.Length + loadCodeRandUsr.Length + endBasic.Length);
        var loader = new byte[] { 0x00, 0x0A, (byte)(length & 0xFF), (byte)(length >> 8) }.
            Concat(clearCode).Concat(clearAscii).Concat(clearInteger).
            Concat(loadCodeRandUsr).Concat(ascii).Concat(integer).Concat(endBasic).ToArray();
        var assembled = romAccess.ReadBytes(ReadKind.Ram, 0x8000, 0x8000);
        var outTape = new ZXSpectrumTape.Tape();
        var basicHeader = new ZXSpectrumTape.HeaderBlock(ZXSpectrumTape.HeaderKind.Program, "JSW", (UInt16)loader.Length, 10, (UInt16)loader.Length);
        outTape.AddHeader(basicHeader);
        var basicBlock = new ZXSpectrumTape.DataBlock(loader);
        outTape.AddBlock(basicBlock);
        var header = new ZXSpectrumTape.HeaderBlock(ZXSpectrumTape.HeaderKind.Code, "JSW", (UInt16)assembled.Length,0x8000, 0);
        outTape.AddHeader(header);
        var block = new ZXSpectrumTape.DataBlock(assembled);
        outTape.AddBlock(block);

        return outTape;
    }

    public bool AutoLoadCondition(IRomAccess romAccess)
    {
        var checkMemory = romAccess.ReadBytes(ReadKind.Ram, 0x5800, 768);
        var hash = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        hash.AppendData(checkMemory);
        return hash.GetCurrentHash().SequenceEqual(screenHash);
    }

    public void SetupGameTemporaryPatches(IRomAccess romAccess)
    {
        romAccess.WriteBytes(WriteKind.TemporaryRam, 0x8785, new byte[] { 0xC9 });          // Store return to force out of cheat code key wait
        romAccess.WriteBytes(WriteKind.TemporaryRam, 0x872C, new byte[] { 0xCA, 0x87 });    // Jump to game start
        romAccess.WriteBytes(WriteKind.TemporaryRam, 0x88AC, new byte[] { 0xFC, 0x88 });    // start game

        byte yPos = 13 * 8;
        byte xPos = 1 * 8;
        byte roomNumber = 0x22;

        ushort attributeAddress = (ushort)(0x5C00 + ((yPos / 8) * 32) + (xPos / 8));

        romAccess.WriteBytes(WriteKind.TemporaryRam, 0x87E6, new byte[] { (byte)(yPos * 2) });          // willys y cordinate
        romAccess.WriteBytes(WriteKind.TemporaryRam, 0x87F0, new byte[] { (byte)(attributeAddress & 0xFF), (byte)(attributeAddress >> 8) });    // willys cordinate
        romAccess.WriteBytes(WriteKind.TemporaryRam, 0x87EB, new byte[] { (byte)(roomNumber) });
    }


    private readonly byte[] screenHash = { 27, 10, 249, 194, 93, 180, 162, 138, 198, 11, 210, 12, 245, 143, 226, 53 };

    public void Close()
    {
    }

    public void Menu(IRomAccess rom, IEditor editorInterface)
    {
        if (ImGui.BeginMenu("Image Viewer"))
        {
            for (int a = 0; a < GetImageCount(rom); a++)
            {
                var map = GetImage(rom,a);
                var mapName = map.Name;
                if (ImGui.MenuItem(mapName))
                {
                    editorInterface.OpenWindow(new ImageWindow(this,GetImage(rom,a)), $"Image {{{mapName}}}");
                }
            }
            ImGui.EndMenu();
        }
        if (ImGui.BeginMenu("Tile Map Editor"))
        {
            for (int a = 0; a < GetMapCount(rom); a++)
            {
                var map = GetMap(rom,a);
                var mapName = map.Name;
                if (ImGui.MenuItem(mapName))
                {
                    editorInterface.OpenWindow(new TileMapEditorWindow(this, map), $"Tile Map {{{mapName}}}");
                }
            }
            ImGui.EndMenu();
        }
    }

    public IImages GetImageInterface()
    {
        return this;
    }

    public ITileMaps GetTileMapInterface()
    {
        return this;
    }

    public IImage GetImage(IRomAccess rom, int mapIndex)
    {
        return new JetSetWillyMap(rom, mapIndex);
    }
    
    public int GetMapCount(IRomAccess rom)
    {
        return GetImageCount(rom);
    }

    public ITileMap GetMap(IRomAccess rom,int mapIndex)
    {
        return new JetSetWilly48TileMap(rom, mapIndex);
    }

    public static void SetMap(IRomAccess rom, int mapIndex, byte[] modified)
    {
        int roomAddress = 0xC000 + (mapIndex * 256);
        rom.WriteBytes(WriteKind.SerialisedRam, (uint)roomAddress, modified);
    }

    public static ushort GetItemCode(IRomAccess rom, byte itemIndex)
    {
        uint itemTable = 41984;
        var hicode = rom.ReadBytes(ReadKind.Ram, itemTable+itemIndex, 1)[0];
        var locode = rom.ReadBytes(ReadKind.Ram, itemTable + itemIndex + 256, 1)[0];
        return (ushort)((hicode<<8) + locode);
    }

    public static byte GetInitialItemIndex(IRomAccess rom)
    {
        return rom.ReadBytes(ReadKind.Ram, 419783,1)[0];
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

    public static GuardianData GetGuardianData(IRomAccess rom, uint guardianIndex,byte roomByte)
    {
        // Guardian data is 8 bytes long and starts at 40960 - There is space 15 extra guardians - 0-127 (127 reservered, 112-126 empty)
        var guardianData = rom.ReadBytes(ReadKind.Ram, 40960 + guardianIndex * 8, 8);

        var guardian = new GuardianData();
        guardian.bytes = guardianData.ToArray();
        guardian.bytes[2]=roomByte;
        return guardian;
    }

    public static ReadOnlySpan<byte> GetSpriteData(IRomAccess rom, byte page, byte spriteIndex)
    {
        return rom.ReadBytes(ReadKind.Ram, (uint)(page * 256 + spriteIndex * 32), 32);
    }

    public static ReadOnlySpan<byte> GetRopeTable(IRomAccess rom)
    {
        return rom.ReadBytes(ReadKind.Ram, 33536, 256);
    }

    public static ReadOnlySpan<byte> GetWilly(IRomAccess rom)
    {
        return rom.ReadBytes(ReadKind.Ram, 0x9D00, 256);
    }

    public static ReadOnlySpan<byte> GetMapTile(ReadOnlySpan<byte> mapData, int code)
    {
        var offset = 128 + 32;
        if (code>5)
        {
            offset += 6 * 9 + 4 + 4 + 1 + 2;    // Item graphic offset
        }
        else
        {
            offset += code * 9;
        }
        return mapData.Slice(offset, 9);
    }


}

public class JetSetWillyMap : IImage
{
    byte[] mapData;
    string mapName;
    int mapIndex;

    bool flashSwap;
    int conveyorOffset;

    int frameCounter;
    int animFrameCounter;

    IRomAccess rom;

    public JetSetWillyMap(IRomAccess rom, int mapIndex)
    {
        this.rom = rom;
        int roomAddress = 0xC000 + (mapIndex * 256);
        this.mapData = rom.ReadBytes(ReadKind.Ram, (uint)roomAddress, 256).ToArray(); 
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
/*
    public Pixel[] GetSpriteData(float seconds)
    {
        var helper = new ZXSpectrum48ImageHelper(Width, Height);

        helper.Clear(0x07);

        var willy = main.GetWilly();

        for (uint y=0;y<16;y++)
        {
            for (uint x=0;x<2;x++)
            {
                var gfx = willy[y * 2 + x];
                helper.Draw8BitsNoAttribute(x * 8, y, gfx, false);
            }
        }

        return helper.Render(seconds);
        //return RenderMap(seconds);
    }
*/

    private Pixel[] RenderMap(float seconds)
    {
        // We should probably convert things to a common editable format - At present I render to bitmap, but thats dumb
        flashSwap = ((int)(seconds*2.0f)&1)==1;
        conveyorOffset=(int)(seconds*20);
        frameCounter=(int)(seconds*25);
        animFrameCounter=(int)(seconds*10);

        uint ySize = 16;
        uint xSize = 8;

        var spectrumScreen = new ZXSpectrum48ImageHelper(xSize * 8 * 4, ySize * 8);

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
                    var tile = JetSetWilly48.GetMapTile(mapData, code);

                    uint xpos = (uint)(x * 8 * 4 + b * 8);
                    uint ypos = (uint)(y * 8);

                    spectrumScreen.Draw8x8(xpos, ypos, tile.Slice(1), tile[0]);
                }
            }
        }

        // Unpack conveyor..
        int conveyorDataOffset = 128 + 32 + 6 * 9;
        RenderStairConveyor(5, conveyorDataOffset, spectrumScreen, true);

        // Unpack stairs..
        int stairDataOffset = 128 + 32 + 6 * 9 + 4;
        RenderStairConveyor(4, stairDataOffset, spectrumScreen, false);
        
        // Unpack items..
        var baseColour = 1+(int)(seconds*6);
        for (int a=JetSetWilly48.GetInitialItemIndex(rom);a<256;a++)
        {
            var itemCode = JetSetWilly48.GetItemCode(rom,(byte)a);
            var xpos = itemCode & 0x1F;
            var ypos = (itemCode >> 5) & 7;
            ypos |= (itemCode & 0x8000) >> 12;
            var room = (itemCode >> 8) & 0x3F;
            if (room == mapIndex)
            {
                var itemTile = JetSetWilly48.GetMapTile(mapData, 6);
                baseColour++;
                if ((baseColour&7)==0)
                {
                    baseColour++;
                }
                spectrumScreen.Draw8x8InkOnly((uint)(xpos * 8), (uint)(ypos * 8), itemTile.Slice(1), (byte)(0x40 | (baseColour & 7)));
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
            var data = JetSetWilly48.GetGuardianData(rom,guardianNo, guardianB);

            if (data.Kind == JetSetWilly48.GuardianKind.Horizontal || data.Kind == JetSetWilly48.GuardianKind.Vertical)
            {
                RenderSprite(spectrumScreen, data);
            }
            if (data.Kind == JetSetWilly48.GuardianKind.Rope)
            {
                RenderRope(spectrumScreen,data);
            }
            if (data.Kind == JetSetWilly48.GuardianKind.Arrow)
            {
                RenderArrow(spectrumScreen,data);
            }
        }

        return spectrumScreen.Render(seconds);
    }

    private void RenderRope(ZXSpectrum48ImageHelper screen, JetSetWilly48.GuardianData data)
    {
        int currentFrame = frameCounter % (2*data.RopeFrameDirectionChange);
        int ropeOffset = currentFrame % data.RopeFrameDirectionChange;
        int direction = currentFrame < data.RopeFrameDirectionChange ? 1 : -1;
        direction*=data.InverseInitialDirection ? -1 : 1;
        var ropeTable = JetSetWilly48.GetRopeTable(rom);
        int y = 0;
        int x = data.XCoord * 8;
        for (int a=0;a<data.RopeLength;a++)
        {
            screen.DrawBitNoAttribute((uint)x, (uint)y, true);
            screen.SetAttribute((uint)(x>>8),(uint)(y>>8),0x07);
            y += ropeTable[128 + a + ropeOffset]/2;
            x += direction*ropeTable[a + ropeOffset];
        }
    }

    private void RenderArrow(ZXSpectrum48ImageHelper screen, JetSetWilly48.GuardianData data)
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
                screen.Draw8BitsInkOnly((uint)xpos,(uint)(ypos+y),row,0x07,false);
            }
        }
    }

    private void RenderSprite(ZXSpectrum48ImageHelper screen, JetSetWilly48.GuardianData data)
    {
        var animFrame = data.InitialFrame + animFrameCounter;
        animFrame&=data.AnimMask;

        var spriteData = JetSetWilly48.GetSpriteData(rom, (byte)data.GraphicPage, (byte)(data.SpriteBaseIndex+animFrame));
        for (int y=0;y<16;y++)
        {
            for (int x=0;x<2;x++)
            {
                var xpos = data.XCoord*8 + x*8;
                var ypos = (data.PixelYCoord/2) + y;

                screen.Draw8BitsInkOnly((uint)xpos, (uint)ypos, spriteData[y*2+x], (byte)(data.Ink + (data.Bright ? 8 : 0)), false);
            }
        }
    }

    private void RenderStairConveyor(int tileNum, int conveyorDataOffset, ZXSpectrum48ImageHelper spectrumScreen, bool conveyorKind)
    {
        var conveyor = JetSetWilly48.GetMapTile(mapData, tileNum);
        var conveyorDirection = mapData[conveyorDataOffset];
        var conveyorPosA = mapData[conveyorDataOffset + 1];
        var conveyorPosB = mapData[conveyorDataOffset + 2];
        var conveyorLength = mapData[conveyorDataOffset + 3]; 
        uint conveyorPos = conveyorPosA + (uint)(conveyorPosB << 8);

        conveyorPos -= 24064;
        uint conveyorPosX = conveyorPos % 32;
        uint conveyorPosY = conveyorPos / 32;

        if (conveyorKind)
        {
            var dupeForConveyor = conveyor.ToArray();
            var amount = conveyorOffset & 7;
            // Rotate first and third rows by offset
            if (conveyorDirection==1)
            {
                dupeForConveyor[1] = (byte)((conveyor[0] >> amount) | (conveyor[0] << (8 - amount)));
                dupeForConveyor[3] = (byte)((conveyor[0] << amount) | (conveyor[0] >> (8 - amount)));
            }
            else
            {
                dupeForConveyor[1] = (byte)((conveyor[0] << amount) | (conveyor[0] >> (8 - amount)));
                dupeForConveyor[3] = (byte)((conveyor[0] >> amount) | (conveyor[0] << (8 - amount)));
            }
            conveyor = dupeForConveyor;
        }

        for (int a=0;a<conveyorLength;a++)
        {
            uint xpos = conveyorPosX * 8;
            uint ypos = conveyorPosY * 8;
            if (conveyorKind)
            {
                spectrumScreen.Draw8x8(xpos, ypos, conveyor.Slice(1), conveyor[0]);
                conveyorPosX++;
            }
            else
            {
                spectrumScreen.Draw8x8(xpos, ypos, conveyor.Slice(1), conveyor[0]);
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
}

public class JetSetWilly48TileMap : ITileMap
{
    byte[] mapData;
    string mapName;
    int mapIndex;

    bool flashSwap;
    int conveyorOffset;

    int frameCounter;
    int animFrameCounter;

    ZXSpectrum48ImageHelper[] helpers;
    JetSetWilly48Tile[] tiles;
    JetSetWilly48Layer layer;
    IRomAccess rom;

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

    }

    public JetSetWilly48TileMap(IRomAccess rom, int mapIndex)
    {
        int roomAddress = 0xC000 + (mapIndex * 256);
        this.rom = rom;
        this.mapData = rom.ReadBytes(ReadKind.Ram, (uint)roomAddress, 256).ToArray();
        this.mapName = GetMapName();
        this.mapIndex = mapIndex;

        this.helpers = new ZXSpectrum48ImageHelper[4];
        for (int a=0;a<4;a++)
        {
            helpers[a] = new ZXSpectrum48ImageHelper(8, 8);
            var tile = JetSetWilly48.GetMapTile(mapData, a);
            helpers[a].Draw8x8(0, 0, tile.Slice(1), tile[0]);
        }
        this.tiles = new JetSetWilly48Tile[4];
        this.tiles[0] = new JetSetWilly48Tile(helpers[0].Render(0), $"Air");
        this.tiles[1] = new JetSetWilly48Tile(helpers[1].Render(0), $"Water");
        this.tiles[2] = new JetSetWilly48Tile(helpers[2].Render(0), $"Earth");
        this.tiles[3] = new JetSetWilly48Tile(helpers[3].Render(0), $"Fire");
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
            tiles[a].Update(helpers[a].Render(seconds));
        }
        
        var data = layer.GetModifiedMap();
        JetSetWilly48.SetMap(rom, mapIndex, data);
    }

    public void Close()
    {
        var data = layer.GetModifiedMap();
        JetSetWilly48.SetMap(rom, mapIndex, data);
    }
}
