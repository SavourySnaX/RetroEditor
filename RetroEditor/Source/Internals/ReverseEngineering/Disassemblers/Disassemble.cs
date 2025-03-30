using RetroEditor.Plugins;

/// <summary>
/// Represents an operand in an instruction
/// </summary>
internal class Operand
{
    /// <summary>
    /// The text representation of the operand
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Whether this operand is a source operand
    /// </summary>
    public bool IsSource { get; set; }

    /// <summary>
    /// Whether this operand is a destination operand
    /// </summary>
    public bool IsDestination { get; set; }

    /// <summary>
    /// The value of the operand (if it's an immediate value)
    /// </summary>
    public ulong? Value { get; set; }

    public Operand(string text, bool isSource = false, bool isDestination = false, ulong? value = null)
    {
        Text = text;
        IsSource = isSource;
        IsDestination = isDestination;
        Value = value;
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
    public List<Operand> Operands { get; set; }

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

    /// <summary>
    /// The possible next instruction addresses after this instruction
    /// </summary>
    public List<ulong> NextAddresses { get; set; }
    public ICpuState cpuState { get; set; }

    public Instruction()
    {
        Operands = new List<Operand>();
        NextAddresses = new List<ulong>();
        Mnemonic = string.Empty;
        Bytes = Array.Empty<byte>();
        cpuState = new EmptyState();
    }

    public Instruction(ulong address, string mnemonic, List<Operand> operands, byte[] bytes, ICpuState state)
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
        string operandsText = string.Join(", ", Operands.Select(o => o.Text));
        return $"{Address:X8}: {Mnemonic} {operandsText}";
    }

    public string InstructionText()
    {
        string operandsText = string.Join(", ", Operands.Select(o => o.Text));
        return $"{Mnemonic} {operandsText}";
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
}

internal struct EmptyState : ICpuState
{
    public ICpuState Clone()
    {
        return new EmptyState();
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
