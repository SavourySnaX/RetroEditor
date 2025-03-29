using System.Linq.Expressions;
using System.Numerics;
using System.Text;
using ImGuiNET;

internal enum Regions
{
    Unknown,
    Code,
    Data
}

internal struct LineInfo
{
    public string Address;
    public string Bytes;
    public string Details;
    public string Comment;

    public LineInfo(string address, string bytes, string details, string comment)
    {
        Address = address;
        Bytes = bytes;
        Details = details;
        Comment = comment;
    }
}

internal abstract class IRegionInfo : IRange
{
    public IRegionInfo(UInt64 start,UInt64 end,RomDataParser parent,uint color)
    {
        AddressStart = start;
        AddressEnd = end;
        Parent = parent;
        Colour = color;
    }

    public UInt64 AddressStart { get; protected set; }
    public UInt64 AddressEnd { get; protected set; }

    public UInt64 LineCount => GetLineCount();
    protected RomDataParser Parent;

    public bool IsSame(IRange other)
    {
        return GetType() == other.GetType();
    }

    public void CombineAdjacent(IRange other)
    {
        if (!IsSame(other))
            throw new ArgumentException("Cannot combine different regions");
        if (other.AddressStart != AddressEnd + 1)
            throw new ArgumentException("Cannot combine non-adjacent ranges");
        AddressEnd = other.AddressEnd;
        Combining((IRegionInfo)other);
    }

    public IRange SplitAfter(ulong position)
    {
        if (position < AddressStart || position >= AddressEnd)
            throw new ArgumentOutOfRangeException("Position is out of range");
        if (position == AddressEnd)
            throw new ArgumentOutOfRangeException("Position is out of range");
        var oldEnd = AddressEnd;
        AddressEnd = position;
        return Split(position+1, oldEnd);
    }

    public IRange SplitBefore(ulong position)
    {
        if (position <= AddressStart || position > AddressEnd)
            throw new ArgumentOutOfRangeException("Position is out of range");
        if (position == AddressStart)
            throw new ArgumentOutOfRangeException("Position is out of range");
        var oldStart = AddressStart;
        AddressStart = position;
        return Split(oldStart, position - 1);
    }

    public string BytesForLine(UInt64 start,UInt64 end)
    {
        StringBuilder sb = new StringBuilder();
        for (UInt64 j = start; j <= end; j++)
        {
            var db = Parent.GetByte(j);
            sb.Append($"{db:X2} ");
        }
        return sb.ToString();
    }

    public uint Colour { get; private set; }

    public abstract IRegionInfo Split(UInt64 start, UInt64 end);
    public abstract void Combining(IRegionInfo other);
    public abstract UInt64 GetLineCount();
    public abstract LineInfo GetLineInfo(UInt64 index);
    public abstract UInt64 LineOffsetForAddress(UInt64 address);
    public abstract UInt64 AddressForLine(UInt64 line);

}

internal class UnknownRegion : IRegionInfo
{
    public UnknownRegion(UInt64 start, UInt64 end, RomDataParser parent) : base(start, end, parent, parent.UnknownColor)
    {
    }
    
    public override ulong GetLineCount() => AddressEnd - AddressStart + 1;

    public override void Combining(IRegionInfo other) { }
    public override IRegionInfo Split(ulong start, ulong end) => new UnknownRegion(start, end, Parent);

    public override LineInfo GetLineInfo(ulong index)
    {
        var db = Parent.GetByte(AddressStart + index);
        return new LineInfo($"{AddressStart + index:X8}", db.ToString("X2"), $"{(Char.IsControl((char)db) ? '.' : (char)db)}", "");
    }

    public override ulong LineOffsetForAddress(UInt64 address)
    {
        return address - AddressStart;
    }

    public override ulong AddressForLine(UInt64 line)
    {
        return AddressStart + line;
    }
}

internal class StringRegion : IRegionInfo
{
    public StringRegion(UInt64 start, UInt64 end, RomDataParser parent) : base(start, end, parent, parent.StringColor)
    {
    }
    
    public override ulong GetLineCount() => (AddressEnd - AddressStart + 16)/16;

    public override void Combining(IRegionInfo other) { }
    public override IRegionInfo Split(ulong start, ulong end) => new UnknownRegion(start, end, Parent);

    public override LineInfo GetLineInfo(ulong index)
    {
        StringBuilder s = new StringBuilder();
        if (index==0)
        {
            s.Append($"db \"");
            for (UInt64 j = AddressStart; j <= AddressEnd; j++)
            {
                var db = Parent.GetByte(j);
                if (Char.IsControl((char)db) || db > 0x7F)
                {
                    s.Append($"\",${db:X2},\"");
                }
                else
                    s.Append((char)db);
            }
            s.Append('"');
        }
        var I = AddressStart+(index * 16);
        return new LineInfo(index == 0 ? $"{I:X8}" : "", BytesForLine(I,Math.Min(AddressEnd,I+16)), s.ToString(), "");
    }

    public override ulong LineOffsetForAddress(UInt64 address)
    {
        return (address - AddressStart) / 16;
    }

    public override ulong AddressForLine(UInt64 line)
    {
        return AddressStart + (line * 16);
    }
}


internal class RomDataParser 
{
    private const int BYTES_PER_LINE = 16;
    RangeCollection<IRegionInfo> romRanges = new RangeCollection<IRegionInfo>();
    private byte[] romData = new byte[0];
    private int romIndex = 0;
    private UInt64 minAddress, maxAddress;

    public uint UnknownColor;
    public uint CodeColor;
    public uint DataColor;
    public uint StringColor;

    public RomDataParser()
    {
        UnknownColor = ImGui.GetColorU32(new Vector4(0, 0, 0, .5f));
        CodeColor = ImGui.GetColorU32(new Vector4(0, 0, 1, .5f));
        DataColor = ImGui.GetColorU32(new Vector4(0, 1, 0, .5f));
        StringColor = ImGui.GetColorU32(new Vector4(1, 0, 0, .5f));
    }

    private LibMameDebugger.DView OpenMemView(LibMameDebugger debugger)
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

    private LibMameDebugger.DView OpenCPUView(LibMameDebugger debugger, string cpuName)
    {
        var view = new LibMameDebugger.DView(debugger.AllocView(LibRetroPlugin.debug_view_type.State), 0, 0, 32, 32, "");
        
        // Find ROM source index
        int sourceCount = debugger.GetSourcesCount(ref view);
        var sources = debugger.GetSourcesList(ref view);
        
        for (int i = 0; i < sourceCount; i++)
        {
            if (sources[i].Contains(cpuName))
            {
                debugger.SetSource(ref view, i);
                break;
            }
        }

        return view;
    }


    private void CloseView(LibMameDebugger debugger, LibMameDebugger.DView view)
    {
        debugger.FreeView(view.view);
    }

    public void AddCodeRange(UInt64 start, UInt64 end)
    {
       // romRanges.AddRange(new RegionInfo(start,end,Regions.Code, this));
    }

    public void AddDataRange(UInt64 start, UInt64 end)
    {
       // romRanges.AddRange(new RegionInfo(start,end,Regions.Data, this));
    }

    public void AddStringRange(UInt64 start, UInt64 end)
    {
        romRanges.AddRange(new StringRegion(start, end, this));
    }
    
    public void AddUnknownRange(UInt64 start, UInt64 end)
    {
        romRanges.AddRange(new UnknownRegion(start, end, this));
    }


    public RangeCollection<IRegionInfo> GetRomRanges => romRanges;

    public UInt64 GetMinAddress => minAddress;
    public UInt64 GetMaxAddress => maxAddress;

    public void Parse(LibMameDebugger debugger)
    {
        // Initialize ROM view
        var view = OpenMemView(debugger);

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
            romRanges.AddRange(new UnknownRegion(minAddress, maxAddress, this));

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
            CloseView(debugger, view);
        }
    }

    public UInt64 GetCPUState(LibMameDebugger debugger, string register)
    {
        var view = OpenCPUView(debugger, "main");
        try
        {
            // Update the view to get the data
            debugger.UpdateDView(ref view);

            return ParseState(view, register);
        }
        finally
        {
            CloseView(debugger, view);
        }
        
    }
    
    private UInt64 ParseState(LibMameDebugger.DView view, string register)
    {
        int bytesPerLine = view.view.W * 2; // Each character is 2 bytes (char + attribute)

        for (int y = 0; y < view.view.H; y++)
        {
            int lineStart = y * bytesPerLine;
            int x = 0;
            
            // Skip initial spaces
            while (x < view.view.W && (char)view.state[lineStart + x * 2] == ' ')
                x++;

            // Verify register name matches
            StringBuilder registerStr = new StringBuilder();
            while (x < view.view.W && (char)view.state[lineStart + x * 2] != ' ')
            {
                registerStr.Append((char)view.state[lineStart + x * 2]);
                x++;
            }
            if (registerStr.ToString() != register)
            {
                continue;
            }
            // Skip spaces between name and value
            while (x < view.view.W && (char)view.state[lineStart + x * 2] == ' ')
                x++;

            // Fetch Value
            registerStr.Clear();
            while (x < view.view.W && (char)view.state[lineStart + x * 2] != ' ')
            {
                registerStr.Append((char)view.state[lineStart + x * 2]);
                x++;
            }
            if (registerStr.Length > 0)
            {
                return UInt64.Parse(registerStr.ToString(), System.Globalization.NumberStyles.HexNumber);
            }
        }

        return 0;
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

    public ReadOnlySpan<byte> FetchBytes(UInt64 address, UInt64 length)
    {
        if (address >= (UInt64)romData.Length)
            return new byte[0];
        length = Math.Min(length, (UInt64)romData.Length - address);
        return new ReadOnlySpan<byte>(romData, (int)address, (int)length);
    }
} 