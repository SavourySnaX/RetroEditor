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
            0x02 => Success(new[] { "LD", "(BC)", "A" }, bytes, address, 1),
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
            0x04 or 0x0C or 0x14 or 0x1C or 0x24 or 0x2C or 0x34 or 0x3C =>
                Success(new[] { "INC", Registers[(opcode >> 3) & 7] }, bytes, address, 1),
            
            // 8-bit DEC
            0x05 or 0x0D or 0x15 or 0x1D or 0x25 or 0x2D or 0x35 or 0x3D =>
                Success(new[] { "DEC", Registers[(opcode >> 3) & 7] }, bytes, address, 1),

            // 8-bit register load immediate  
            0x06 or 0x0E or 0x16 or 0x1E or 0x26 or 0x2E or 0x36 or 0x3E => 
                bytes.Length < 2 ? DecodeResult.NeedMoreBytes(2) :
                Success(new[] { "LD", Registers[(opcode >> 3) & 7], $"#{bytes[1]:X2}" }, bytes, address, 2),

            // 16-bit register load immediate
            0x01 or 0x11 or 0x21 or 0x31 =>
                bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success(new[] { "LD", Register16[opcode >> 4], $"#{ReadU16(bytes, 1):X4}" }, bytes, address, 3),
                
            // 16-bit HL memory operations
            0x22 => bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success(new[] { "LD", $"(${ReadU16(bytes, 1):X4})", "HL" }, bytes, address, 3),
            0x2A => bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success(new[] { "LD", "HL", $"(${ReadU16(bytes, 1):X4})" }, bytes, address, 3),
                
            // 8-bit A memory operations
            0x32 => bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success(new[] { "LD", $"(${ReadU16(bytes, 1):X4})", "A" }, bytes, address, 3),
            0x3A => bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success(new[] { "LD", "A", $"(${ReadU16(bytes, 1):X4})" }, bytes, address, 3),

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
            
            0x0A => Success(new[] { "LD", "A", "(BC)" }, bytes, address, 1),
            0x12 => Success(new[] { "LD", "(DE)", "A" }, bytes, address, 1),
            0x1A => Success(new[] { "LD", "A", "(DE)" }, bytes, address, 1),

            // Special control
            0x08 => Success(new[] { "EX", "AF", "AF'" }, bytes, address, 1),
            0xD9 => Success("EXX", bytes, address, 1),
            0xE3 => Success(new[] { "EX", "(SP)", "HL" }, bytes, address, 1),
            0xEB => Success(new[] { "EX", "DE", "HL" }, bytes, address, 1),
            
            // DJNZ - branch with displacement
            0x10 => bytes.Length < 2 ? DecodeResult.NeedMoreBytes(2) :
                Success(new[] { "DJNZ", (sbyte)bytes[1] >= 0 ? $"+{(sbyte)bytes[1]}" : $"{(sbyte)bytes[1]}" }, bytes, address, 2, isBranch: true),
            
            // JR - unconditional branch
            0x18 => bytes.Length < 2 ? DecodeResult.NeedMoreBytes(2) :
                Success(new[] { "JR", (sbyte)bytes[1] >= 0 ? $"+{(sbyte)bytes[1]}" : $"{(sbyte)bytes[1]}" }, bytes, address, 2, isBranch: true),
            
            // JR cc - conditional branch
            0x20 or 0x28 or 0x30 or 0x38 => 
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
                Success(new[] { "RST", $"${(((opcode >> 3) & 7) * 8):X4}" }, bytes, address, 1, isBranch: true, isTerminator: true),    // RST vectors often change return address

            // I/O
            0xDB => bytes.Length < 2 ? DecodeResult.NeedMoreBytes(2) :
                Success(new[] { "IN", "A", $"#{bytes[1]:X2}" }, bytes, address, 2),
            0xD3 => bytes.Length < 2 ? DecodeResult.NeedMoreBytes(2) :
                Success(new[] { "OUT", $"#{bytes[1]:X2}", "A" }, bytes, address, 2),

            // Control
            0xF3 => Success("DI", bytes, address, 1),
            0xF9 => Success(new[] { "LD", "SP", "HL" }, bytes, address, 1),
            0xFB => Success("EI", bytes, address, 1),

            // CB prefix (bit operations)
            0xCB => bytes.Length < 2 ? DecodeResult.NeedMoreBytes(2) : DecodeCB(bytes, address),

            // ED prefix (extended)
            0xED => bytes.Length < 2 ? DecodeResult.NeedMoreBytes(2) : DecodeED(bytes, address),

            // DD/FD index registers (IX/IY)
            0xDD => bytes.Length < 2 ? DecodeResult.NeedMoreBytes(2) : DecodeDD(bytes, address),
            0xFD => bytes.Length < 2 ? DecodeResult.NeedMoreBytes(2) : DecodeFD(bytes, address),

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
        
        return secondByte switch
        {
            // Single byte ED instructions
            0x44 => Success("NEG", bytes, address, 2),
            0x45 => Success("RETN", bytes, address, 2, isBranch: true, isTerminator: true),
            0x46 => Success(new[] { "IM", "0" }, bytes, address, 2),
            0x47 => Success(new[] { "LD", "I", "A" }, bytes, address, 2),
            0x4D => Success("RETI", bytes, address, 2, isBranch: true, isTerminator: true),
            0x4F => Success(new[] { "LD", "R", "A" }, bytes, address, 2),
            0x56 => Success(new[] { "IM", "1" }, bytes, address, 2),
            0x57 => Success(new[] { "LD", "A", "I" }, bytes, address, 2),
            0x5E => Success(new[] { "IM", "2" }, bytes, address, 2),
            0x5F => Success(new[] { "LD", "A", "R" }, bytes, address, 2),
            0x67 => Success("RRD", bytes, address, 2),
            0x6F => Success("RLD", bytes, address, 2),
            
            // Block transfer/search instructions
            0xA0 => Success("LDI", bytes, address, 2),
            0xA1 => Success("CPI", bytes, address, 2),
            0xA2 => Success("INI", bytes, address, 2),
            0xA3 => Success("OUTI", bytes, address, 2),
            0xA8 => Success("LDD", bytes, address, 2),
            0xA9 => Success("CPD", bytes, address, 2),
            0xAA => Success("IND", bytes, address, 2),
            0xAB => Success("OUTD", bytes, address, 2),
            0xB0 => Success("LDIR", bytes, address, 2, isTerminator: true),
            0xB1 => Success("CPIR", bytes, address, 2, isTerminator: true),
            0xB2 => Success("INIR", bytes, address, 2, isTerminator: true),
            0xB3 => Success("OTIR", bytes, address, 2, isTerminator: true),
            0xB8 => Success("LDDR", bytes, address, 2, isTerminator: true),
            0xB9 => Success("CPDR", bytes, address, 2, isTerminator: true),
            0xBA => Success("INDR", bytes, address, 2, isTerminator: true),
            0xBB => Success("OTDR", bytes, address, 2, isTerminator: true),
            
            // 16-bit register loads with (nn)
            0x43 or 0x53 or 0x63 or 0x73 => bytes.Length < 4 ? DecodeResult.NeedMoreBytes(4) :
                Success(new[] { "LD", $"(${ReadU16(bytes, 2):X4})", Get16BitRegED(secondByte) }, bytes, address, 4),
            0x4B or 0x5B or 0x6B or 0x7B => bytes.Length < 4 ? DecodeResult.NeedMoreBytes(4) :
                Success(new[] { "LD", Get16BitRegED(secondByte), $"(${ReadU16(bytes, 2):X4})" }, bytes, address, 4),
            
            // 16-bit arithmetic with HL
            0x42 or 0x52 or 0x62 or 0x72 =>
                Success(new[] { "SBC", "HL", Get16BitRegED(secondByte) }, bytes, address, 2),
            0x4A or 0x5A or 0x6A or 0x7A =>
                Success(new[] { "ADC", "HL", Get16BitRegED(secondByte) }, bytes, address, 2),
            
            // IN/OUT instructions
            >= 0x40 and <= 0x78 when (secondByte & 0x07) == 0x00 =>
                Success(new[] { "IN", GetRegED((secondByte >> 3) & 7), "(C)" }, bytes, address, 2),
            >= 0x41 and <= 0x79 when (secondByte & 0x07) == 0x01 =>
                Success(new[] { "OUT", "(C)", GetRegED((secondByte >> 3) & 7) }, bytes, address, 2),
            
            _ => Success("ILLEGAL", bytes, address, 2)
        };
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

    private string Get16BitRegED(byte opcode)
    {
        return ((opcode >> 4) & 3) switch
        {
            0 => "BC",
            1 => "DE",
            2 => "HL",
            3 => "SP",
            _ => "??"
        };
    }
    
    private string GetRegED(int regIndex)
    {
        return regIndex switch
        {
            0 => "B", 1 => "C", 2 => "D", 3 => "E",
            4 => "H", 5 => "L", 6 => "(HL)", 7 => "A",
            _ => "??"
        };
    }
    
    private DecodeResult DecodeDD(ReadOnlySpan<byte> bytes, ulong address)
    {
        var secondByte = bytes[1];
        
        return secondByte switch
        {
            // IX register operations
            0x09 or 0x19 or 0x29 or 0x39 =>
                Success(new[] { "ADD", "IX", GetIX16Reg(secondByte >> 4) }, bytes, address, 2),
            0x21 => bytes.Length < 4 ? DecodeResult.NeedMoreBytes(4) :
                Success(new[] { "LD", "IX", $"#{ReadU16(bytes, 2):X4}" }, bytes, address, 4),
            0x22 => bytes.Length < 4 ? DecodeResult.NeedMoreBytes(4) :
                Success(new[] { "LD", $"(${ReadU16(bytes, 2):X4})", "IX" }, bytes, address, 4),
            0x23 => Success(new[] { "INC", "IX" }, bytes, address, 2),
            0x24 => Success(new[] { "INC", "IXH" }, bytes, address, 2),
            0x25 => Success(new[] { "DEC", "IXH" }, bytes, address, 2),
            0x26 => bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success(new[] { "LD", "IXH", $"#{bytes[2]:X2}" }, bytes, address, 3),
            0x2A => bytes.Length < 4 ? DecodeResult.NeedMoreBytes(4) :
                Success(new[] { "LD", "IX", $"(${ReadU16(bytes, 2):X4})" }, bytes, address, 4),
            0x2B => Success(new[] { "DEC", "IX" }, bytes, address, 2),
            0x2C => Success(new[] { "INC", "IXL" }, bytes, address, 2),
            0x2D => Success(new[] { "DEC", "IXL" }, bytes, address, 2),
            0x2E => bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success(new[] { "LD", "IXL", $"#{bytes[2]:X2}" }, bytes, address, 3),
            0xE1 => Success(new[] { "POP", "IX" }, bytes, address, 2),
            0xE3 => Success(new[] { "EX", "(SP)", "IX" }, bytes, address, 2),
            0xE5 => Success(new[] { "PUSH", "IX" }, bytes, address, 2),
            0xE9 => Success(new[] { "JP", "(IX)" }, bytes, address, 2, isBranch: true, isTerminator: true),
            0xF9 => Success(new[] { "LD", "SP", "IX" }, bytes, address, 2),
            
            // IX+displacement operations
            0x34 or 0x35 => bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success(new[] { secondByte == 0x34 ? "INC" : "DEC", $"(IX{FormatDisplacement((sbyte)bytes[2])})" }, bytes, address, 3),
            0x36 => bytes.Length < 4 ? DecodeResult.NeedMoreBytes(4) :
                Success(new[] { "LD", $"(IX{FormatDisplacement((sbyte)bytes[2])})", $"#{bytes[3]:X2}" }, bytes, address, 4),
            
            // LD operations with IXH/IXL (0x44-0x45, 0x4C-0x4D, 0x54-0x55, 0x5C-0x5D, 0x60-0x6F, 0x7C-0x7D)
            0x44 => Success(new[] { "LD", "B", "IXH" }, bytes, address, 2),
            0x45 => Success(new[] { "LD", "B", "IXL" }, bytes, address, 2),
            0x4C => Success(new[] { "LD", "C", "IXH" }, bytes, address, 2),
            0x4D => Success(new[] { "LD", "C", "IXL" }, bytes, address, 2),
            0x54 => Success(new[] { "LD", "D", "IXH" }, bytes, address, 2),
            0x55 => Success(new[] { "LD", "D", "IXL" }, bytes, address, 2),
            0x5C => Success(new[] { "LD", "E", "IXH" }, bytes, address, 2),
            0x5D => Success(new[] { "LD", "E", "IXL" }, bytes, address, 2),
            0x60 => Success(new[] { "LD", "IXH", "B" }, bytes, address, 2),
            0x61 => Success(new[] { "LD", "IXH", "C" }, bytes, address, 2),
            0x62 => Success(new[] { "LD", "IXH", "D" }, bytes, address, 2),
            0x63 => Success(new[] { "LD", "IXH", "E" }, bytes, address, 2),
            0x64 => Success(new[] { "LD", "IXH", "IXH" }, bytes, address, 2),
            0x65 => Success(new[] { "LD", "IXH", "IXL" }, bytes, address, 2),
            0x67 => Success(new[] { "LD", "IXH", "A" }, bytes, address, 2),
            0x68 => Success(new[] { "LD", "IXL", "B" }, bytes, address, 2),
            0x69 => Success(new[] { "LD", "IXL", "C" }, bytes, address, 2),
            0x6A => Success(new[] { "LD", "IXL", "D" }, bytes, address, 2),
            0x6B => Success(new[] { "LD", "IXL", "E" }, bytes, address, 2),
            0x6C => Success(new[] { "LD", "IXL", "IXH" }, bytes, address, 2),
            0x6D => Success(new[] { "LD", "IXL", "IXL" }, bytes, address, 2),
            0x6F => Success(new[] { "LD", "IXL", "A" }, bytes, address, 2),
            0x7C => Success(new[] { "LD", "A", "IXH" }, bytes, address, 2),
            0x7D => Success(new[] { "LD", "A", "IXL" }, bytes, address, 2),
            
            // ALU operations with IXH/IXL
            0x84 => Success(new[] { "ADD", "A", "IXH" }, bytes, address, 2),
            0x85 => Success(new[] { "ADD", "A", "IXL" }, bytes, address, 2),
            0x8C => Success(new[] { "ADC", "A", "IXH" }, bytes, address, 2),
            0x8D => Success(new[] { "ADC", "A", "IXL" }, bytes, address, 2),
            0x94 => Success(new[] { "SUB", "A", "IXH" }, bytes, address, 2),
            0x95 => Success(new[] { "SUB", "A", "IXL" }, bytes, address, 2),
            0x9C => Success(new[] { "SBC", "A", "IXH" }, bytes, address, 2),
            0x9D => Success(new[] { "SBC", "A", "IXL" }, bytes, address, 2),
            0xA4 => Success(new[] { "AND", "A", "IXH" }, bytes, address, 2),
            0xA5 => Success(new[] { "AND", "A", "IXL" }, bytes, address, 2),
            0xAC => Success(new[] { "XOR", "A", "IXH" }, bytes, address, 2),
            0xAD => Success(new[] { "XOR", "A", "IXL" }, bytes, address, 2),
            0xB4 => Success(new[] { "OR", "A", "IXH" }, bytes, address, 2),
            0xB5 => Success(new[] { "OR", "A", "IXL" }, bytes, address, 2),
            0xBC => Success(new[] { "CP", "A", "IXH" }, bytes, address, 2),
            0xBD => Success(new[] { "CP", "A", "IXL" }, bytes, address, 2),
            >= 0x46 and <= 0x7E when secondByte != 0x76 => bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                DecodeIXDisplacementLD(bytes, address, secondByte),
                
            // ALU operations with IX+displacement (0x86-0xBE range)
            >= 0x86 and <= 0xBE when (secondByte & 0x07) == 0x06 => bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                DecodeIXDisplacementALU(bytes, address, secondByte),
            
            // IX CB prefix
            0xCB => bytes.Length < 4 ? DecodeResult.NeedMoreBytes(4) : DecodeIXCB(bytes, address),
            
            _ => Success("ILLEGAL", bytes, address, 2)
        };
    }
    
    private DecodeResult DecodeFD(ReadOnlySpan<byte> bytes, ulong address)
    {
        var secondByte = bytes[1];
        
        return secondByte switch
        {
            // IY register operations  
            0x09 or 0x19 or 0x29 or 0x39 =>
                Success(new[] { "ADD", "IY", GetIY16Reg(secondByte >> 4) }, bytes, address, 2),
            0x21 => bytes.Length < 4 ? DecodeResult.NeedMoreBytes(4) :
                Success(new[] { "LD", "IY", $"#{ReadU16(bytes, 2):X4}" }, bytes, address, 4),
            0x22 => bytes.Length < 4 ? DecodeResult.NeedMoreBytes(4) :
                Success(new[] { "LD", $"(${ReadU16(bytes, 2):X4})", "IY" }, bytes, address, 4),
            0x23 => Success(new[] { "INC", "IY" }, bytes, address, 2),
            0x24 => Success(new[] { "INC", "IYH" }, bytes, address, 2),
            0x25 => Success(new[] { "DEC", "IYH" }, bytes, address, 2),
            0x26 => bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success(new[] { "LD", "IYH", $"#{bytes[2]:X2}" }, bytes, address, 3),
            0x2A => bytes.Length < 4 ? DecodeResult.NeedMoreBytes(4) :
                Success(new[] { "LD", "IY", $"(${ReadU16(bytes, 2):X4})" }, bytes, address, 4),
            0x2B => Success(new[] { "DEC", "IY" }, bytes, address, 2),
            0x2C => Success(new[] { "INC", "IYL" }, bytes, address, 2),
            0x2D => Success(new[] { "DEC", "IYL" }, bytes, address, 2),
            0x2E => bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success(new[] { "LD", "IYL", $"#{bytes[2]:X2}" }, bytes, address, 3),
            0xE1 => Success(new[] { "POP", "IY" }, bytes, address, 2),
            0xE3 => Success(new[] { "EX", "(SP)", "IY" }, bytes, address, 2),
            0xE5 => Success(new[] { "PUSH", "IY" }, bytes, address, 2),
            0xE9 => Success(new[] { "JP", "(IY)" }, bytes, address, 2, isBranch: true, isTerminator: true),
            0xF9 => Success(new[] { "LD", "SP", "IY" }, bytes, address, 2),
            
            // IY+displacement operations
            0x34 or 0x35 => bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success(new[] { secondByte == 0x34 ? "INC" : "DEC", $"(IY{FormatDisplacement((sbyte)bytes[2])})" }, bytes, address, 3),
            0x36 => bytes.Length < 4 ? DecodeResult.NeedMoreBytes(4) :
                Success(new[] { "LD", $"(IY{FormatDisplacement((sbyte)bytes[2])})", $"#{bytes[3]:X2}" }, bytes, address, 4),
            
            // LD operations with IYH/IYL
            0x44 => Success(new[] { "LD", "B", "IYH" }, bytes, address, 2),
            0x45 => Success(new[] { "LD", "B", "IYL" }, bytes, address, 2),
            0x4C => Success(new[] { "LD", "C", "IYH" }, bytes, address, 2),
            0x4D => Success(new[] { "LD", "C", "IYL" }, bytes, address, 2),
            0x54 => Success(new[] { "LD", "D", "IYH" }, bytes, address, 2),
            0x55 => Success(new[] { "LD", "D", "IYL" }, bytes, address, 2),
            0x5C => Success(new[] { "LD", "E", "IYH" }, bytes, address, 2),
            0x5D => Success(new[] { "LD", "E", "IYL" }, bytes, address, 2),
            0x60 => Success(new[] { "LD", "IYH", "B" }, bytes, address, 2),
            0x61 => Success(new[] { "LD", "IYH", "C" }, bytes, address, 2),
            0x62 => Success(new[] { "LD", "IYH", "D" }, bytes, address, 2),
            0x63 => Success(new[] { "LD", "IYH", "E" }, bytes, address, 2),
            0x64 => Success(new[] { "LD", "IYH", "IYH" }, bytes, address, 2),
            0x65 => Success(new[] { "LD", "IYH", "IYL" }, bytes, address, 2),
            0x67 => Success(new[] { "LD", "IYH", "A" }, bytes, address, 2),
            0x68 => Success(new[] { "LD", "IYL", "B" }, bytes, address, 2),
            0x69 => Success(new[] { "LD", "IYL", "C" }, bytes, address, 2),
            0x6A => Success(new[] { "LD", "IYL", "D" }, bytes, address, 2),
            0x6B => Success(new[] { "LD", "IYL", "E" }, bytes, address, 2),
            0x6C => Success(new[] { "LD", "IYL", "IYH" }, bytes, address, 2),
            0x6D => Success(new[] { "LD", "IYL", "IYL" }, bytes, address, 2),
            0x6F => Success(new[] { "LD", "IYL", "A" }, bytes, address, 2),
            0x7C => Success(new[] { "LD", "A", "IYH" }, bytes, address, 2),
            0x7D => Success(new[] { "LD", "A", "IYL" }, bytes, address, 2),
            
            // ALU operations with IYH/IYL
            0x84 => Success(new[] { "ADD", "A", "IYH" }, bytes, address, 2),
            0x85 => Success(new[] { "ADD", "A", "IYL" }, bytes, address, 2),
            0x8C => Success(new[] { "ADC", "A", "IYH" }, bytes, address, 2),
            0x8D => Success(new[] { "ADC", "A", "IYL" }, bytes, address, 2),
            0x94 => Success(new[] { "SUB", "A", "IYH" }, bytes, address, 2),
            0x95 => Success(new[] { "SUB", "A", "IYL" }, bytes, address, 2),
            0x9C => Success(new[] { "SBC", "A", "IYH" }, bytes, address, 2),
            0x9D => Success(new[] { "SBC", "A", "IYL" }, bytes, address, 2),
            0xA4 => Success(new[] { "AND", "A", "IYH" }, bytes, address, 2),
            0xA5 => Success(new[] { "AND", "A", "IYL" }, bytes, address, 2),
            0xAC => Success(new[] { "XOR", "A", "IYH" }, bytes, address, 2),
            0xAD => Success(new[] { "XOR", "A", "IYL" }, bytes, address, 2),
            0xB4 => Success(new[] { "OR", "A", "IYH" }, bytes, address, 2),
            0xB5 => Success(new[] { "OR", "A", "IYL" }, bytes, address, 2),
            0xBC => Success(new[] { "CP", "A", "IYH" }, bytes, address, 2),
            0xBD => Success(new[] { "CP", "A", "IYL" }, bytes, address, 2),
            >= 0x46 and <= 0x7E when secondByte != 0x76 => bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                DecodeIYDisplacementLD(bytes, address, secondByte),
                
            // ALU operations with IY+displacement (0x86-0xBE range)
            >= 0x86 and <= 0xBE when (secondByte & 0x07) == 0x06 => bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                DecodeIYDisplacementALU(bytes, address, secondByte),
            
            // IY CB prefix
            0xCB => bytes.Length < 4 ? DecodeResult.NeedMoreBytes(4) : DecodeIYCB(bytes, address),
            
            _ => Success("ILLEGAL", bytes, address, 2)
        };
    }
    
    private DecodeResult DecodeIXCB(ReadOnlySpan<byte> bytes, ulong address)
    {
        var displacement = (sbyte)bytes[2];
        var opcode = bytes[3];
        var operation = GetCBOperation(opcode);
        var target = $"(IX{FormatDisplacement(displacement)})";
        
        if (operation.StartsWith("BIT") || operation.StartsWith("SET") || operation.StartsWith("RES"))
        {
            var bitNum = (opcode >> 3) & 7;
            return Success(new[] { operation.Split(' ')[0], bitNum.ToString(), target }, bytes, address, 4);
        }
        
        return Success(new[] { operation, target }, bytes, address, 4);
    }
    
    private DecodeResult DecodeIYCB(ReadOnlySpan<byte> bytes, ulong address)
    {
        var displacement = (sbyte)bytes[2];
        var opcode = bytes[3];
        var operation = GetCBOperation(opcode);
        var target = $"(IY{FormatDisplacement(displacement)})";
        
        if (operation.StartsWith("BIT") || operation.StartsWith("SET") || operation.StartsWith("RES"))
        {
            var bitNum = (opcode >> 3) & 7;
            return Success(new[] { operation.Split(' ')[0], bitNum.ToString(), target }, bytes, address, 4);
        }
        
        return Success(new[] { operation, target }, bytes, address, 4);
    }
    
    private string GetCBOperation(byte opcode)
    {
        var x = (opcode >> 6) & 3;
        var y = (opcode >> 3) & 7;
        
        return x switch
        {
            0 => y switch { 0 => "RLC", 1 => "RRC", 2 => "RL", 3 => "RR", 4 => "SLA", 5 => "SRA", 6 => "SLL", 7 => "SRL", _ => "???" },
            1 => "BIT",
            2 => "RES", 
            3 => "SET",
            _ => "???"
        };
    }
    
    private string GetIX16Reg(int regIndex)
    {
        return regIndex switch
        {
            0 => "BC", 1 => "DE", 2 => "IX", 3 => "SP",
            _ => "??"
        };
    }
    
    private string GetIY16Reg(int regIndex)
    {
        return regIndex switch
        {
            0 => "BC", 1 => "DE", 2 => "IY", 3 => "SP",
            _ => "??"
        };
    }
    
    private string FormatDisplacement(sbyte displacement)
    {
        if (displacement >= 0)
            return $"+${displacement:X2}";
        else
            return $"-${(-displacement):X2}";
    }

    private UInt16 ReadU16(ReadOnlySpan<byte> bytes, int offset)
    {
        return (UInt16)(bytes[offset] | (bytes[offset + 1] << 8));
    }

    private DecodeResult DecodeIXDisplacementLD(ReadOnlySpan<byte> bytes, ulong address, byte opcode)
    {
        var displacement = (sbyte)bytes[2];
        var indexedOperand = $"(IX{FormatDisplacement(displacement)})";
        
        var srcReg = Registers[opcode & 7];
        var dstReg = Registers[(opcode >> 3) & 7];
        
        if ((opcode & 7) == 6) // Source is (HL) -> (IX+d)
            return Success(new[] { "LD", dstReg, indexedOperand }, bytes, address, 3);
        else if (((opcode >> 3) & 7) == 6) // Dest is (HL) -> (IX+d)
            return Success(new[] { "LD", indexedOperand, srcReg }, bytes, address, 3);
        else
            return Success("ILLEGAL", bytes, address, 2);
    }
    
    private DecodeResult DecodeIYDisplacementLD(ReadOnlySpan<byte> bytes, ulong address, byte opcode)
    {
        var displacement = (sbyte)bytes[2];
        var indexedOperand = $"(IY{FormatDisplacement(displacement)})";
        
        var srcReg = Registers[opcode & 7];
        var dstReg = Registers[(opcode >> 3) & 7];
        
        if ((opcode & 7) == 6) // Source is (HL) -> (IY+d)
            return Success(new[] { "LD", dstReg, indexedOperand }, bytes, address, 3);
        else if (((opcode >> 3) & 7) == 6) // Dest is (HL) -> (IY+d)
            return Success(new[] { "LD", indexedOperand, srcReg }, bytes, address, 3);
        else
            return Success("ILLEGAL", bytes, address, 2);
    }
    
    private DecodeResult DecodeIXDisplacementALU(ReadOnlySpan<byte> bytes, ulong address, byte opcode)
    {
        var displacement = (sbyte)bytes[2];
        var indexedOperand = $"(IX{FormatDisplacement(displacement)})";
        var aluOp = GetAluMnemonic(opcode);
        
        return Success(new[] { aluOp, "A", indexedOperand }, bytes, address, 3);
    }
    
    private DecodeResult DecodeIYDisplacementALU(ReadOnlySpan<byte> bytes, ulong address, byte opcode)
    {
        var displacement = (sbyte)bytes[2];
        var indexedOperand = $"(IY{FormatDisplacement(displacement)})";
        var aluOp = GetAluMnemonic(opcode);
        
        return Success(new[] { aluOp, "A", indexedOperand }, bytes, address, 3);
    }

    private DecodeResult Success(string mnemonic, ReadOnlySpan<byte> bytes, ulong address, int size, bool isBranch = false, bool isTerminator = false)
    {
        var instructionBytes = SliceBytes(bytes, size);
        var instruction = new Instruction(address, mnemonic, new List<IOperand>(), instructionBytes, State.Clone());
        instruction.IsBranch = isBranch;
        instruction.IsBasicBlockTerminator = isTerminator;
        
        // Add next addresses based on instruction type
        AddNextAddresses(instruction, bytes, address, size, isBranch, isTerminator);
        
        return DecodeResult.CreateSuccess(instruction, size);
    }

    private DecodeResult Success(string[] operands, ReadOnlySpan<byte> bytes, ulong address, int size, bool isBranch = false, bool isTerminator = false)
    {
        var ops = new List<IOperand>();
        for (int i = 1; i < operands.Length; i++)
        {
            var operand = operands[i];
            if (operand.StartsWith("#"))
            {
                // Immediate value
                var valueStr = operand[1..];
                if (ulong.TryParse(valueStr, System.Globalization.NumberStyles.HexNumber, null, out var value))
                    ops.Add(new Z80ImmediateOperand(value, valueStr.Length <= 2 ? 1 : 2));
                else
                    ops.Add(new Z80RegisterOperand(operand));
            }
            else if (operand.StartsWith("$"))
            {
                // Address
                var valueStr = operand[1..];
                if (ulong.TryParse(valueStr, System.Globalization.NumberStyles.HexNumber, null, out var value))
                    ops.Add(new Z80AddressOperand(value));
                else
                    ops.Add(new Z80RegisterOperand(operand));
            }
            else if (operand.StartsWith("(") && operand.EndsWith(")"))
            {
                // Indirect or indexed operand
                var inner = operand[1..^1];
                if (inner.Contains("+") || inner.Contains("-"))
                {
                    // Indexed operand like (IX+$10)
                    ops.Add(new Z80RegisterOperand(operand));
                }
                else if (inner.StartsWith("$"))
                {
                    // Address indirect like ($1234)
                    ops.Add(new Z80RegisterOperand(operand));
                }
                else
                {
                    // Register indirect like (HL)
                    ops.Add(new Z80IndirectOperand(inner));
                }
            }
            else if (operand.StartsWith("+") || operand.StartsWith("-"))
            {
                // Displacement
                ops.Add(new Z80DisplacementOperand(operand));
            }
            else
            {
                // Register or literal
                ops.Add(new Z80RegisterOperand(operand));
            }
        }
        
        var instructionBytes = SliceBytes(bytes, size);
        var instruction = new Instruction(address, operands[0], ops, instructionBytes, State.Clone());
        instruction.IsBranch = isBranch;
        instruction.IsBasicBlockTerminator = isTerminator;
        
        // Add next addresses based on instruction type
        AddNextAddresses(instruction, bytes, address, size, isBranch, isTerminator);
        
        return DecodeResult.CreateSuccess(instruction, size);
    }

    private DecodeResult Success(string mnemonic, string op1, string op2, ReadOnlySpan<byte> bytes, ulong address, int size)
    {
        var instructionBytes = SliceBytes(bytes, size);
        var ops = new List<IOperand> { new Z80RegisterOperand(op1), new Z80RegisterOperand(op2) };
        var instruction = new Instruction(address, mnemonic, ops, instructionBytes, State.Clone());
        instruction.NextAddresses.Add(address + (ulong)size);
        return DecodeResult.CreateSuccess(instruction, size);
    }

    private void AddNextAddresses(Instruction instruction, ReadOnlySpan<byte> bytes, ulong address, int size, bool isBranch, bool isTerminator)
    {
        if (!isTerminator)
        {
            // Regular instructions just fall through
            instruction.NextAddresses.Add(address + (ulong)size);
        }
        if (!isBranch)
            return;

        // Handle branch instructions
        var opcode = bytes[0];
        var fallThroughAddress = address + (ulong)size;

        switch (opcode)
        {
            case 0x10: // DJNZ - conditional branch
                {
                    var offset = (sbyte)bytes[1];
                    var targetAddress = (ulong)((long)fallThroughAddress + offset);
                    instruction.NextAddresses.Add(targetAddress);      // Branch target
                    break;
                }
            
            case 0x18: // JR - unconditional relative jump
                {
                    var offset = (sbyte)bytes[1];
                    var targetAddress = (ulong)((long)fallThroughAddress + offset);
                    instruction.NextAddresses.Add(targetAddress); // Only branch target
                    break;
                }
            
            case 0x20 or 0x28 or 0x30 or 0x38: // JR cc - conditional relative jump
                {
                    var offset = (sbyte)bytes[1];
                    var targetAddress = (ulong)((long)fallThroughAddress + offset);
                    instruction.NextAddresses.Add(targetAddress);      // Branch target
                    break;
                }
            
            case 0xC0 or 0xC8 or 0xD0 or 0xD8 or 0xE0 or 0xE8 or 0xF0 or 0xF8: // RET cc - conditional return
                {
                    break;
                }
            
            case 0xC2 or 0xCA or 0xD2 or 0xDA or 0xE2 or 0xEA or 0xF2 or 0xFA: // JP cc - conditional absolute jump
                {
                    var targetAddress = ReadU16(bytes, 1);
                    instruction.NextAddresses.Add(targetAddress);      // Jump target
                    break;
                }
            
            case 0xC4 or 0xCC or 0xD4 or 0xDC or 0xE4 or 0xEC or 0xF4 or 0xFC: // CALL cc - conditional call
                {
                    var targetAddress = ReadU16(bytes, 1);
                    instruction.NextAddresses.Add(targetAddress);      // Call target
                    break;
                }
            
            case 0xC3: // JP - unconditional absolute jump
                {
                    var targetAddress = ReadU16(bytes, 1);
                    instruction.NextAddresses.Add(targetAddress); // Only jump target
                    break;
                }
            
            case 0xCD: // CALL - unconditional call
                {
                    var targetAddress = ReadU16(bytes, 1);
                    instruction.NextAddresses.Add(targetAddress); // Only call target
                    break;
                }
            
            case 0xC7 or 0xCF or 0xD7 or 0xDF or 0xE7 or 0xEF or 0xF7 or 0xFF: // RST - restart
                {
                    var vector = ((opcode >> 3) & 7) * 8;
                    instruction.NextAddresses.Add((ulong)vector); // Only RST vector target
                    break;
                }
            
            default:
                {
                    // For DD/FD prefix instructions
                    if (opcode == 0xDD || opcode == 0xFD)
                    {
                        if (bytes.Length >= 2)
                        {
                            var secondByte = bytes[1];
                            // Handle JP (IX) and JP (IY)
                            if (secondByte == 0xE9)
                            {
                                // JP (IX/IY) - indirect jump, can't determine target
                                break;
                            }
                            // For other DD/FD instructions, add fall-through if not already added
                        }
                        break;
                    }
                    
                    // For other branch instructions (like RETN/RETI in ED prefix), 
                    // fall back to fall-through if we don't handle them specifically
                    if (bytes[0] == 0xED && bytes.Length >= 2)
                    {
                        var secondByte = bytes[1];
                        if (secondByte == 0x45 || secondByte == 0x4D) // RETN/RETI
                        {
                            // These are returns, no next addresses (handled as terminators)
                            break;
                        }
                    }
                    break;
                }
        }
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
