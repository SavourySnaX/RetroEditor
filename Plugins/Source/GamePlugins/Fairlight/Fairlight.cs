using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using RetroEditor.Plugins;

public class Fairlight : IRetroPlugin, IMenuProvider
{
    // This is MD5 of a ram dump of the game, will update with tap/tzx version later 
    private byte[][] supportedMD5s = new byte[][] {
        new byte[] { 0x7e, 0x7d, 0x2d, 0xce, 0x09, 0xae, 0x41, 0xf9, 0x8f, 0x7e, 0x21, 0x7a, 0x3d, 0x4d, 0xe1, 0xe0 }, // ./Fairlight - A Prelude - The Light Revealed (1985)(Edge, The)(48K-128K).tap

    /* 
     Unsupport for now (128K or current auto load fails - or offsets dont match)
        new byte[] { 0x87, 0xe9, 0xda, 0x89, 0xe9, 0x2c, 0xb1, 0x7c, 0x59, 0xda, 0x9c, 0x92, 0x49, 0xb6, 0xb4, 0x2c }, // ./Fairlight (1985)(Micro Selection, The)(48K-128K)[re-release].tzx
        new byte[] { 0x4f, 0x09, 0x7f, 0x83, 0xf3, 0x3e, 0x32, 0x89, 0x94, 0x96, 0x88, 0xe8, 0x60, 0xd9, 0x24, 0xdb }, // ./Fairlight - A Prelude - The Light Revealed (1985)(Edge, The)(48K-128K)(Side A)[m tzxtools][Alkatraz Protection System].tap
        new byte[] { 0xe8, 0x54, 0xa7, 0xd7, 0xee, 0xe3, 0xc1, 0xbe, 0xde, 0x71, 0xbb, 0xce, 0xdd, 0x29, 0xcb, 0x6b }, // ./Fairlight - A Prelude - The Light Revealed (1985)(Edge, The)(48K-128K)[h Coda].tap
        new byte[] { 0x64, 0x05, 0x01, 0xbf, 0x82, 0x8a, 0xf1, 0x18, 0x55, 0x97, 0x33, 0x6d, 0xec, 0x9a, 0xf8, 0xda }, // ./Fairlight - A Prelude - The Light Revealed (1985)(Edge, The)(48K-128K)[h Future Soft].tap
        new byte[] { 0x5e, 0x90, 0xa1, 0x86, 0x53, 0xa4, 0x82, 0x52, 0x4e, 0xca, 0x43, 0xf4, 0x10, 0x22, 0x39, 0xcf }, // ./Fairlight - A Prelude - The Light Revealed (1985)(Edge, The)(48K-128K)[h Prospekt][tr ru].tap
        new byte[] { 0x76, 0xfc, 0x13, 0x49, 0x74, 0xdc, 0xa0, 0xd6, 0x32, 0x5c, 0xde, 0x41, 0xa2, 0x53, 0xd4, 0xce }, // ./Fairlight - A Prelude - The Light Revealed (1985)(Edge, The)(48K-128K)[a].tzx
        new byte[] { 0x45, 0xe3, 0xa9, 0x77, 0x9a, 0xa3, 0x5d, 0x1c, 0xa4, 0x68, 0x58, 0xde, 0xdf, 0x6c, 0xbb, 0x18 }, // ./Fairlight - A Prelude - The Light Revealed (1985)(Edge, The)(48K-128K)[h].tzx
        new byte[] { 0x2b, 0x9b, 0x69, 0x2d, 0x79, 0x97, 0xfb, 0xb8, 0xdc, 0xe5, 0x78, 0x08, 0xd4, 0xfd, 0x60, 0xb2 }, // ./Fairlight - A Prelude - The Light Revealed (1985)(Edge, The)(128K)[Alkatraz Protection System].tzx
        new byte[] { 0x24, 0x09, 0xd2, 0xfd, 0x90, 0x23, 0x8e, 0x2f, 0x66, 0xec, 0x5f, 0x0a, 0x3d, 0x6a, 0x60, 0xea }, // ./Fairlight - A Prelude - The Light Revealed (1985)(Edge, The)(128K)[h].tzx
        new byte[] { 0xda, 0x6d, 0xa4, 0xd8, 0x79, 0x8e, 0xf4, 0xc1, 0x15, 0x7f, 0x67, 0x9e, 0xdb, 0xbf, 0xcf, 0xc6 }, // ./Fairlight - A Prelude - The Light Revealed (1985)(Edge, The)(128K).tap
        new byte[] { 0x3f, 0x39, 0x55, 0xd9, 0xf6, 0xdc, 0x4c, 0x13, 0x02, 0x82, 0x3b, 0x24, 0x19, 0x84, 0x7d, 0x8c }, // ./Fairlight - A Prelude - The Light Revealed (1985)(Edge, The)(128K)[cr Matasoft][t Matasoft].tap
        new byte[] { 0x31, 0x4d, 0x7d, 0xc2, 0x7f, 0x8e, 0xe5, 0x84, 0xc9, 0x6b, 0x63, 0x84, 0x09, 0xa2, 0x76, 0x5c }, // ./Fairlight (1985)(ABC Soft)(48K-128K)(ES)(en)[Alkatraz Protection System][re-release].tzx
        new byte[] { 0xee, 0x6a, 0xcd, 0x81, 0x88, 0x3c, 0x4d, 0xa7, 0xe7, 0x8f, 0x81, 0x20, 0x77, 0xc6, 0xff, 0x62 }, // ./Fairlight - A Prelude - The Light Revealed (1985)(Edge, The)[Alkatraz Protection System].tzx
        new byte[] { 0x1d, 0xba, 0x2a, 0xc5, 0x3f, 0xd2, 0x5f, 0x4c, 0xc1, 0x06, 0x5e, 0x18, 0xe3, 0x1a, 0x7b, 0x96 }, // ./Fairlight - A Prelude - The Light Revealed (1985)(Edge, The).tzx
        new byte[] { 0x56, 0xad, 0x38, 0x92, 0xa2, 0x1e, 0xc1, 0xfd, 0x46, 0x99, 0x0c, 0x54, 0x7d, 0xcd, 0x0d, 0xe2 }, // ./Fairlight - A Prelude - The Light Revealed (1985)(Edge, The)(48K-128K).tzx

    */
    };

    public static string Name => "Fairlight";

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

    public void ConfigureMenu(IMemoryAccess rom, IMenu menu)
    {
        var imageMenu = menu.AddItem("Images");
        for (int a = 0; a < GetImageCount(rom); a++)
        {
            var idx = a;    // Otherwise lambda captures last value of a
            var mapName = GetMapName(idx);
            menu.AddItem(imageMenu, mapName, 
                (editorInterface,menuItem) => {
                    editorInterface.OpenUserWindow(mapName, GetImage(rom, idx));
                });
        }
    }

    private int fastLoadWait = 0;
    public bool AutoLoadCondition(IMemoryAccess romAccess)
    {
        // Since there is no unique screen to catch after loading, we wait for a sequence of patterns in memory instead
        var memory = romAccess.ReadBytes(ReadKind.Ram, 0xFF58, 24);
        if (memory.SequenceEqual(matchRam))
        {
            fastLoadWait++;
        }
        return fastLoadWait>100;    // Safe to stop now -- TODO vastly improve this stop detection
    }

    private byte[] matchRam = new byte[] { 0xFF, 0xFD, 0xB6, 0x16, 0xC8, 0xE5, 0xCD, 0x03, 0xF2, 0xE1, 0x3A, 0x83, 0xFF, 0xFD, 0x34, 0x03, 0xFD, 0xBE, 0x00, 0xCA, 0x68, 0xFE, 0x11, 0x14 };

    public void SetupGameTemporaryPatches(IMemoryAccess romAccess)
    {
        
    }

    public ISave Export(IMemoryAccess romAcess)
    {
        // Blankety blank tape for now?
        var tape = new ZXSpectrumTape.Tape();
        return tape;
    }

    public int GetImageCount(IMemoryAccess rom)
    {
        return 64;
    }

    public FairlightImage GetImage(IMemoryAccess rom,int mapIndex)
    {
        var tableStart = FetchTableAddress(rom, 0x68B0, mapIndex);
        return new FairlightImage(rom, mapIndex, tableStart);
    }
    
    public string GetMapName(int mapIndex)
    {
        return $"Room {mapIndex}";
    }


    public static ushort FetchTableAddress(IMemoryAccess rom,ushort baseAddress,int mapIndex)
    {
        var tableStart = baseAddress;
        ushort skip = 0;
        for (int a=0;a<=mapIndex;a++)
        {
            tableStart += skip;
            var bytes = rom.ReadBytes(ReadKind.Ram, tableStart, 2);
            skip = rom.FetchMachineOrder16(0, bytes);
        }

        tableStart += 2;

        return tableStart;
    }

    public static byte GetByte(IMemoryAccess rom, uint address)
    {
        return rom.ReadBytes(ReadKind.Ram, address, 1)[0];
    }

    public static ReadOnlySpan<byte> GetBytes(IMemoryAccess rom,uint address, uint length)
    {
        return rom.ReadBytes(ReadKind.Ram, address, length);
    }
}

public class FairlightImage : IImage, IUserWindow
{
    int mapIndex;
    ushort mapAddress;

    IMemoryAccess rom;

    ZXSpectrum48ImageHelper screen;
    ZXSpectrum48ImageHelper fillScreen;

    Stack<State> stateStack = new Stack<State>();

    class State
    {
        public State(ushort address)
        {
            this.address = address;
            nextX = 0;
            nextY = 0;
            lastX = 0;
            lastY = 0;
            flagBit0 = false;
            flagBit1 = false;
            flagBit2 = false;
            flagBit3 = false;
            flagBit4 = false;
            drawLine = false;
            flagBit6 = false;
            flagBit7 = false;
            loopCount = 0;
            loopPoint = 0;
            patternAddress = 0;
        }

        public State(ushort address, State other)
        {
            this.address = address;
            nextX = other.nextX;
            nextY = other.nextY;
            lastX = other.lastX;
            lastY = other.lastY;
            flagBit0 = other.flagBit0;
            flagBit1 = other.flagBit1;
            flagBit2 = other.flagBit2;
            flagBit3 = other.flagBit3;
            flagBit4 = other.flagBit4;
            drawLine = other.drawLine;
            flagBit6 = other.flagBit6;
            flagBit7 = other.flagBit7;
            loopCount = other.loopCount;
            loopPoint = other.loopPoint;
            patternAddress = other.patternAddress;
        }

        public ushort address;
        public ushort patternAddress;
        public byte nextX, nextY;
        public byte lastX, lastY;
        public byte loopCount;
        public ushort loopPoint;

        public bool flagBit0;
        public bool flagBit1;
        public bool flagBit2;
        public bool flagBit3;
        public bool flagBit4;
        public bool drawLine;
        public bool flagBit6;
        public bool flagBit7;
    }

    public FairlightImage(IMemoryAccess rom, int mapIndex, ushort mapAddress)
    {
        this.rom = rom;
        this.mapAddress = mapAddress;
        this.mapIndex = mapIndex;
        screen = new ZXSpectrum48ImageHelper(Width, Height);
        fillScreen = new ZXSpectrum48ImageHelper(Width, Height);
    }

    public uint Width => 256;

    public uint Height => 192;

    public float ScaleX => 2.0f;

    public float ScaleY => 2.0f;

    public ReadOnlySpan<Pixel> GetImageData(float seconds)
    {
        return RenderMap(seconds);
    }

    int lineCounter;
    int maxLine;
    int fillCounter;
    int maxFill;

    bool iy37bit0;
    private Pixel[] RenderMap(float seconds)
    {
        stateStack.Clear();

        var currentState = new State(mapAddress);

        screen.Clear(0x38);
        fillScreen.Clear(0);

        lineCounter = 0;
        maxLine = 99999;
        fillCounter = 0;
        maxFill = 999;
        iy37bit0 = false;

        var ff8e = Fairlight.GetByte(rom, currentState.address++);
        ProcessCommands(currentState);

        screen.FlipVertical();

        return screen.Render(seconds);
    }

    private bool ProcessCommands(State currentState)
    {
        byte index = default;

        while (true)
        {
            byte code = Fairlight.GetByte(rom, currentState.address++);

            if (code == 0xE5)   // END
                return true;

            if (currentState.flagBit7)
            {
                if (code!=0xE4)
                {
                    if (code<0xE4)
                    {
                        // TODO
                        currentState.address += 4;
                    }
                    else
                    {
                        //TODO
                    }
                }
                //throw new Exception($"Unhandled command {code:X} at {currentState.address - 1:X}");
            }
            else
            {
                if (code < 0xC0)    // MOVE
                {
                    var nY = code;
                    var a = Fairlight.GetByte(rom, currentState.address++);
                    if (currentState.flagBit6)
                    {
                        if (currentState.flagBit4)
                        {
                            a = (byte)(0 - a);
                        }
                        else
                        {
                            a ^= 0xFF;
                        }
                    }
                    var nX = a;
                    if (currentState.flagBit4)
                    {
                        nX += currentState.nextX;

                        if (nY + currentState.nextY > 0xFF)
                        {
                            nY += currentState.nextY;
                            nY += 0x40;
                        }
                        else
                        {
                            nY += currentState.nextY;
                            if (nY >= 0xC0)
                                nY += 0x40;
                        }
                    }
                    if (currentState.flagBit3)
                    {
                        var aa = currentState.lastX;
                        aa += nX;
                        aa -= currentState.nextX;
                        currentState.lastX = aa;
                        aa=currentState.lastY;
                        aa += nY;
                        aa -= currentState.nextY;
                        if (aa>=0xc0)
                        {
                            aa += 0x40;
                        }
                        currentState.lastY = aa;
                    }
                    currentState.nextY = nY;
                    currentState.nextX = nX;
                    if (currentState.nextY>=0xc0)
                    {// add 0x40?
                        throw new Exception($"Unhandled command {code:X} at {currentState.address - 1:X}");
                    }

                    // Move Y,X
                    if (currentState.drawLine)
                    {
                        DrawLine(currentState.lastX, currentState.lastY, currentState.nextX, currentState.nextY);
                        lineCounter++;
                        if (lineCounter==maxLine)
                        {
                            return false;
                        }
                    }
                    if (currentState.flagBit2)
                    {
                        currentState.lastX = currentState.nextX;
                        currentState.lastY = currentState.nextY;
                    }
                }
                else
                {
                    if (code >= 0xE6)   // FILL
                    {
                        var patternIndex = code - 0xE6;
                        ushort baseAddress = 0xE0A4;
                        currentState.patternAddress = (ushort)(baseAddress + (patternIndex * 32));
                        if (!fillScreen.GetBit(currentState.nextX, currentState.nextY))
                        {
                            // Fill should occur
                            FillPattern(currentState);
                            fillCounter++;
                            if (fillCounter == maxFill)
                            {
                                return false;
                            }
                        }
                    }
                    else
                    {
                        switch (code)
                        {
                            case 0xC0:  // SET_POS
                                // Set current X,Y - TODO modified by flags
                                if (currentState.flagBit4)
                                {
                                    var a= Fairlight.GetByte(rom,currentState.address++);
                                    if (a + currentState.lastY > 0xFF)
                                    {
                                        a += currentState.lastY;
                                        a += 0x40;
                                    }
                                    else
                                    {
                                        a += currentState.lastY;
                                        if (a >= 0xC0)
                                        {
                                            a += 0x40;
                                        }
                                    }
                                    currentState.lastY = a;
                                    a=Fairlight.GetByte(rom,currentState.address++);
                                    if (currentState.flagBit6)
                                    {
                                        a = (byte)(0 - a);
                                    }
                                    a+=currentState.lastX;
                                    currentState.lastX = a;
                                }
                                else
                                {
                                    currentState.lastY = Fairlight.GetByte(rom,currentState.address++);
                                    var a = Fairlight.GetByte(rom, currentState.address++);
                                    if (currentState.flagBit6)
                                    {
                                        a ^= 0xFF;
                                    }
                                    currentState.lastX = a;
                                }
                                break;
                            case 0xC1:  // SET_FLAG_4
                                currentState.flagBit4 = true;
                                break;
                            case 0xC2:  // RESET_FLAG_4
                                currentState.flagBit4 = false;
                                break;
                            case 0xC3:  // SET_FLAG_2
                                currentState.flagBit2 = true;
                                break;
                            case 0xC4:  // RESET_FLAG_2
                                currentState.flagBit2 = false;
                                break;
                            case 0xC5:  // SET_FLAG_3
                                currentState.flagBit3 = true;
                                break;
                            case 0xC6:  // RESET_FLAG_3
                                currentState.flagBit3 = false;
                                break;
                            case 0xC7:  // SET_FLAG_5
                                currentState.drawLine = true;
                                break;
                            case 0xC8:  // RESET_FLAG_5
                                currentState.drawLine = false;
                                break;
                            case 0xC9:  // SET_FLAG_6
                                currentState.flagBit6 = true;
                                break;
                            case 0xCA:  // RESET_FLAG_6
                                currentState.flagBit6 = false;
                                break;
                            case 0xCB:  // SET_FLAG_0
                                currentState.flagBit0 = true;
                                break;
                            case 0xCC:  // RESET_FLAG_0
                                currentState.flagBit0 = false;
                                break;
                            case 0xCD:  // SET_FLAG_1
                                currentState.flagBit1 = true;
                                break;
                            case 0xCE:  // RESET_FLAG_1
                                currentState.flagBit1 = false;
                                break;
                            case 0xCF:  // UPDATE_LAST
                                currentState.lastX = currentState.nextX;
                                currentState.lastY = currentState.nextY;
                                break;
                            case 0xD5:  // SET_LOOP
                                currentState.loopCount = Fairlight.GetByte(rom, currentState.address++);
                                currentState.loopPoint = currentState.address;
                                break;
                            case 0xD6:  // DEC_LOOP
                                currentState.loopCount--;
                                if (currentState.loopCount != 0)
                                {
                                    currentState.address = currentState.loopPoint;
                                }
                                break;
                            case 0xE0:  // GOSUB
                                index = Fairlight.GetByte(rom, currentState.address++);
                                stateStack.Push(currentState);
                                currentState = new State(Fairlight.FetchTableAddress(rom, 0x7593, index - 1), currentState);  // 0x7593 might be dynamic, its loaded from 0xFFB5 - TODO trace
                                if (!ProcessCommands(currentState))
                                    return false;
                                currentState.flagBit7 = false;
                                currentState = new State(stateStack.Pop().address, currentState);
                                break;
                            case 0xE1:  // GOSUB_SAVE
                                // Push state and process new command

                                index = Fairlight.GetByte(rom, currentState.address++);
                                stateStack.Push(currentState);

                                currentState=new State(Fairlight.FetchTableAddress(rom, 0x7593, index-1), currentState);  // 0x7593 might be dynamic, its loaded from 0xFFB5 - TODO trace
                                if (!ProcessCommands(currentState))
                                    return false;
                                currentState.flagBit7 = false;
                                currentState = stateStack.Pop();
                                break;
                            case 0xE2:  // COPY_TO_TEMP_SCREEN
                                fillScreen.CopyBitmapFrom(screen);
                                break;
                            case 0xE4:  // EXTENDED_COMMAND
                                var eCode=Fairlight.GetByte(rom, currentState.address++);
                                switch (eCode)
                                {
                                    case 0x04:
                                        currentState.flagBit7 = true;
                                        if (!iy37bit0)
                                        {
                                            iy37bit0 = true;
                                            // unknown
                                        }
                                        break;
                                    default:
                                        return false;
                                        throw new Exception($"Unhandled command {code:X} {eCode:X} at {currentState.address - 1:X}");
                                }
                                break;
                            default:
                                return false;
                                throw new Exception($"Unknown command {code:X} at {currentState.address - 1:X}");
                        }
                    }
                }
            }
        }

    }

    // Pattern is 8x32, 1 bit per pixel
    // organised as : 
    // AB
    // CD
    // ABCD
    private bool GetPatternBit(ReadOnlySpan<byte> pattern,int x,int y)
    {
        x &= 15;
        y &= 15;
        y = 15 - y;
        var quadrant = (y / 8) * 2 + (x / 8);
        var offset = quadrant * 8;
        var byteOffset = offset + (y & 7);
        var bitOffset = x & 7;
        return (pattern[byteOffset] & (1 << (7-bitOffset))) != 0;
    }

    // assume for now, state is not affected
    private void FillPattern(State currentState)
    {
        // Basic flood fill, assume pattern is aligned to screen co-ordinates for now
        var pattern = Fairlight.GetBytes(rom, currentState.patternAddress, 32);

        var x = currentState.nextX;
        var y = currentState.nextY;

        FloodFill(x, y, pattern);

    }

    private void FloodFill(uint x, uint y, ReadOnlySpan<byte> pattern)
    {
        Queue<(uint x, uint y)> queue = new Queue<(uint x, uint y)>();
        queue.Enqueue((x, y));

        while (queue.Count!=0)
        {
            var (x1, y1) = queue.Dequeue();
            if (x1 >= 256 || y1 >= 192)
                continue;
            if (fillScreen.GetBit(x1, y1))
                continue;

            fillScreen.DrawBitNoAttribute(x1, y1, true);
            screen.DrawBitNoAttribute(x1, y1, GetPatternBit(pattern, (int)x1, (int)y1));

            queue.Enqueue((x1 + 1, y1));
            queue.Enqueue((x1 - 1, y1));
            queue.Enqueue((x1, y1 + 1));
            queue.Enqueue((x1, y1 - 1));
        }
    }

    // Standard Bresnham, may not match original - TODO check
    private void DrawLine(byte lastX, byte lastY, byte nextX, byte nextY)
    {
        int dX=nextX-lastX;
        int dY=nextY-lastY;
        if (Math.Abs(dY) < Math.Abs(dX))
        {
            if (lastX > nextX)
            {
                DrawLineLow(nextX, nextY, lastX, lastY);
            }
            else
            {
                DrawLineLow(lastX, lastY, nextX, nextY);
            }
        }
        else
        {
            if (lastY > nextY)
            {
                DrawLineHigh(nextX, nextY, lastX, lastY);
            }
            else
            {
                DrawLineHigh(lastX, lastY, nextX, nextY);
            }
        }
    }

    private void DrawLineLow(int x0, int y0, int x1, int y1)
    {
        var dx = x1 - x0;
        var dy = y1 - y0;
        var yi = 1;
        if (dy < 0)
        {
            yi = -1;
            dy = -dy;
        }
        int D = 2 * dy - dx;
        int y = y0;
        for (int x = x0; x <= x1; x++)
        {
            screen.DrawBitNoAttribute((uint)x, (uint)y, true);
            if (D > 0)
            {
                y = y + yi;
                D = D - 2 * dx;
            }
            D = D + 2 * dy;
        }
    }

    private void DrawLineHigh(int x0, int y0, int x1, int y1)
    {
        var dx = x1 - x0;
        var dy = y1 - y0;
        var xi = 1;
        if (dx < 0)
        {
            xi = -1;
            dx = -dx;
        }
        int D = 2 * dx - dy;
        int x = x0;
        for (int y = y0; y <= y1; y++)
        {
            screen.DrawBitNoAttribute((uint)x, (uint)y, true);
            if (D > 0)
            {
                x = x + xi;
                D = D - 2 * dy;
            }
            D = D + 2 * dx;
        }
    }

    public float UpdateInterval => 1 / 30.0f;

    public void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls)
    {
        widget.AddImageView(this);
    }

    public void OnClose()
    {
    }
}