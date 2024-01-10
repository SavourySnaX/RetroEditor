
public class Fairlight : IRetroPlugin, IImages
{
    // This is MD5 of a ram dump of the game, will update with tap/tzx version later 
    private byte[] FairlightInMem = new byte[] { 116, 169, 129, 5, 73, 97, 39, 227, 202, 65, 167, 100, 76, 177, 243, 185 };

    public string Name => "Fairlight";
    IRomPlugin rom;


    public Fairlight()
    {
        rom = new NullRomPlugin();
    }

    public bool CanHandle(byte[] md5, byte[] bytes, string filename)
    {
        // One issue with this approach, is we can't generically load hacks of the game..
        return FairlightInMem.SequenceEqual(md5);
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
        return 64;
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
        var tableStart = FetchTableAddress(0x68B0, mapIndex);
        return new FairlightImage(this, mapIndex, tableStart);
    }

    public ushort FetchTableAddress(ushort baseAddress,int mapIndex)
    {
        var tableStart = baseAddress;
        ushort skip = 0;
        for (int a=0;a<=mapIndex;a++)
        {
            tableStart += skip;
            skip = rom.ReadWord((uint)tableStart);
        }

        tableStart += 2;

        return tableStart;
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

public class FairlightImage : IImage
{
    Fairlight main;
    string mapName;
    int mapIndex;
    ushort mapAddress;
    int frameCounter;

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

    public FairlightImage(Fairlight main, int mapIndex, ushort mapAddress)
    {
        this.main = main;
        this.mapAddress = mapAddress;
        this.mapIndex = mapIndex;
        this.mapName = GetMapName();
        screen = new ZXSpectrum48ImageHelper(Width, Height);
        fillScreen = new ZXSpectrum48ImageHelper(Width, Height);
    }

    public string GetMapName()
    {
        return $"Room {mapIndex}";
    }

    public uint Width => 256;

    public uint Height => 192;

    public string Name => mapName;

    public Pixel[] GetImageData(float seconds)
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
        frameCounter = (int)(seconds * 25);

        stateStack.Clear();

        var currentState = new State(mapAddress);

        screen.Clear(0x38);
        fillScreen.Clear(0);

        lineCounter = 0;
        maxLine = 99999;
        fillCounter = 0;
        maxFill = 999;
        iy37bit0 = false;

        var ff8e = main.GetByte(currentState.address++);
        ProcessCommands(currentState);

        screen.FlipVertical();

        return screen.Render(seconds);
    }

    private bool ProcessCommands(State currentState)
    {
        byte index = default;

        while (true)
        {
            byte code = main.GetByte(currentState.address++);

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
                    var a = main.GetByte(currentState.address++);
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
                                    var a= main.GetByte(currentState.address++);
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
                                    a=main.GetByte(currentState.address++);
                                    if (currentState.flagBit6)
                                    {
                                        a = (byte)(0 - a);
                                    }
                                    a+=currentState.lastX;
                                    currentState.lastX = a;
                                }
                                else
                                {
                                    currentState.lastY = main.GetByte(currentState.address++);
                                    var a= main.GetByte(currentState.address++);
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
                                currentState.loopCount = main.GetByte(currentState.address++);
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
                                index = main.GetByte(currentState.address++);
                                stateStack.Push(currentState);
                                currentState=new State(main.FetchTableAddress(0x7593, index-1), currentState);  // 0x7593 might be dynamic, its loaded from 0xFFB5 - TODO trace
                                if (!ProcessCommands(currentState))
                                    return false;
                                currentState.flagBit7 = false;
                                currentState = new State(stateStack.Pop().address, currentState);
                                break;
                            case 0xE1:  // GOSUB_SAVE
                                // Push state and process new command

                                index = main.GetByte(currentState.address++);
                                stateStack.Push(currentState);

                                currentState=new State(main.FetchTableAddress(0x7593, index-1), currentState);  // 0x7593 might be dynamic, its loaded from 0xFFB5 - TODO trace
                                if (!ProcessCommands(currentState))
                                    return false;
                                currentState.flagBit7 = false;
                                currentState = stateStack.Pop();
                                break;
                            case 0xE2:  // COPY_TO_TEMP_SCREEN
                                fillScreen.CopyBitmapFrom(screen);
                                break;
                            case 0xE4:  // EXTENDED_COMMAND
                                var eCode=main.GetByte(currentState.address++);
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
    private bool GetPatternBit(byte[] pattern,int x,int y)
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
        var pattern = main.GetBytes(currentState.patternAddress, 32);

        var x = currentState.nextX;
        var y = currentState.nextY;

        FloodFill(x, y, pattern);

    }

    private void FloodFill(uint x,uint y,byte[] pattern)
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
}
