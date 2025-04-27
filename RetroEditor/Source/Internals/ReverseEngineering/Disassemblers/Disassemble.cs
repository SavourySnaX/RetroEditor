using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using RetroEditor.Plugins;


internal interface ISymbolProvider
{
    bool HasSymbol(ulong address, int symbolLength);  // Maybe we need more disambiguation ... TODO
    string GetSymbol(ulong address, int symbolLength);  // Maybe we need more disambiguation ... TODO
}

internal abstract class IOperand
{
    public abstract string Text(ISymbolProvider symbols);
    public abstract string Text();

    public bool IsSource { get; set; }

    public bool IsDestination { get; set; }

    public ulong Value { get; set; }

    public IOperand(bool isSource = false, bool isDestination = false, ulong value = 0)
    {
        IsSource = isSource;
        IsDestination = isDestination;
        Value = value;
    }

    // Custom serialization for operands
    public virtual Dictionary<string, object> Save()
    {
        return new Dictionary<string, object>
        {
            { "Type", GetType().Name },
            { "IsSource", IsSource },
            { "IsDestination", IsDestination },
            { "Value", Value }
        };
    }

    // Custom deserialization for operands
    public static IOperand Load(Dictionary<string, object> dict)
    {
        var typeName = ((JsonElement)dict["Type"]).GetString();
        var type = Type.GetType(typeName);
        if (type == null)
            throw new InvalidOperationException($"Unknown operand type: {typeName}");
        var operand = RuntimeHelpers.GetUninitializedObject(type) as IOperand;
        if (operand==null)
            throw new InvalidOperationException($"Cannot create operand of type: {typeName}");
        operand.IsSource = ((JsonElement)dict["IsSource"]).GetBoolean();
        operand.IsDestination = ((JsonElement)dict["IsDestination"]).GetBoolean();
        operand.Value = ((JsonElement)dict["Value"]).GetUInt64();
        return operand;
    }
}

/// <summary>
/// Represents a single disassembled instruction
/// </summary>
internal class Instruction
{
    /// <summary>
    /// The address where this instruction is located
    /// </summary>
    public ulong Address { get; set; }

    /// <summary>
    /// The mnemonic of the instruction
    /// </summary>
    public string Mnemonic { get; set; }

    /// <summary>
    /// The operands of the instruction
    /// </summary>
    public List<IOperand> Operands { get; set; }

    /// <summary>
    /// The raw bytes that make up this instruction
    /// </summary>
    public byte[] Bytes { get; set; }

    /// <summary>
    /// Whether this instruction is a branch/jump instruction
    /// </summary>
    public bool IsBranch { get; set; }

    /// <summary>
    /// Whether this instruction terminates a basic block
    /// </summary>
    public bool IsBasicBlockTerminator { get; set; }

    public List<ulong> NextAddresses { get; set; }
    public ICpuState cpuState { get; set; }

    public Instruction()
    {
        Operands = new List<IOperand>();
        NextAddresses = new List<ulong>();
        Mnemonic = string.Empty;
        Bytes = Array.Empty<byte>();
        cpuState = new EmptyState();
    }

    public Instruction(ulong address, string mnemonic, List<IOperand> operands, byte[] bytes, ICpuState state)
    {
        Address = address;
        Mnemonic = mnemonic;
        Operands = operands;
        Bytes = bytes;
        NextAddresses = new List<ulong>();
        cpuState = state;
    }

    public override string ToString()
    {
        string operandsText = string.Join(", ", Operands.Select(o => o.Text()));
        return $"{Address:X8}: {Mnemonic} {operandsText}";
    }

    public string InstructionText(ISymbolProvider symbols)
    {
        string operandsText = string.Join(", ", Operands.Select(o => o.Text(symbols)));
        return $"{Mnemonic} {operandsText}";
    }

    // Custom serialization for Instruction
    public virtual Dictionary<string, object> Save()
    {
        return new Dictionary<string, object>
        {
            { "Address", Address },
            { "Mnemonic", Mnemonic },
            { "Operands", Operands.Select(o => o.Save()).ToList() },
            { "Bytes", Bytes },
            { "IsBranch", IsBranch },
            { "IsBasicBlockTerminator", IsBasicBlockTerminator },
            { "NextAddresses", NextAddresses },
            { "CpuStateType", cpuState.GetType().Name},
            { "CpuState", cpuState.Save() }
        };
    }

    // Custom deserialization for Instruction
    public static Instruction Load(Dictionary<string, object> dict)
    {
        var instr = new Instruction();
        instr.Address = ((JsonElement)dict["Address"]).GetUInt64();
        instr.Mnemonic = dict["Mnemonic"]?.ToString() ?? string.Empty;
        instr.Operands = new List<IOperand>();
        foreach (var o in ((JsonElement)dict["Operands"]).EnumerateArray())
        {
            var odict = JsonSerializer.Deserialize<Dictionary<string, object>>(o.ToString());
            if (odict != null)
                instr.Operands.Add(IOperand.Load(odict));
        }
        instr.Bytes = ((JsonElement)dict["Bytes"]).GetBytesFromBase64();
        instr.IsBranch = ((JsonElement)dict["IsBranch"]).GetBoolean();
        instr.IsBasicBlockTerminator = ((JsonElement)dict["IsBasicBlockTerminator"]).GetBoolean();
        if (dict["NextAddresses"] is IEnumerable<object> nextList)
            instr.NextAddresses = nextList.Select(Convert.ToUInt64).ToList();
        else
            instr.NextAddresses = new List<ulong>();
        var type = Type.GetType(((JsonElement)dict["CpuStateType"]).GetString());
        if (type == null)
            throw new ArgumentException($"Cannot find type {dict["CpuStateType"]}");

        var cpuState = RuntimeHelpers.GetUninitializedObject(type) as ICpuState;
        if (cpuState == null)
        {
            throw new ArgumentException($"Cannot create type {dict["CpuStateType"]}");
        }
        var stateDict = JsonSerializer.Deserialize<Dictionary<string, object>>(dict["CpuState"].ToString());
        if (stateDict == null)
        {
            throw new ArgumentException($"Cannot deserialize state for type {dict["CpuStateType"]}");
        }
        instr.cpuState = cpuState.Load(stateDict);
        return instr;
    }
}

/// <summary>
/// Represents the current state of a CPU that affects instruction decoding
/// </summary>
internal interface ICpuState
{
    /// <summary>
    /// Creates a deep copy of the CPU state
    /// </summary>
    public ICpuState Clone();

    public Dictionary<string, object> Save();
    public ICpuState Load(Dictionary<string, object> dict);
}

internal struct EmptyState : ICpuState
{
    public ICpuState Clone()
    {
        return new EmptyState();
    }

    public ICpuState Load(Dictionary<string, object> dict)
    {
        return new EmptyState();
    }

    public Dictionary<string, object> Save()
    {
        return new Dictionary<string, object>();
    }
}

/// <summary>
/// Represents the result of attempting to decode an instruction
/// </summary>
internal class DecodeResult
{
    /// <summary>
    /// The decoded instruction, if successful
    /// </summary>
    public required Instruction Instruction { get; set; }

    /// <summary>
    /// The number of bytes consumed by the instruction
    /// </summary>
    public int BytesConsumed { get; set; }

    /// <summary>
    /// Whether more bytes are needed to complete the instruction
    /// </summary>
    public bool NeedsMoreBytes { get; set; }

    /// <summary>
    /// The number of additional bytes needed
    /// </summary>
    public int AdditionalBytesNeeded { get; set; }

    /// <summary>
    /// Whether the instruction was successfully decoded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Any error message if decoding failed
    /// </summary>
    public string ErrorMessage { get; set; } = "";

    public static DecodeResult NeedMoreBytes(int additionalBytes)
    {
        return new DecodeResult
        {
            Success = false,
            Instruction = new(),
            NeedsMoreBytes = true,
            AdditionalBytesNeeded = additionalBytes
        };
    }

    public static DecodeResult CreateSuccess(Instruction instruction, int bytesConsumed)
    {
        return new DecodeResult
        {
            Success = true,
            Instruction = instruction,
            BytesConsumed = bytesConsumed,
            NeedsMoreBytes = false
        };
    }

    public static DecodeResult CreateError(string message)
    {
        return new DecodeResult
        {
            Success = false,
            Instruction = new(),
            ErrorMessage = message,
            NeedsMoreBytes = false
        };
    }
}

/// <summary>
/// Base interface for all disassemblers
/// </summary>
internal interface IDisassembler
{
    /// <summary>
    /// Gets the name of the CPU architecture this disassembler supports
    /// </summary>
    string ArchitectureName { get; }

    /// <summary>
    /// Gets the endianness of the CPU architecture
    /// </summary>
    MemoryEndian Endianness { get; }

    /// <summary>
    /// Gets or sets the current CPU state
    /// </summary>
    ICpuState State { get; set; }

    /// <summary>
    /// Attempts to decode the next instruction from the given bytes
    /// </summary>
    /// <param name="bytes">The bytes to decode</param>
    /// <param name="address">The address where the bytes start</param>
    /// <returns>A result indicating success, need for more bytes, or error</returns>
    DecodeResult DecodeNext(ReadOnlySpan<byte> bytes, ulong address);
}

/// <summary>
/// Base class for implementing CPU-specific disassemblers
/// </summary>
internal abstract class DisassemblerBase : IDisassembler
{
    private ICpuState _state;

    protected DisassemblerBase()
    {
        _state = CreateInitialState();
    }

    public abstract string ArchitectureName { get; }
    public abstract MemoryEndian Endianness { get; }

    public ICpuState State
    {
        get => _state;
        set => _state = value ?? CreateInitialState();
    }

    /// <summary>
    /// Creates the initial CPU state for this architecture
    /// </summary>
    protected abstract ICpuState CreateInitialState();

    /// <summary>
    /// Attempts to decode the next instruction from the given bytes
    /// </summary>
    public abstract DecodeResult DecodeNext(ReadOnlySpan<byte> bytes, ulong address);

    /// <summary>
    /// Helper method to read a value from memory in the correct endianness
    /// </summary>
    protected ulong ReadValue(ReadOnlySpan<byte> bytes, int offset, int size)
    {
        if (Endianness == MemoryEndian.Little)
        {
            ulong value = 0;
            for (int i = 0; i < size; i++)
            {
                value |= (ulong)bytes[offset + i] << (i * 8);
            }
            return value;
        }
        else
        {
            ulong value = 0;
            for (int i = 0; i < size; i++)
            {
                value |= (ulong)bytes[offset + i] << ((size - 1 - i) * 8);
            }
            return value;
        }
    }
}
