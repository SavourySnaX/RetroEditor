using System.Text;

internal enum Regions
{
    Unknown,
    Code,
    Data
}

internal struct RegionInfo : IRange
{
    public Regions Region;

    public RegionInfo(UInt64 start, UInt64 end, Regions region)
    {
        Start = start;
        End = end;
        Region = region;
    }
    public UInt64 LineCount => 1;//End - Start + 1;

    public ulong Start { get; private set; }
    public ulong End { get; private set; }

    public IRange CreateRange(ulong start, ulong end)
    {
        return new RegionInfo(start,end,this.Region);
    }

    public bool IsSame(IRange other)
    {
        return Region == ((RegionInfo)other).Region;
    }
}

internal class RomDataParser 
{
    private const int BYTES_PER_LINE = 16;
    RangeCollection<RegionInfo> romRanges = new RangeCollection<RegionInfo>();
    private byte[] romData = new byte[0];
    private int romIndex = 0;
    private UInt64 minAddress, maxAddress;

    private LibMameDebugger.DView OpenDView(LibMameDebugger debugger)
    {
        var view = new LibMameDebugger.DView(debugger.AllocView(LibRetroPlugin.debug_view_type.Memory), 0, 0, 256, 256, "");
        
        // Find ROM source index
        int sourceCount = debugger.GetSourcesCount(ref view);
        var sources = debugger.GetSourcesList(ref view);
        
        for (int i = 0; i < sourceCount; i++)
        {
            if (sources[i].Contains("Region ':snsslot:cart:rom'"))
            {
                debugger.SetSource(ref view, i);
                break;
            }
        }

        return view;
    }

    private void CloseDView(LibMameDebugger debugger, LibMameDebugger.DView view)
    {
        debugger.FreeView(view.view);
    }

    public void AddCodeRange(UInt64 start, UInt64 end)
    {
        romRanges.AddRange(new RegionInfo(start,end,Regions.Code));
    }

    public void AddDataRange(UInt64 start, UInt64 end)
    {
        romRanges.AddRange(new RegionInfo(start,end,Regions.Data));
    }

    public RangeCollection<RegionInfo> GetRomRanges => romRanges;

    public UInt64 GetMinAddress => minAddress;
    public UInt64 GetMaxAddress => maxAddress;

    public void Parse(LibMameDebugger debugger)
    {
        // Initialize ROM view
        var view = OpenDView(debugger);

        try
        {
            // We don't know the size of the rom, so, first off set the address of the view to the biggest possible value
            view.view.Expression = $"${UInt64.MaxValue:X}";
            debugger.SetExpression(ref view);
            debugger.UpdateDView(ref view);

            // Now we can get the size of the rom
            var romSize = FindLastAddress(view);
            romData = new byte[romSize];

            minAddress = 0;
            maxAddress = romSize - 1;
            romRanges.AddRange(new RegionInfo(minAddress, maxAddress, Regions.Unknown));

            // Now we know the size of the rom, so, set the address of the view to the start of the rom
            UInt64 offset = 0;
            while (offset < romSize)
            {
                // Set the view expression to the current offset
                view.view.Expression = $"${offset:X}";
                debugger.SetExpression(ref view);

                // Update the view to get the data
                debugger.UpdateDView(ref view);

                // Parse the data from the view state
                offset = ParseChunk(view, offset);
            }
        }
        finally
        {
            CloseDView(debugger, view);
        }
    }

    private UInt64 ParseChunk(LibMameDebugger.DView view, UInt64 firstOffset)
    {
        romIndex = (int)firstOffset;
        UInt64 expectedOffset = firstOffset;
        int bytesPerLine = view.view.W * 2; // Each character is 2 bytes (char + attribute)

        for (int y = 0; y < view.view.H; y++)
        {
            int lineStart = y * bytesPerLine;
            int x = 0;
            
            // Skip initial spaces
            while (x < view.view.W && (char)view.state[lineStart + x * 2] == ' ')
                x++;

            // Parse address
            StringBuilder addressStr = new StringBuilder();
            while (x < view.view.W && (char)view.state[lineStart + x * 2] != ' ')
            {
                addressStr.Append((char)view.state[lineStart + x * 2]);
                x++;
            }
            var address = UInt64.Parse(addressStr.ToString(), System.Globalization.NumberStyles.HexNumber);
            if (address != expectedOffset)
            {
                throw new Exception($"Address mismatch at offset {expectedOffset}");
            }
            expectedOffset += BYTES_PER_LINE;

            // Skip spaces between address and bytes
            while (x < view.view.W && (char)view.state[lineStart + x * 2] == ' ')
                x++;

            // Parse bytes
            for (int byteCount = 0; byteCount < BYTES_PER_LINE && x < view.view.W - 1; byteCount++)
            {
                // Get the two hex chars
                char highNibble = (char)view.state[lineStart + x * 2];
                char lowNibble = (char)view.state[lineStart + (x + 1) * 2];

                // Convert hex chars to byte
                if (char.IsLetterOrDigit(highNibble) && char.IsLetterOrDigit(lowNibble))
                {
                    byte value = Convert.ToByte($"{highNibble}{lowNibble}", 16);
                    romData[romIndex++] = value;
                }
                else
                {
                    throw new Exception($"Invalid byte at offset {expectedOffset}");
                }

                x += 3; // Skip the two chars and the space
            }
        }

        return expectedOffset;
    }

    private UInt64 FindLastAddress(LibMameDebugger.DView view)
    {
        UInt64 lastAddress = 0;
        int bytesPerLine = view.view.W * 2; // Each character is 2 bytes (char + attribute)
        int lastValidRow = -1;

        // Find the last valid row
        for (int y = 0; y < view.view.H; y++)
        {
            int lineStart = y * bytesPerLine;
            int x = 0;
            
            // Skip initial spaces
            while (x < view.view.W && (char)view.state[lineStart + x * 2] == ' ')
                x++;

            // Check if we have a valid address
            if (x < view.view.W && char.IsLetterOrDigit((char)view.state[lineStart + x * 2]))
            {
                StringBuilder addressStr = new StringBuilder();
                while (x < view.view.W && (char)view.state[lineStart + x * 2] != ' ')
                {
                    addressStr.Append((char)view.state[lineStart + x * 2]);
                    x++;
                }
                if (addressStr.Length > 0)
                {
                    lastAddress = UInt64.Parse(addressStr.ToString(), System.Globalization.NumberStyles.HexNumber);
                    lastValidRow = y;
                }
            }
        }

        if (lastValidRow >= 0)
        {
            // Count valid bytes in the last row
            int lineStart = lastValidRow * bytesPerLine;
            int x = 0;
            
            // Skip to bytes section
            while (x < view.view.W && (char)view.state[lineStart + x * 2] != ' ')
                x++;
            while (x < view.view.W && (char)view.state[lineStart + x * 2] == ' ')
                x++;

            // Count valid bytes
            int validBytes = 0;
            for (int byteCount = 0; byteCount < BYTES_PER_LINE && x < view.view.W - 1; byteCount++)
            {
                char highNibble = (char)view.state[lineStart + x * 2];
                char lowNibble = (char)view.state[lineStart + (x + 1) * 2];

                if (char.IsLetterOrDigit(highNibble) && char.IsLetterOrDigit(lowNibble))
                {
                    validBytes++;
                }
                else
                    break;
                x += 3; // Skip the two chars and the space
            }

            // Adjust the last address by the number of valid bytes
            if (validBytes > 0)
            {
                lastAddress += (UInt64)validBytes-1;        // -1 because the address is inclusive
            }
        }

        return lastAddress;
    }

    public byte[] GetRomData => romData;

    public byte GetByte(UInt64 address)
    {
        if (address >= (UInt64)romData.Length)
            return 0;
        return romData[address];
    }
} 