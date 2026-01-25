using System.Text.Json;
using RetroEditor.Plugins;

/// <summary>
/// Represents the Z80 CPU state that affects instruction decoding
/// </summary>
internal struct Z80State : ICpuState
{
    internal bool InterruptMode { get; set; }

    public ICpuState Clone()
    {
        return new Z80State { InterruptMode = this.InterruptMode };
    }

    public ICpuState Load(Dictionary<string, object> dict)
    {
        if (dict == null) throw new ArgumentNullException(nameof(dict));
        return new Z80State { InterruptMode = ((JsonElement)dict["InterruptMode"]).GetBoolean() };
    }

    public Dictionary<string, object> Save()
    {
        return new Dictionary<string, object> { { "InterruptMode", InterruptMode } };
    }

    public override string ToString() => $"|IM{(InterruptMode ? "1" : "0")}|";
}

internal struct Z80RegisterState : ICpuRegisterState
{
    public UInt16 AF, BC, DE, HL, IX, IY, SP, PC;

    public Z80RegisterState()
    {
        AF = BC = DE = HL = IX = IY = SP = PC = 0;
    }

    public ICpuRegisterState Clone()
    {
        return new Z80RegisterState { AF = AF, BC = BC, DE = DE, HL = HL, IX = IX, IY = IY, SP = SP, PC = PC };
    }
}

/// <summary>
/// Z80 Disassembler for ZX Spectrum and other Z80-based systems
/// </summary>
internal class Z80Disassembler : DisassemblerBase
{
    private static readonly string[] Registers = { "B", "C", "D", "E", "H", "L", "(HL)", "A" };
    private static readonly string[] Register16 = { "BC", "DE", "HL", "SP" };
    private static readonly string[] Conditions = { "NZ", "Z", "NC", "C", "PO", "PE", "P", "M" };

    public override string ArchitectureName => "Z80";
    public override MemoryEndian Endianness => MemoryEndian.Little;

    protected override ICpuState CreateInitialState() => new Z80State { InterruptMode = false };

    public override DecodeResult DecodeNext(ReadOnlySpan<byte> bytes, ulong address)
    {
        if (bytes.Length < 1)
            return DecodeResult.NeedMoreBytes(1);

        var opcode = bytes[0];

        return opcode switch
        {
            // Single-byte instructions
            0x00 => Success("NOP", bytes, address, 1),
            0x07 => Success("RLCA", bytes, address, 1),
            0x0F => Success("RRCA", bytes, address, 1),
            0x17 => Success("RLA", bytes, address, 1),
            0x1F => Success("RRA", bytes, address, 1),
            0x27 => Success("DAA", bytes, address, 1),
            0x2F => Success("CPL", bytes, address, 1),
            0x37 => Success("SCF", bytes, address, 1),
            0x3F => Success("CCF", bytes, address, 1),
            0x76 => Success("HALT", bytes, address, 1, isTerminator: true),
            
            // 8-bit INC
            0x04 or 0x0C or 0x14 or 0x1C or 0x24 or 0x2C or 0x3C =>
                Success(new[] { "INC", Registers[(opcode >> 3) & 7] }, bytes, address, 1),
            
            // 8-bit DEC
            0x05 or 0x0D or 0x15 or 0x1D or 0x25 or 0x2D or 0x3D =>
                Success(new[] { "DEC", Registers[(opcode >> 3) & 7] }, bytes, address, 1),

            // 8-bit register load immediate  
            0x06 or 0x0E or 0x16 or 0x1E or 0x26 or 0x2E or 0x3E => 
                bytes.Length < 2 ? DecodeResult.NeedMoreBytes(2) :
                Success(new[] { "LD", Registers[(opcode >> 3) & 7], $"#{bytes[1]:X2}" }, bytes, address, 2),

            // 16-bit register load immediate
            0x01 or 0x11 or 0x21 or 0x31 =>
                bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success(new[] { "LD", Register16[opcode >> 4], $"#{ReadU16(bytes, 1):X4}" }, bytes, address, 3),

            // Single byte combos
            0x09 => Success("ADD", "HL", "BC", bytes, address, 1),
            0x19 => Success("ADD", "HL", "DE", bytes, address, 1),
            0x29 => Success("ADD", "HL", "HL", bytes, address, 1),
            0x39 => Success("ADD", "HL", "SP", bytes, address, 1),

            // 16-bit INC/DEC
            0x03 or 0x13 or 0x23 or 0x33 =>
                Success(new[] { "INC", Register16[opcode >> 4] }, bytes, address, 1),
            0x0B or 0x1B or 0x2B or 0x3B =>
                Success(new[] { "DEC", Register16[opcode >> 4] }, bytes, address, 1),

            // Special control
            0x08 => Success(new[] { "EX", "AF", "AF'" }, bytes, address, 1),
            0xD9 => Success("EXX", bytes, address, 1),
            0xEB => Success(new[] { "EX", "DE", "HL" }, bytes, address, 1),
            
            // DJNZ - branch with displacement
            0x10 => bytes.Length < 2 ? DecodeResult.NeedMoreBytes(2) :
                Success(new[] { "DJNZ", (sbyte)bytes[1] >= 0 ? $"+{(sbyte)bytes[1]}" : $"{(sbyte)bytes[1]}" }, bytes, address, 2, isBranch: true),
            
            // JR - unconditional branch
            0x18 => bytes.Length < 2 ? DecodeResult.NeedMoreBytes(2) :
                Success(new[] { "JR", (sbyte)bytes[1] >= 0 ? $"+{(sbyte)bytes[1]}" : $"{(sbyte)bytes[1]}" }, bytes, address, 2, isBranch: true),
            
            // JR cc - conditional branch
            _ when (opcode & 0xC7) == 0x20 => 
                bytes.Length < 2 ? DecodeResult.NeedMoreBytes(2) :
                Success(new[] { "JR", Conditions[(opcode >> 3) & 3], (sbyte)bytes[1] >= 0 ? $"+{(sbyte)bytes[1]}" : $"{(sbyte)bytes[1]}" }, bytes, address, 2, isBranch: true),
            
            // Register-to-register LD
            >= 0x40 and <= 0x7F when opcode != 0x76 =>
                Success(new[] { "LD", Registers[(opcode >> 3) & 7], Registers[opcode & 7] }, bytes, address, 1),
            
            // 8-bit ALU operations (ADD, ADC, SUB, SBC, AND, XOR, OR, CP)
            >= 0x80 and <= 0xBF =>
                Success(new[] { GetAluMnemonic(opcode), "A", Registers[opcode & 7] }, bytes, address, 1),

            // ALU with immediate
            0xC6 or 0xCE or 0xD6 or 0xDE or 0xE6 or 0xEE or 0xF6 or 0xFE =>
                bytes.Length < 2 ? DecodeResult.NeedMoreBytes(2) :
                Success(new[] { GetAluMnemonic(opcode), "A", $"#{bytes[1]:X2}" }, bytes, address, 2),

            // Return
            0xC0 or 0xC8 or 0xD0 or 0xD8 or 0xE0 or 0xE8 or 0xF0 or 0xF8 =>
                Success(new[] { "RET", Conditions[(opcode >> 3) & 7] }, bytes, address, 1, isBranch: true),
            0xC9 => Success("RET", bytes, address, 1, isTerminator: true),

            // Jump and call - conditional
            0xC2 or 0xCA or 0xD2 or 0xDA or 0xE2 or 0xEA or 0xF2 or 0xFA =>
                bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success(new[] { "JP", Conditions[(opcode >> 3) & 7], $"${ReadU16(bytes, 1):X4}" }, bytes, address, 3, isBranch: true),
            0xC4 or 0xCC or 0xD4 or 0xDC or 0xE4 or 0xEC or 0xF4 or 0xFC =>
                bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success(new[] { "CALL", Conditions[(opcode >> 3) & 7], $"${ReadU16(bytes, 1):X4}" }, bytes, address, 3, isBranch: true),

            // Jump and call - unconditional
            0xC3 => bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success(new[] { "JP", $"${ReadU16(bytes, 1):X4}" }, bytes, address, 3, isBranch: true, isTerminator: true),
            0xCD => bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success(new[] { "CALL", $"${ReadU16(bytes, 1):X4}" }, bytes, address, 3, isBranch: true),

            // Push/Pop
            0xC1 or 0xD1 or 0xE1 or 0xF1 =>
                Success(new[] { "POP", Register16[(opcode >> 4) & 3] }, bytes, address, 1),
            0xC5 or 0xD5 or 0xE5 or 0xF5 =>
                Success(new[] { "PUSH", Register16[(opcode >> 4) & 3] }, bytes, address, 1),

            // RST - Restart
            0xC7 or 0xCF or 0xD7 or 0xDF or 0xE7 or 0xEF or 0xF7 or 0xFF =>
                Success(new[] { "RST", $"${(((opcode >> 3) & 7) * 8):X4}" }, bytes, address, 1, isBranch: true),

            // I/O
            0xDB => bytes.Length < 2 ? DecodeResult.NeedMoreBytes(2) :
                Success(new[] { "IN", "A", $"#{bytes[1]:X2}" }, bytes, address, 2),
            0xD3 => bytes.Length < 2 ? DecodeResult.NeedMoreBytes(2) :
                Success(new[] { "OUT", $"#{bytes[1]:X2}", "A" }, bytes, address, 2),

            // Control
            0xF3 => Success("DI", bytes, address, 1),
            0xFB => Success("EI", bytes, address, 1),

            // CB prefix (bit operations)
            0xCB => bytes.Length < 2 ? DecodeResult.NeedMoreBytes(2) : DecodeCB(bytes, address),

            // ED prefix (extended)
            0xED => bytes.Length < 2 ? DecodeResult.NeedMoreBytes(2) : DecodeED(bytes, address),

            // DD/FD index registers (simplified)
            0xDD or 0xFD => Success("ILLEGAL", bytes, address, 1),

            _ => Success("ILLEGAL", bytes, address, 1),
        };
    }

    private DecodeResult DecodeCB(ReadOnlySpan<byte> bytes, ulong address)
    {
        var secondByte = bytes[1];
        var x = (secondByte >> 6) & 3;
        var y = (secondByte >> 3) & 7;
        var z = secondByte & 7;

        var mnemonics = new[] {
            new[] { "RLC", "RRC", "RL", "RR", "SLA", "SRA", "SLL", "SRL" },
            new[] { "BIT", "BIT", "BIT", "BIT", "BIT", "BIT", "BIT", "BIT" },
            new[] { "RES", "RES", "RES", "RES", "RES", "RES", "RES", "RES" },
            new[] { "SET", "SET", "SET", "SET", "SET", "SET", "SET", "SET" }
        };

        var mnemonic = mnemonics[x][z >= 8 ? 0 : (y < 3 ? 0 : 1)];
        if (x == 1 || x == 2 || x == 3)
            return Success(new[] { mnemonic, y.ToString(), Registers[z] }, bytes, address, 2);
        
        return Success(new[] { mnemonic, Registers[z] }, bytes, address, 2);
    }

    private DecodeResult DecodeED(ReadOnlySpan<byte> bytes, ulong address)
    {
        var secondByte = bytes[1];
        var mnemonic = secondByte switch
        {
            0x44 => "NEG",
            0x45 => "RETN",
            0x4D => "RETI",
            0xA0 => "LDI",
            0xA1 => "CPI",
            0xA8 => "LDD",
            0xA9 => "CPD",
            0xB0 => "LDIR",
            0xB1 => "CPIR",
            0xB8 => "LDDR",
            0xB9 => "CPDR",
            _ => "ILLEGAL"
        };

        var isTerminator = secondByte == 0x45 || secondByte == 0x4D || secondByte == 0xB0 || secondByte == 0xB8;
        var isBranch = secondByte == 0x45 || secondByte == 0x4D;  // Only RETN/RETI are branches
        return Success(mnemonic, bytes, address, 2, isBranch: isBranch, isTerminator: isTerminator);
    }

    private string GetAluMnemonic(byte opcode)
    {
        return ((opcode >> 3) & 7) switch
        {
            0 => "ADD",
            1 => "ADC",
            2 => "SUB",
            3 => "SBC",
            4 => "AND",
            5 => "XOR",
            6 => "OR",
            7 => "CP",
            _ => "???"
        };
    }

    private string GetAluMnemonIc(byte opcode)
    {
        return ((opcode >> 3) & 7) switch
        {
            0 => "ADD",
            1 => "ADC",
            2 => "SUB",
            3 => "SBC",
            4 => "AND",
            5 => "XOR",
            6 => "OR",
            7 => "CP",
            _ => "???"
        };
    }

    private UInt16 ReadU16(ReadOnlySpan<byte> bytes, int offset)
    {
        return (UInt16)(bytes[offset] | (bytes[offset + 1] << 8));
    }

    private DecodeResult Success(string mnemonic, ReadOnlySpan<byte> bytes, ulong address, int size, bool isBranch = false, bool isTerminator = false)
    {
        var instructionBytes = SliceBytes(bytes, size);
        var instruction = new Instruction(address, mnemonic, new List<IOperand>(), instructionBytes, State.Clone());
        instruction.IsBranch = isBranch;
        instruction.IsBasicBlockTerminator = isTerminator;
        return DecodeResult.CreateSuccess(instruction, size);
    }

    private DecodeResult Success(string[] operands, ReadOnlySpan<byte> bytes, ulong address, int size, bool isBranch = false, bool isTerminator = false)
    {
        var ops = new List<IOperand>();
        for (int i = 1; i < operands.Length; i++)
            ops.Add(new Z80RegisterOperand(operands[i]));
        var instructionBytes = SliceBytes(bytes, size);
        var instruction = new Instruction(address, operands[0], ops, instructionBytes, State.Clone());
        instruction.IsBranch = isBranch;
        instruction.IsBasicBlockTerminator = isTerminator;
        return DecodeResult.CreateSuccess(instruction, size);
    }

    private DecodeResult Success(string mnemonic, string op1, string op2, ReadOnlySpan<byte> bytes, ulong address, int size)
    {
        var instructionBytes = SliceBytes(bytes, size);
        var ops = new List<IOperand> { new Z80RegisterOperand(op1), new Z80RegisterOperand(op2) };
        var instruction = new Instruction(address, mnemonic, ops, instructionBytes, State.Clone());
        return DecodeResult.CreateSuccess(instruction, size);
    }

    private static byte[] SliceBytes(ReadOnlySpan<byte> bytes, int size)
    {
        var arr = new byte[size];
        bytes.Slice(0, size).CopyTo(arr);
        return arr;
    }

    public override List<(ulong address, uint size)> FetchMemoryAccesses(Instruction ins, ICpuRegisterState registers)
    {
        return new List<(ulong, uint)>();
    }
}
