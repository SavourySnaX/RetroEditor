using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using RetroEditor.Source.Internals.ReverseEngineering.Platform;

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

internal interface IRomDataParser
{
    byte GetByte(UInt64 address);
    ReadOnlySpan<byte> FetchBytes(UInt64 address, UInt64 length);
    void AddSymbol(ulong value, int size, string symbol);
    
    ISymbolProvider SymbolProvider { get; }
}

internal abstract class IRegionInfo : IRange
{
    public IRegionInfo(UInt64 start, UInt64 end, IRomDataParser parent, ResourcerConfig.ConfigColour color)
    {
        AddressStart = start;
        AddressEnd = end;
        Parent = parent;
        Colour = color;
    }

    public UInt64 AddressStart { get; protected set; }
    public UInt64 AddressEnd { get; protected set; }

    public UInt64 LineCount => GetLineCount();
    protected IRomDataParser Parent { get; private set; }

    public virtual bool IsSame(IRange otherRange)
    {
        var other = otherRange as IRegionInfo;
        if (other == null)
            return false;
        return GetType() == other.GetType() && Above.Count == 0 && Below.Count == 0 && other.Above.Count == 0 && other.Below.Count == 0;
    }

    public void CombineAdjacent(IRange other)
    {
        var otherRange = other as IRegionInfo;
        if (otherRange == null)
            throw new ArgumentException("Cannot combine different regions");
        if (!IsSame(other))
            throw new ArgumentException("Cannot combine different regions");
        if (other.AddressStart != AddressEnd + 1)
            throw new ArgumentException("Cannot combine non-adjacent ranges");
        AddressEnd = other.AddressEnd;
        Combining((IRegionInfo)other);
        if (Above.Count > 0 || Below.Count > 0 || otherRange.Above.Count > 0 || otherRange.Below.Count > 0)
            throw new ArgumentException("Cannot combine non-adjacent ranges with children");
    }

    public IRange SplitAfter(ulong position)
    {
        if (position < AddressStart || position >= AddressEnd)
            throw new ArgumentOutOfRangeException("Position is out of range");
        if (position == AddressEnd)
            throw new ArgumentOutOfRangeException("Position is out of range");
        var oldEnd = AddressEnd;
        AddressEnd = position;
        var after = Split(position + 1, oldEnd);
        after.Below = this.Below;
        Below = new();
        return after;
    }

    public IRange SplitBefore(ulong position)
    {
        if (position <= AddressStart || position > AddressEnd)
            throw new ArgumentOutOfRangeException("Position is out of range");
        if (position == AddressStart)
            throw new ArgumentOutOfRangeException("Position is out of range");
        var oldStart = AddressStart;
        AddressStart = position;
        var before = Split(oldStart, position - 1);
        before.Above = this.Above;
        Above = new();
        return before;
    }

    public string BytesForSpan(ReadOnlySpan<byte> bytes)
    {
        StringBuilder sb = new StringBuilder();

        foreach (var db in bytes)
        {
            sb.Append($"{db:X2} ");
        }
        return sb.ToString();
    }

    public string BytesForLine(UInt64 start, UInt64 end)
    {
        return BytesForSpan(Parent.FetchBytes(start, end - start + 1));
    }

    public List<IRegionInfo> Above = new();
    public List<IRegionInfo> Below = new();

    public ResourcerConfig.ConfigColour Colour { get; private set; }

    public abstract IRegionInfo Split(UInt64 start, UInt64 end);
    public abstract void Combining(IRegionInfo other);
    public UInt64 GetLineCount()
    {
        UInt64 lineCount = 0;
        foreach (var r in Above)
        {
            lineCount += r.GetLineCount();
        }
        foreach (var r in Below)
        {
            lineCount += r.GetLineCount();
        }
        return lineCount + GetRegionLineCount();
    }

    public LineInfo GetLineInfo(UInt64 index)
    {
        UInt64 lineCount;
        foreach (var r in Above)
        {
            lineCount = r.GetLineCount();
            if (index < lineCount)
            {
                return r.GetLineInfo(index);
            }
            index -= lineCount;
        }
        lineCount = GetRegionLineCount();
        if (index < lineCount)
        {
            return GetRegionLineInfo(index);
        }
        index -= lineCount;
        foreach (var r in Below)
        {
            lineCount = r.GetLineCount();
            if (index < lineCount)
            {
                return r.GetLineInfo(index);
            }
            index -= lineCount;
        }
        throw new ArgumentOutOfRangeException("Index is out of range");
    }

    public UInt64 LineOffsetForAddress(UInt64 address)
    {
        UInt64 lineCount = 0;
        foreach (var r in Above)
        {
            lineCount += r.GetLineCount();
        }
        return lineCount + RegionLineOffsetForAddress(address);

    }
    public UInt64 AddressForLine(UInt64 line)
    {
        UInt64 lineCount;
        foreach (var r in Above)
        {
            lineCount = r.GetLineCount();
            if (line < lineCount)
            {
                return RegionAddressForLine(0);
            }
            line -= lineCount;
        }
        lineCount = GetRegionLineCount();
        if (line < lineCount)
        {
            return RegionAddressForLine(line);
        }
        return RegionAddressForLine(lineCount - 1);
    }

    public abstract UInt64 GetRegionLineCount();
    public abstract LineInfo GetRegionLineInfo(UInt64 index);
    public abstract UInt64 RegionLineOffsetForAddress(UInt64 address);
    public abstract UInt64 RegionAddressForLine(UInt64 line);

    public void Overwrite(IRange other)
    {
        // This node is being overwritten, make sure other has 
        var otherRegion = other as IRegionInfo;
        if (otherRegion == null)
        {
            throw new ArgumentException("Cannot replace different regions");
        }
        if (otherRegion.Above.Count != 0)
        {
            throw new ArgumentException("Cannot replace regions with children");
        }
        else
        {
            otherRegion.Above = Above;
            Above = new();
        }
        if (otherRegion.Below.Count != 0)
        {
            throw new ArgumentException("Cannot replace regions with children");
        }
        else
        {
            otherRegion.Below = Below;
            Below = new();
        }
    }

    public UInt64 ComputeTrueIndex(UInt64 index)
    {
        foreach (var r in Above)
        {
            index -= r.GetLineCount();
        }
        return index;
    }

    public Dictionary<string, object> Save()
    {
        var dict = new Dictionary<string, object>
        {
            { "AddressStart", AddressStart },
            { "AddressEnd", AddressEnd },
            { "Colour", Colour.ToString() },
            { "Above", Above.Select(r => r.Save()).ToList() },
            { "Below", Below.Select(r => r.Save()).ToList() },
            { "Type", GetType().Name }
        };
        RegionSave(dict);
        return dict;
    }

    public abstract void RegionSave(Dictionary<string, object> dict);

    public static IRegionInfo Load(Dictionary<string, object> dict, IRomDataParser parent)
    {
        var start = ((JsonElement)dict["AddressStart"]).GetUInt64();
        var end = ((JsonElement)dict["AddressEnd"]).GetUInt64();
        var element = (JsonElement)dict["Colour"];
        if (element.ValueKind == JsonValueKind.Null)
            throw new ArgumentException("Colour is null");
        var elementColour = element.GetString();
        if (elementColour == null)
            throw new ArgumentException("Colour is null");
        var colour = (ResourcerConfig.ConfigColour)Enum.Parse(typeof(ResourcerConfig.ConfigColour), elementColour);
        var typeName = ((JsonElement)dict["Type"]).GetString();
        if (typeName == null)
            throw new ArgumentException("Type is null");
        var type = Type.GetType(typeName);

        if (type == null)
            throw new ArgumentException($"Cannot find type {dict["Type"]}");

        var region = RuntimeHelpers.GetUninitializedObject(type) as IRegionInfo;
        if (region == null)
        {
            throw new ArgumentException($"Cannot create type {dict["Type"]}");
        }
        region = region.RegionLoad(dict);
        region.AddressEnd = end;
        region.AddressStart = start;
        region.Parent = parent;
        region.Colour = colour;

        // deserialize Above and below
        var aboveElem = (JsonElement)dict["Above"];
        region.Above = new List<IRegionInfo>();
        foreach (var item in aboveElem.EnumerateArray())
        {
            var itemDict = JsonSerializer.Deserialize<Dictionary<string, object>>(item.ToString());
            if (itemDict == null)
                throw new ArgumentException("Cannot deserialize region");
            var aboveRegion = Load(itemDict, parent);
            region.Above.Add(aboveRegion);
        }
        var belowElem = (JsonElement)dict["Below"];
        region.Below = new List<IRegionInfo>();
        foreach (var item in belowElem.EnumerateArray())
        {
            var itemDict = JsonSerializer.Deserialize<Dictionary<string, object>>(item.ToString());
            if (itemDict == null)
                throw new ArgumentException("Cannot deserialize region");
            var belowRegion = Load(itemDict, parent);
            region.Below.Add(belowRegion);
        }

        region.RegionLoad(dict);
        return region;
    }

    public abstract IRegionInfo RegionLoad(Dictionary<string, object> dict);
}

internal class MultiLineComment : IRegionInfo
{
    private string[] lines;
    public MultiLineComment(string[] lines, IRomDataParser parent) 
        : base(0, 0, parent, ResourcerConfig.ConfigColour.Comment)
    {
        this.lines = lines;
    }

    public override ulong GetRegionLineCount() => (ulong)lines.Length;

    public override void Combining(IRegionInfo other) => throw new NotImplementedException("Cannot combine a comment region");
    public override IRegionInfo Split(ulong start, ulong end) => throw new NotImplementedException("Cannot split a comment region");

    public override LineInfo GetRegionLineInfo(ulong index)
    {
        return new LineInfo($"", "", $"{lines[index]}", "");
    }

    public override ulong RegionLineOffsetForAddress(UInt64 address)
    {
        return address - AddressStart;
    }

    public override ulong RegionAddressForLine(UInt64 line)
    {
        return AddressStart + line;
    }

    public override void RegionSave(Dictionary<string, object> dict)
    {
        dict["Lines"] = lines.ToList();
    }

    public override IRegionInfo RegionLoad(Dictionary<string, object> dict)
    {
        var lineElem = (JsonElement)dict["Lines"];
        lines = new string[lineElem.GetArrayLength()];
        for (int i = 0; i < lineElem.GetArrayLength(); i++)
        {
            var line = lineElem[i];
            if (line.ValueKind != JsonValueKind.String)
                throw new ArgumentException("Line is not a string");
            var lineString = line.GetString();
            if (lineString == null)
                throw new ArgumentException("Line is null");
            lines[i] = lineString;
        }
        return this;
    }
}

internal class UnknownRegion : IRegionInfo
{
    private bool dataIsKnown;
    public UnknownRegion(UInt64 start, UInt64 end, bool hasData, IRomDataParser parent) 
        : base(start, end, parent, ResourcerConfig.ConfigColour.Unknown)
    {
        dataIsKnown=hasData;
    }

    public override ulong GetRegionLineCount() => AddressEnd - AddressStart + 1;

    public override void Combining(IRegionInfo other) { }
    public override IRegionInfo Split(ulong start, ulong end) => new UnknownRegion(start, end, dataIsKnown, Parent);

    public override LineInfo GetRegionLineInfo(ulong index)
    {
        if (dataIsKnown)
        {
            var db = Parent.GetByte(AddressStart + index);
            return new LineInfo($"{AddressStart + index:X8}", db.ToString("X2"), $"{(Char.IsControl((char)db) ? '.' : (char)db)}", "");
        }
        else
        {
            return new LineInfo($"{AddressStart + index:X8}", "??", "??", "");
        }
    }

    public override ulong RegionLineOffsetForAddress(UInt64 address)
    {
        return address - AddressStart;
    }

    public override ulong RegionAddressForLine(UInt64 line)
    {
        return AddressStart + line;
    }

    public override void RegionSave(Dictionary<string, object> dict)
    {
        dict["DataIsKnown"] = dataIsKnown;
    }

    public override IRegionInfo RegionLoad(Dictionary<string, object> dict)
    {
        dataIsKnown = ((JsonElement)dict["DataIsKnown"]).GetBoolean();
        return this;
    }
}

internal class DataRegion : IRegionInfo
{
    private bool dataIsKnown;
    private UInt64 size;
    public DataRegion(UInt64 start, UInt64 end, UInt64 size, bool hasData, IRomDataParser parent) 
        : base(start, end, parent, ResourcerConfig.ConfigColour.Data)
    {
        dataIsKnown = hasData;
        this.size = size;
    }

    public override ulong GetRegionLineCount() => (AddressEnd - AddressStart + size)/size;

    public override void Combining(IRegionInfo other) { }
    public override IRegionInfo Split(ulong start, ulong end)
    {
        if (size!=end-start+1)
        {
            throw new ArgumentException($"Cannot split a data region of size {size} into {end-start+1}");
        }
        return new DataRegion(start, end, size, dataIsKnown, Parent);
    }

    LineInfo GetByteLineInfo(ulong index)
    {
        var db = Parent.GetByte(AddressStart + index);
        return new LineInfo($"{AddressStart + index:X8}", BytesForLine(AddressStart+index,AddressStart+index+size-1), $"db {db:X2}", "");
    }

    LineInfo GetWordLineInfo(ulong index)
    {
        var index2 = index * 2;
        var address = AddressStart + index2;
        UInt16 dw = Parent.GetByte(address + 0);
        dw <<= 8;
        dw |= Parent.GetByte(address + 1);
        return new LineInfo($"{address:X8}", BytesForSpan(Parent.FetchBytes(address,2)), $"dw {dw:X4}", "");
    }

    LineInfo GetTripleLineInfo(ulong index)
    {
        var index3 = index * 3;
        var address = AddressStart + index3;
        UInt32 dl = Parent.GetByte(address + 0);
        dl <<= 8;
        dl |= Parent.GetByte(address + 1);
        dl <<= 8;
        dl |= Parent.GetByte(address + 2);
        return new LineInfo($"{address:X8}", BytesForSpan(Parent.FetchBytes(address,3)), $"dl {dl:X6}", "");
    }

    LineInfo GetLongLineInfo(ulong index)
    {
        var index4 = index * 4;
        var address = AddressStart + index4;
        UInt32 dl = Parent.GetByte(address + 0);
        dl <<= 8;
        dl |= Parent.GetByte(address + 1);
        dl <<= 8;
        dl |= Parent.GetByte(address + 2);
        dl <<= 8;
        dl |= Parent.GetByte(address + 3);
        return new LineInfo($"{address:X8}", BytesForSpan(Parent.FetchBytes(address,4)), $"dl {dl:X8}", "");
    }

    public override LineInfo GetRegionLineInfo(ulong index)
    {
        if (dataIsKnown)
        {
            switch (size)
            {
                case 1:
                    return GetByteLineInfo(index);
                case 2:
                    return GetWordLineInfo(index);
                case 3:
                    return GetTripleLineInfo(index);
                case 4:
                    return GetLongLineInfo(index);
                default:
                    throw new ArgumentException($"Unknown size {size}");
            }
        }
        else
        {
            return new LineInfo($"{AddressStart + index:X8}", $"??", $"ds {size}", "");
        }
    }

    public override ulong RegionLineOffsetForAddress(UInt64 address)
    {
        return (address - AddressStart)/size;
    }

    public override ulong RegionAddressForLine(UInt64 line)
    {
        return AddressStart + line * size;
    }

    public override void RegionSave(Dictionary<string, object> dict)
    {
        dict["DataIsKnown"] = dataIsKnown;
        dict["Size"] = size;
    }

    public override IRegionInfo RegionLoad(Dictionary<string, object> dict)
    {
        size=((JsonElement)dict["Size"]).GetUInt64();
        dataIsKnown = ((JsonElement)dict["DataIsKnown"]).GetBoolean();
        return this;
    }

    public override bool IsSame(IRange otherRange)
    {
        if (base.IsSame(otherRange))
        {
            if (otherRange is DataRegion other)
            {
                return size == other.size;
            }
        }
        return false;
    }
}

internal class StringRegion : IRegionInfo
{
    private bool dataIsKnown;
    public StringRegion(UInt64 start, UInt64 end, bool hasData, IRomDataParser parent) : base(start, end, parent, ResourcerConfig.ConfigColour.String)
    {
        dataIsKnown = hasData;
    }

    public override ulong GetRegionLineCount() => (AddressEnd - AddressStart + 16) / 16;

    public override bool IsSame(IRange otherRange)
    {
        return false;   // Don't combine string regions
    }

    public override void Combining(IRegionInfo other) { }
    public override IRegionInfo Split(ulong start, ulong end) => new StringRegion(start, end, dataIsKnown, Parent);

    public override LineInfo GetRegionLineInfo(ulong index)
    {
        if (dataIsKnown)
        {
            StringBuilder s = new StringBuilder();
            if (index == 0)
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
            var I = AddressStart + (index * 16);
            return new LineInfo(index == 0 ? $"{I:X8}" : "", BytesForLine(I, Math.Min(AddressEnd, I + 16)), s.ToString(), "");
        }
        else
        {
            StringBuilder s = new ();
            var I = AddressStart + (index * 16);
            for (var a=0;I<=Math.Min(AddressEnd,I+16);a++)
            {
                s.Append($"?? ");
            }
            return new LineInfo(index == 0 ? $"{I:X8}" : "", s.ToString().Trim(), $"ds {AddressEnd - AddressStart + 1}", "");
        }
    }

    public override ulong RegionLineOffsetForAddress(UInt64 address)
    {
        return (address - AddressStart) / 16;
    }

    public override ulong RegionAddressForLine(UInt64 line)
    {
        return AddressStart + (line * 16);
    }

    public override void RegionSave(Dictionary<string, object> dict)
    {
        dict["DataIsKnown"] = dataIsKnown;
    }

    public override IRegionInfo RegionLoad(Dictionary<string, object> dict)
    {
        dataIsKnown = ((JsonElement)dict["DataIsKnown"]).GetBoolean();
        return this;
    }
}

internal class SymbolProvider : ISymbolProvider
{
    public Dictionary<(ulong, int), string> symbols;
    public SymbolProvider()
    {
        symbols = new();
    }

    public void AddSymbol(ulong address, int size, string symbol)
    {
        if (symbols.ContainsKey((address, size)))
            symbols[(address, size)] = symbol;
        else
            symbols.Add((address, size), symbol);
    }

    public bool HasSymbol(ulong address, int size) => symbols.ContainsKey((address, size));
    public string GetSymbol(ulong address, int size) => symbols[(address, size)];
}

internal class CodeRegion : IRegionInfo
{
    protected SortedDictionary<ulong, Instruction> instructions = new();
    public CodeRegion(UInt64 start, UInt64 end, Instruction instruction, IRomDataParser parent) : base(start, end, parent, ResourcerConfig.ConfigColour.Code)
    {
        instructions.Add(start, instruction);
    }

    public override ulong GetRegionLineCount() => (UInt64)instructions.Count;

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

    public override LineInfo GetRegionLineInfo(ulong index)
    {
        var I = instructions.ElementAt((int)index);
        var address = I.Key;
        var bytesAtMemory =BytesForLine(address, address+(ulong)I.Value.Bytes.Length-1);
        var bytesForSpan = BytesForSpan(I.Value.Bytes);

        return new LineInfo($"{I.Key:X8}", $" {I.Value.Address:X8} {bytesAtMemory} | {bytesForSpan}", I.Value.InstructionText(Parent.SymbolProvider), $"; {I.Value.cpuState}");
    }

    public Instruction GetInstructionForLine(ulong index)
    {
        index = ComputeTrueIndex(index);
        return instructions.ElementAt((int)index).Value;
    }

    public override ulong RegionLineOffsetForAddress(UInt64 address)
    {
        UInt64 offset = 0;
        foreach (var i in instructions)
        {
            if (i.Key >= address)
                return offset;
            offset++;
        }
        return (UInt64)(instructions.Count - 1);
    }

    public override ulong RegionAddressForLine(UInt64 line)
    {
        return instructions.ElementAt((int)line).Key;
    }

    public override void RegionSave(Dictionary<string, object> dict)
    {
        dict["Instructions"] = instructions.Select(i => new { Key = i.Key, Value = i.Value.Save() }).ToList();
    }

    public override IRegionInfo RegionLoad(Dictionary<string, object> dict)
    {
        instructions = new SortedDictionary<ulong, Instruction>();
        var instructionsElem = (JsonElement)dict["Instructions"];
        foreach (var i in instructionsElem.EnumerateArray())
        {
            // i is a JsonElement representing an object with Key and Value
            var address = i.GetProperty("Key").GetUInt64();
            var cdict = JsonSerializer.Deserialize<Dictionary<string, object>>(i.GetProperty("Value").ToString());
            if (cdict == null)
                throw new ArgumentException("Cannot deserialize instruction");
            var instruction = Instruction.Load(cdict);
            instructions.Add(address, instruction);
        }
        return this;
    }
}

internal class RomDataParser : IRomDataParser
{
    private const int BYTES_PER_LINE = 16;
    private RangeCollection<IRegionInfo> ranges;
    private IMemoryRegionDataProvider dataProvider;
    private SymbolProvider symbolProvider;
    private UInt64 minAddress, maxAddress;
    private string regionName;
    private IMemoryInformation memoryInfo;

    public IMemoryInformation MemoryInformation => memoryInfo;
    public RomDataParser(string regionName, IMemoryInformation memInfo, IMemoryRegionDataProvider provider)
    {
        this.regionName = regionName;
        this.memoryInfo = memInfo;
        this.dataProvider = provider;
        this.ranges = new RangeCollection<IRegionInfo>();
        this.symbolProvider = new SymbolProvider();
        var (start, end) = memInfo.AddressRange;
        this.minAddress = start;
        this.maxAddress = end;
    }

    public void AddCodeRange(DisassemblerBase disassembler, UInt64 minAddress, UInt64 maxAddress, IMemoryMapper mapper)
    {
        while (minAddress <= maxAddress)
        {
            // minAddress is linear, need to compute approx PC
            var pc = mapper.MapRomToCpu(minAddress);
            if (AddCodeRange(disassembler, pc, out var i, mapper))
            {
                pc += (UInt64)i.Bytes.Length;
                if (i.IsBasicBlockTerminator)
                {
                    return;
                }
                if (i.NextAddresses.Contains(pc))
                {
                    minAddress += (UInt64)i.Bytes.Length;
                }
            }
            else
            {
                return;
            }
        }
    }

    public bool AddCodeRange(DisassemblerBase disassembler, UInt64 pc, out Instruction instruction, IMemoryMapper mapper)
    {
        bool done = false;
        UInt64 length = 0;
        UInt64 address = mapper.MapCpuToHardwareAddress(pc, out var region);
        /*if (region != RetroEditor.Source.Internals.ReverseEngineering.Platform.MemoryRegion.ROM)
        {
            // Not a valid LoROM address - code probably in ram
            //Console.WriteLine($"Invalid LoROM address: {address:X8} in region {region} {pc:X6}");
            instruction = new();
            return false;
        }*/

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
                    throw new Exception($"Error: {result.ErrorMessage}");
                }
            }
            
            var current = ranges.GetRangeContainingAddress(address, out var lineOff);
            if (current != null)
            {
                if (current.Value is CodeRegion cregion)
                {
                    // Check we overlap the instruction
                    var check = cregion.GetInstructionForLine(lineOff);
                    if (check.ToString() != result.Instruction.ToString())        // TODO de-shitiffy
                    {
                        throw new Exception($"Error: instruction overlaps different instruction {check} != {result.Instruction}");
                    }
                }
                else
                {
                    if (((UInt64)result.Instruction.Bytes.Length) > current.Value.AddressEnd - current.Value.AddressStart + 1)
                    {
                        Console.WriteLine($"Error: instruction does not fit! {current.Value.AddressStart:X8} != {result.Instruction.ToString()}");
                    }
                    else
                    {
                        ranges.AddRange(new CodeRegion(address, address + (UInt64)result.BytesConsumed - 1, result.Instruction, this));
                    }
                }
            }
            instruction = result.Instruction;
            return true;
        }
        instruction = new();
        return false;
    }

    public bool CheckRegionUnknown(UInt64 start, UInt64 end)
    {
        for (UInt64 i = start; i <= end; i++)
        {
            var range = ranges.GetRangeContainingAddress(i, out _);
            if (!(range!=null && range.Value is UnknownRegion))
            {
                return false;
            }
        }
        return true;
    }

    public void AddDataRange(UInt64 start, UInt64 end, uint size, bool hasData = true)
    {
        ranges.AddRange(new DataRegion(start, end, size, hasData, this));
    }

    public void AddStringRange(UInt64 start, UInt64 end, bool hasData = true)
    {
        ranges.AddRange(new StringRegion(start, end, hasData, this));
    }

    public void AddUnknownRange(UInt64 start, UInt64 end, bool hasData = true)
    {
        ranges.AddRange(new UnknownRegion(start, end, hasData, this));
    }

    public RangeCollection<IRegionInfo> GetRanges() => ranges;

    public UInt64 GetMinAddress => minAddress;
    public UInt64 GetMaxAddress => maxAddress;

    // Only used by tests
    internal void LoadRomData(ReadOnlySpan<byte> data)
    {
        if (dataProvider is BufferDataProvider bufferProvider)
        {
            bufferProvider.LoadBuffer(data);
        }
        else
        {
            throw new InvalidOperationException($"Region {regionName} does not support direct data loading");
        }
    }

    public void Parse(LibMameDebugger debugger)
    {
        // Load data from debugger for this region
        dataProvider.LoadFromDebugger(debugger);
        
        // Set address bounds based on memory info
        var (start, end) = memoryInfo.AddressRange;
        minAddress = start;
        maxAddress = end;
    }

    public byte[] GetRomData
    {
        get
        {
            // Return data from this region's provider
            if (dataProvider is BufferDataProvider bufferProvider && bufferProvider is DebuggerDataProvider debuggerProvider)
            {
                return debuggerProvider.GetBuffer();
            }
            return new byte[0];
        }
    }

    public ISymbolProvider SymbolProvider => symbolProvider;

    public void AddSymbol(ulong value, int size, string symbol)
    {
        symbolProvider.AddSymbol(value, size, symbol);
    }

    public byte GetByte(UInt64 address)
    {
        return dataProvider.GetByte(address);
    }

    public ReadOnlySpan<byte> FetchBytes(UInt64 address, UInt64 length)
    {
        return dataProvider.FetchBytes(address, length);
    }

    //TODO - doesn't handle mid region insertion....
    internal void AddCommentRange(String[] value, UInt64 start)
    {
        Range<IRegionInfo>? region = ranges.GetRangeContainingAddress(start, out _);
        if (region != null)
        {
            region.Value.Above.Add(new MultiLineComment(value, this));
        }
        ranges.Recompute();
    }

    public void Save(string filePath)
    {
        // Serialize symbolProvider.symbols as Dictionary<string, string>
        var serializableSymbols = symbolProvider.symbols.ToDictionary(
            kvp => $"{kvp.Key.Item1}:{kvp.Key.Item2}",
            kvp => kvp.Value
        );
        
        // Serialize this region's ranges
        var regionData = ranges.Select(r => r.Value.Save()).ToList();
        
        var data = new
        {
            RegionName = regionName,
            Ranges = regionData,
            SymbolProvider = serializableSymbols
        };
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    public void Load(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        if (root.TryGetProperty("SymbolProvider", out var symbolsElem))
        {
            var dict = new Dictionary<(ulong, int), string>();
            foreach (var prop in symbolsElem.EnumerateObject())
            {
                var keyParts = prop.Name.Split(':');
                if (keyParts.Length == 2 && ulong.TryParse(keyParts[0], out var addr) && int.TryParse(keyParts[1], out var size))
                {
                    dict[(addr, size)] = prop.Value.GetString() ?? string.Empty;
                }
            }
            symbolProvider.symbols = dict;
        }
        
        // Load ranges for this region - support both old and new format
        if (root.TryGetProperty("Ranges", out var rangesElem))
        {
            foreach (var rangeElem in rangesElem.EnumerateArray())
            {
                var rangeDict = rangeElem.Deserialize<Dictionary<string, object>>();
                if (rangeDict != null)
                {
                    var range = IRegionInfo.Load(rangeDict, this);
                    ranges.AddRange(range);
                }
            }
        }
        // Backward compatibility: try loading old format with MemoryRegions dictionary
        else if (root.TryGetProperty("MemoryRegions", out var regionsElem))
        {
            foreach (var prop in regionsElem.EnumerateObject())
            {
                // Load data from matching region name in the saved file
                if (prop.Name == regionName)
                {
                    foreach (var rangeElem in prop.Value.EnumerateArray())
                    {
                        var rangeDict = rangeElem.Deserialize<Dictionary<string, object>>();
                        if (rangeDict != null)
                        {
                            var range = IRegionInfo.Load(rangeDict, this);
                            ranges.AddRange(range);
                        }
                    }
                    break;
                }
            }
        }
        // Backward compatibility: try loading old RomRanges/RamRanges format
        else if (root.TryGetProperty("RomRanges", out var romRangesElem))
        {
            foreach (var rangeElem in romRangesElem.EnumerateArray())
            {
                var rangeDict = rangeElem.Deserialize<Dictionary<string, object>>();
                if (rangeDict != null)
                {
                    var range = IRegionInfo.Load(rangeDict, this);
                    ranges.AddRange(range);
                }
            }
        }
    }
}