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

    public string BytesForSpan(ReadOnlySpan<byte> bytes)
    {
        StringBuilder sb = new StringBuilder();

        foreach(var db in bytes)
        {
            sb.Append($"{db:X2} ");
        }
        return sb.ToString();
    }

    public string BytesForLine(UInt64 start,UInt64 end)
    {
        return BytesForSpan(Parent.FetchBytes(start,end-start+1));
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

internal class CodeRegion : IRegionInfo
{
    protected SortedDictionary<ulong, Instruction> instructions = new ();
    public CodeRegion(UInt64 start, UInt64 end, Instruction instruction, RomDataParser parent) : base(start, end, parent, parent.CodeColor)
    {
        instructions.Add(start, instruction);
    }
    
    public override ulong GetLineCount() => (UInt64)instructions.Count;

    public override void Combining(IRegionInfo other) 
    { 
        if (other is CodeRegion cregion)
        {
            foreach (var i in cregion.instructions)
            {
                if (!instructions.ContainsKey(i.Key))
                    instructions.Add(i.Key, i.Value);
            }
        }
        else
        {
            throw new ArgumentException("Cannot combine different regions");
        }
    }
    public override IRegionInfo Split(ulong start, ulong end)
    {
        // remove instructions in the range start-end and add them to the new region
        var newRegion = new CodeRegion(start, end, instructions[start], Parent);
        instructions.Remove(start);
        List<ulong> keysToRemove = new List<ulong>();
        foreach (var i in instructions)
        {
            if (i.Key >= start && i.Key <= end)
            {
                newRegion.instructions.Add(i.Key, i.Value);
                keysToRemove.Add(i.Key);
            }
        }
        foreach (var key in keysToRemove)
        {
            instructions.Remove(key);
        }
        return newRegion;
    }

    public override LineInfo GetLineInfo(ulong index)
    {
        var I = instructions.ElementAt((int)index);
        return new LineInfo($"{I.Key:X8}", BytesForSpan(I.Value.Bytes), I.Value.InstructionText(), $"; {I.Value.cpuState}");
    }

    public Instruction GetInstructionForLine(ulong index)
    {
        return instructions.ElementAt((int)index).Value;
    }

    public override ulong LineOffsetForAddress(UInt64 address)
    {
        UInt64 offset=0;
        foreach (var i in instructions)
        {
            if (i.Key >= address)
                return offset;
            offset++;
        }
        return (UInt64)(instructions.Count-1);
    }

    public override ulong AddressForLine(UInt64 line)
    {
        return instructions.ElementAt((int)line).Key;
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

    public enum SNESLoRomRegion
    {
        ROM,
        IO,
        SRAM,
        RAM,
    }

    public UInt64 MapRomToCpu(UInt64 linearAddress)
    {
        UInt64 bank = (linearAddress >> 15) & 0x7F;
        UInt64 offset = linearAddress & 0x7FFF;

        if (bank < 0x40)
        {
            return (bank << 16) | 0x8000 | offset;
        }
        else if (bank < 0x70)
        {
            return (bank << 15) | offset;
        }
        else
        {
            return ((bank - 0x70) << 15) | offset;
        }
    }

    public UInt64 MapSnesCpuToLorom(UInt64 address, out SNESLoRomRegion region)
    {
        region = SNESLoRomRegion.ROM;
        var bank = address >> 16;
        var offset = address & 0xFFFF;

        if (bank==0x7E || bank==0x7F)
        {
            region = SNESLoRomRegion.RAM;
            return ((bank - 0x7E) << 16) | offset;
        }
        else if (bank==0xFE || bank==0xFF)
        {
            if (offset<0x8000)
            {
                // SRAM
                region = SNESLoRomRegion.SRAM;
                return ((bank - 0xF0) << 15) | offset;
            }
            else
            {
                // ROM
                return 0x3F0000 | ((bank-0xFE)<<15) | (offset&0x7FFF);           
            }
        }
        bank&=0x7F;
        if (bank<0x40)
        {
            if (offset<0x2000)
            {
                // Low RAM
                region = SNESLoRomRegion.RAM;
                return offset;
            }
            else if (offset<0x8000)
            {
                // IO
                region = SNESLoRomRegion.IO;
                return offset-0x2000;
            }
            else
            {
                // ROM
                return (bank << 15) | (offset & 0x7FFF);
            }
        }
        else if (bank<0x70)
        {
            // ROM
            return (bank << 15) | (offset & 0x7FFF);
        }
        else
        {
            // 70-7D
            if (offset<0x8000)
            {
                region = SNESLoRomRegion.SRAM;
                return ((bank - 0x70) << 15) | offset;
            }
            else
            {
                //ROM
                return (bank<<15) | (offset&0x7FFF);
            }
        }
    }

    public void AddCodeRange(DisassemblerBase disassembler, UInt64 minAddress, UInt64 maxAddress)
    {
        while (minAddress<=maxAddress)
        {
            // minAddress is linear, need to compute approx PC
            var pc = MapRomToCpu(minAddress);
            if (AddCodeRange(disassembler, pc, out var i))
            {
                pc+= (UInt64)i.Bytes.Length;
                if (i.IsBasicBlockTerminator)
                {
                    return;
                }
                if (i.NextAddresses.Contains(pc))
                {
                    minAddress+= (UInt64)i.Bytes.Length;
                }
            }
            else
            {
                return;
            }
        }
    }

    public bool AddCodeRange(DisassemblerBase disassembler, UInt64 pc, out Instruction instruction)
    {
        bool done = false;
        UInt64 length = 0;
        UInt64 address = MapSnesCpuToLorom(pc, out var region);
        if (region!=RomDataParser.SNESLoRomRegion.ROM)
        {
            // Not a valid LoROM address
            Console.WriteLine($"Invalid LoROM address: {address:X8} in region {region} {pc:X6}");
            instruction = new();
            return false;
        }

        while (!done)
        {
            var result = disassembler.DecodeNext(FetchBytes(address, length), pc);
            if (!result.Success)
            {
                if (result.NeedsMoreBytes)
                {
                    length += (UInt64)result.AdditionalBytesNeeded;
                    continue;
                }
                else
                {
                    Console.WriteLine($"Error: {result.ErrorMessage}");
                    instruction = new();
                    return false;
                }
            }
            var current = romRanges.GetRangeContainingAddress(address, out var lineOff);
            if (current.Value != null)
            {
                if (current.Value is CodeRegion cregion)
                {
                    // Check we overlap the instruction
                    var check = cregion.GetInstructionForLine(lineOff);
                    if (check.ToString()!=result.Instruction.ToString())        // TODO de-shitiffy
                    {
                        Console.WriteLine($"Error: instruction overlaps different instruction {check} != {result.Instruction}");
                        instruction = new();
                        return false;
                    }
                }
                else
                {
                    if (((UInt64)result.Instruction.Bytes.Length) > current.Value.AddressEnd - current.Value.AddressStart + 1)
                    {
                        Console.WriteLine($"Error: instruction does not fit! {current.Value.AddressStart:X8} != {result.Instruction.ToString()}");
                        instruction = new();
                        return false;
                    }
                    romRanges.AddRange(new CodeRegion(address, address + (UInt64)result.BytesConsumed - 1, result.Instruction, this));
                }
            }
            instruction = result.Instruction;
            return true;
        }
        instruction = new();
        return false;
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