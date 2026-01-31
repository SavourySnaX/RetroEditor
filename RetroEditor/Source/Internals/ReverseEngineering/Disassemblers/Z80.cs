using System.Text.Json;
using RetroEditor.Plugins;
using RetroEditor.Source.Internals.ReverseEngineering.Platform;

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

    /// <summary>
    /// Converts a register name string to Z80Register enum value
    /// </summary>
    // Helper methods for creating common operand types
    private static Z80SpecialRegisterOperand SR(string name) => new Z80SpecialRegisterOperand(name);
    private static Z80RegisterOperand R(Z80Register reg) => new Z80RegisterOperand(reg);
    private static Z80Register16Operand R16(Z80Register16 reg) => new Z80Register16Operand(reg);
    private static Z80ImmediateOperand Imm(ulong value, int size = 1) => new Z80ImmediateOperand(value, size);
    private static Z80AddressOperand Addr(ulong address) => new Z80AddressOperand(address);
    private static Z80IndirectAddressOperand IndAddr(ulong address) => new Z80IndirectAddressOperand(address);
    private static Z80IndirectOperand Ind(Z80Register16 reg) => new Z80IndirectOperand(reg);
    private static Z80IndexedOperand Idx(Z80IndexRegister reg, sbyte disp) => new Z80IndexedOperand(reg, disp);

    public override DecodeResult DecodeNext(ReadOnlySpan<byte> bytes, ulong address)
    {
        if (bytes.Length < 1)
            return DecodeResult.NeedMoreBytes(1);

        var opcode = bytes[0];

        return opcode switch
        {
            // Single-byte instructions
            0x00 => Success("NOP", bytes, address, 1),
            0x02 => Success("LD", new List<IOperand> { new Z80IndirectOperand(Z80Register16.BC), new Z80RegisterOperand(Z80Register.A) }, bytes, address, 1),
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
                Success("INC", (Z80Register)((opcode >> 3) & 7), bytes, address, 1),
            
            // 8-bit DEC
            0x05 or 0x0D or 0x15 or 0x1D or 0x25 or 0x2D or 0x35 or 0x3D =>
                Success("DEC", (Z80Register)((opcode >> 3) & 7), bytes, address, 1),

            // 8-bit register load immediate  
            0x06 or 0x0E or 0x16 or 0x1E or 0x26 or 0x2E or 0x36 or 0x3E => 
                bytes.Length < 2 ? DecodeResult.NeedMoreBytes(2) :
                Success("LD", (Z80Register)((opcode >> 3) & 7), (ulong)bytes[1], bytes, address, 2),

            // 16-bit register load immediate
            0x01 or 0x11 or 0x21 or 0x31 =>
                bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success("LD", (Z80Register16)(opcode >> 4), ReadU16(bytes, 1), bytes, address, 3),
                
            // 16-bit HL memory operations
            0x22 => bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success("LD", new List<IOperand> { new Z80IndirectAddressOperand(ReadU16(bytes, 1)), new Z80Register16Operand(Z80Register16.HL) }, bytes, address, 3),
            0x2A => bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success("LD", new List<IOperand> { new Z80Register16Operand(Z80Register16.HL), new Z80IndirectAddressOperand(ReadU16(bytes, 1)) }, bytes, address, 3),
                
            // 8-bit A memory operations
            0x32 => bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success("LD", new List<IOperand> { new Z80IndirectAddressOperand(ReadU16(bytes, 1)), new Z80RegisterOperand(Z80Register.A) }, bytes, address, 3),
            0x3A => bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success("LD", new List<IOperand> { new Z80RegisterOperand(Z80Register.A), new Z80IndirectAddressOperand(ReadU16(bytes, 1)) }, bytes, address, 3),

            // Single byte combos
            0x09 => Success("ADD", new List<IOperand> { R16(Z80Register16.HL), R16(Z80Register16.BC) }, bytes, address, 1),
            0x19 => Success("ADD", new List<IOperand> { R16(Z80Register16.HL), R16(Z80Register16.DE) }, bytes, address, 1),
            0x29 => Success("ADD", new List<IOperand> { R16(Z80Register16.HL), R16(Z80Register16.HL) }, bytes, address, 1),
            0x39 => Success("ADD", new List<IOperand> { R16(Z80Register16.HL), R16(Z80Register16.SP) }, bytes, address, 1),

            // 16-bit INC/DEC
            0x03 => Success("INC", new List<IOperand> { R16(Z80Register16.BC) }, bytes, address, 1),
            0x13 => Success("INC", new List<IOperand> { R16(Z80Register16.DE) }, bytes, address, 1),
            0x23 => Success("INC", new List<IOperand> { R16(Z80Register16.HL) }, bytes, address, 1),
            0x33 => Success("INC", new List<IOperand> { R16(Z80Register16.SP) }, bytes, address, 1),
            0x0B => Success("DEC", new List<IOperand> { R16(Z80Register16.BC) }, bytes, address, 1),
            0x1B => Success("DEC", new List<IOperand> { R16(Z80Register16.DE) }, bytes, address, 1),
            0x2B => Success("DEC", new List<IOperand> { R16(Z80Register16.HL) }, bytes, address, 1),
            0x3B => Success("DEC", new List<IOperand> { R16(Z80Register16.SP) }, bytes, address, 1),
            
            0x0A => Success("LD", new List<IOperand> { new Z80RegisterOperand(Z80Register.A), new Z80IndirectOperand(Z80Register16.BC) }, bytes, address, 1),
            0x12 => Success("LD", new List<IOperand> { new Z80IndirectOperand(Z80Register16.DE), new Z80RegisterOperand(Z80Register.A) }, bytes, address, 1),
            0x1A => Success("LD", new List<IOperand> { new Z80RegisterOperand(Z80Register.A), new Z80IndirectOperand(Z80Register16.DE) }, bytes, address, 1),

            // Special control
            0x08 => Success("EX", new List<IOperand> { new Z80Register16Operand(Z80Register16.BC), new Z80SpecialRegisterOperand("AF'") }, bytes, address, 1),
            0xD9 => Success("EXX", bytes, address, 1),
            0xE3 => Success("EX", new List<IOperand> { new Z80SpecialRegisterOperand("(SP)"), new Z80Register16Operand(Z80Register16.HL) }, bytes, address, 1),
            0xEB => Success("EX", new List<IOperand> { new Z80Register16Operand(Z80Register16.DE), new Z80Register16Operand(Z80Register16.HL) }, bytes, address, 1),
            
            // DJNZ - branch with displacement
            0x10 => bytes.Length < 2 ? DecodeResult.NeedMoreBytes(2) :
                Success("DJNZ", new List<IOperand> { new Z80RelativeOperand(address + 2 + (ulong)(long)(sbyte)bytes[1]) }, bytes, address, 2, isBranch: true),
            
            // JR - unconditional branch
            0x18 => bytes.Length < 2 ? DecodeResult.NeedMoreBytes(2) :
                Success("JR", new List<IOperand> { new Z80RelativeOperand(address + 2 + (ulong)(long)(sbyte)bytes[1]) }, bytes, address, 2, isBranch: true),
            
            // JR cc - conditional branch
            0x20 or 0x28 or 0x30 or 0x38 => 
                bytes.Length < 2 ? DecodeResult.NeedMoreBytes(2) :
                Success("JR", new List<IOperand> { new Z80ConditionOperand((opcode >> 3) & 3), new Z80RelativeOperand(address + 2 + (ulong)(long)(sbyte)bytes[1]) }, bytes, address, 2, isBranch: true),
            
            // Register-to-register LD
            >= 0x40 and <= 0x7F when opcode != 0x76 =>
                Success("LD", (Z80Register)((opcode >> 3) & 7), (Z80Register)(opcode & 7), bytes, address, 1),
            
            // 8-bit ALU operations (ADD, ADC, SUB, SBC, AND, XOR, OR, CP)
            >= 0x80 and <= 0xBF =>
                Success(GetAluMnemonic(opcode), new List<IOperand> { new Z80RegisterOperand(Z80Register.A), new Z80RegisterOperand((Z80Register)(opcode & 7)) }, bytes, address, 1),

            // ALU with immediate
            0xC6 or 0xCE or 0xD6 or 0xDE or 0xE6 or 0xEE or 0xF6 or 0xFE =>
                bytes.Length < 2 ? DecodeResult.NeedMoreBytes(2) :
                Success(GetAluMnemonic(opcode), new List<IOperand> { new Z80RegisterOperand(Z80Register.A), new Z80ImmediateOperand(bytes[1], 1) }, bytes, address, 2),

            // Return
            0xC0 or 0xC8 or 0xD0 or 0xD8 or 0xE0 or 0xE8 or 0xF0 or 0xF8 =>
                Success("RET", new List<IOperand> { new Z80ConditionOperand((opcode >> 3) & 7) }, bytes, address, 1, isBranch: true),
            0xC9 => Success("RET", bytes, address, 1, isTerminator: true),

            // Jump and call - conditional
            0xC2 or 0xCA or 0xD2 or 0xDA or 0xE2 or 0xEA or 0xF2 or 0xFA =>
                bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success("JP", new List<IOperand> { new Z80ConditionOperand((opcode >> 3) & 7), new Z80AddressOperand(ReadU16(bytes, 1)) }, bytes, address, 3, isBranch: true),
            0xC4 or 0xCC or 0xD4 or 0xDC or 0xE4 or 0xEC or 0xF4 or 0xFC =>
                bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success("CALL", new List<IOperand> { new Z80ConditionOperand((opcode >> 3) & 7), new Z80AddressOperand(ReadU16(bytes, 1)) }, bytes, address, 3, isBranch: true),

            // Jump and call - unconditional
            0xC3 => bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success("JP", new List<IOperand> { new Z80AddressOperand(ReadU16(bytes, 1)) }, bytes, address, 3, isBranch: true, isTerminator: true),
            0xCD => bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success("CALL", new List<IOperand> { new Z80AddressOperand(ReadU16(bytes, 1)) }, bytes, address, 3, isBranch: true),

            // Push/Pop
            0xC1 or 0xD1 or 0xE1 or 0xF1 =>
                Success("POP", (Z80Register16)((opcode >> 4) & 3), bytes, address, 1),
            0xC5 or 0xD5 or 0xE5 or 0xF5 =>
                Success("PUSH", (Z80Register16)((opcode >> 4) & 3), bytes, address, 1),

            // RST - Restart
            0xC7 or 0xCF or 0xD7 or 0xDF or 0xE7 or 0xEF or 0xF7 or 0xFF =>
                Success("RST", new List<IOperand> { new Z80AddressOperand((ulong)(((opcode >> 3) & 7) * 8)) }, bytes, address, 1, isBranch: true, isTerminator: true),

            // I/O
            0xDB => bytes.Length < 2 ? DecodeResult.NeedMoreBytes(2) :
                Success("IN", new List<IOperand> { new Z80RegisterOperand(Z80Register.A), new Z80ImmediateOperand(bytes[1], 1) }, bytes, address, 2),
            0xD3 => bytes.Length < 2 ? DecodeResult.NeedMoreBytes(2) :
                Success("OUT", new List<IOperand> { new Z80ImmediateOperand(bytes[1], 1), new Z80RegisterOperand(Z80Register.A) }, bytes, address, 2),

            // Control
            0xF3 => Success("DI", bytes, address, 1),
            0xF9 => Success("LD", new List<IOperand> { new Z80Register16Operand(Z80Register16.SP), new Z80Register16Operand(Z80Register16.HL) }, bytes, address, 1),
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

        var mnemonic = mnemonics[x][y];
        var targetReg = R((Z80Register)z);
        
        if (x == 1 || x == 2 || x == 3)
            return Success(mnemonic, new List<IOperand> { new Z80LiteralOperand((ulong)y), targetReg }, bytes, address, 2);
        
        return Success(mnemonic, new List<IOperand> { targetReg }, bytes, address, 2);
    }

    private DecodeResult DecodeED(ReadOnlySpan<byte> bytes, ulong address)
    {
        var secondByte = bytes[1];
        
        return secondByte switch
        {
            // Single byte ED instructions
            0x44 => Success("NEG", bytes, address, 2),
            0x45 => Success("RETN", bytes, address, 2, isBranch: true, isTerminator: true),
            0x46 => Success("IM", new List<IOperand> { new Z80LiteralOperand(0) }, bytes, address, 2),
            0x47 => Success("LD", new List<IOperand> { SR("I"), R(Z80Register.A) }, bytes, address, 2),
            0x4D => Success("RETI", bytes, address, 2, isBranch: true, isTerminator: true),
            0x4F => Success("LD", new List<IOperand> { SR("R"), R(Z80Register.A) }, bytes, address, 2),
            0x56 => Success("IM", new List<IOperand> { new Z80LiteralOperand(1) }, bytes, address, 2),
            0x57 => Success("LD", new List<IOperand> { R(Z80Register.A), SR("I") }, bytes, address, 2),
            0x5E => Success("IM", new List<IOperand> { new Z80LiteralOperand(2) }, bytes, address, 2),
            0x5F => Success("LD", new List<IOperand> { R(Z80Register.A), SR("R") }, bytes, address, 2),
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
            
            0x43 => bytes.Length < 4 ? DecodeResult.NeedMoreBytes(4) :
                Success("LD", new List<IOperand> { IndAddr(ReadU16(bytes, 2)), R16(Z80Register16.BC) }, bytes, address, 4),
            0x53 => bytes.Length < 4 ? DecodeResult.NeedMoreBytes(4) :
                Success("LD", new List<IOperand> { IndAddr(ReadU16(bytes, 2)), R16(Z80Register16.DE) }, bytes, address, 4),
            0x63 => bytes.Length < 4 ? DecodeResult.NeedMoreBytes(4) :
                Success("LD", new List<IOperand> { IndAddr(ReadU16(bytes, 2)), R16(Z80Register16.HL) }, bytes, address, 4),
            0x73 => bytes.Length < 4 ? DecodeResult.NeedMoreBytes(4) :
                Success("LD", new List<IOperand> { IndAddr(ReadU16(bytes, 2)), R16(Z80Register16.SP) }, bytes, address, 4),
            0x4B => bytes.Length < 4 ? DecodeResult.NeedMoreBytes(4) :
                Success("LD", new List<IOperand> { R16(Z80Register16.BC), IndAddr(ReadU16(bytes, 2)) }, bytes, address, 4),
            0x5B => bytes.Length < 4 ? DecodeResult.NeedMoreBytes(4) :
                Success("LD", new List<IOperand> { R16(Z80Register16.DE), IndAddr(ReadU16(bytes, 2)) }, bytes, address, 4),
            0x6B => bytes.Length < 4 ? DecodeResult.NeedMoreBytes(4) :
                Success("LD", new List<IOperand> { R16(Z80Register16.HL), IndAddr(ReadU16(bytes, 2)) }, bytes, address, 4),
            0x7B => bytes.Length < 4 ? DecodeResult.NeedMoreBytes(4) :
                Success("LD", new List<IOperand> { R16(Z80Register16.SP), IndAddr(ReadU16(bytes, 2)) }, bytes, address, 4),
            
            0x42 => Success("SBC", new List<IOperand> { R16(Z80Register16.HL), R16(Z80Register16.BC) }, bytes, address, 2),
            0x52 => Success("SBC", new List<IOperand> { R16(Z80Register16.HL), R16(Z80Register16.DE) }, bytes, address, 2),
            0x62 => Success("SBC", new List<IOperand> { R16(Z80Register16.HL), R16(Z80Register16.HL) }, bytes, address, 2),
            0x72 => Success("SBC", new List<IOperand> { R16(Z80Register16.HL), R16(Z80Register16.SP) }, bytes, address, 2),
            0x4A => Success("ADC", new List<IOperand> { R16(Z80Register16.HL), R16(Z80Register16.BC) }, bytes, address, 2),
            0x5A => Success("ADC", new List<IOperand> { R16(Z80Register16.HL), R16(Z80Register16.DE) }, bytes, address, 2),
            0x6A => Success("ADC", new List<IOperand> { R16(Z80Register16.HL), R16(Z80Register16.HL) }, bytes, address, 2),
            0x7A => Success("ADC", new List<IOperand> { R16(Z80Register16.HL), R16(Z80Register16.SP) }, bytes, address, 2),
            
            0x40 => Success("IN", new List<IOperand> { R(Z80Register.B), SR("(C)") }, bytes, address, 2),
            0x48 => Success("IN", new List<IOperand> { R(Z80Register.C), SR("(C)") }, bytes, address, 2),
            0x50 => Success("IN", new List<IOperand> { R(Z80Register.D), SR("(C)") }, bytes, address, 2),
            0x58 => Success("IN", new List<IOperand> { R(Z80Register.E), SR("(C)") }, bytes, address, 2),
            0x60 => Success("IN", new List<IOperand> { R(Z80Register.H), SR("(C)") }, bytes, address, 2),
            0x68 => Success("IN", new List<IOperand> { R(Z80Register.L), SR("(C)") }, bytes, address, 2),
            0x78 => Success("IN", new List<IOperand> { R(Z80Register.A), SR("(C)") }, bytes, address, 2),
            0x41 => Success("OUT", new List<IOperand> { SR("(C)"), R(Z80Register.B) }, bytes, address, 2),
            0x49 => Success("OUT", new List<IOperand> { SR("(C)"), R(Z80Register.C) }, bytes, address, 2),
            0x51 => Success("OUT", new List<IOperand> { SR("(C)"), R(Z80Register.D) }, bytes, address, 2),
            0x59 => Success("OUT", new List<IOperand> { SR("(C)"), R(Z80Register.E) }, bytes, address, 2),
            0x61 => Success("OUT", new List<IOperand> { SR("(C)"), R(Z80Register.H) }, bytes, address, 2),
            0x69 => Success("OUT", new List<IOperand> { SR("(C)"), R(Z80Register.L) }, bytes, address, 2),
            0x79 => Success("OUT", new List<IOperand> { SR("(C)"), R(Z80Register.A) }, bytes, address, 2),
            
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
    
    private DecodeResult DecodeDD(ReadOnlySpan<byte> bytes, ulong address)
    {
        var secondByte = bytes[1];
        
        return secondByte switch
        {
            // IX register operations
            0x09 => Success("ADD", new List<IOperand> { SR("IX"), R16(Z80Register16.BC) }, bytes, address, 2),
            0x19 => Success("ADD", new List<IOperand> { SR("IX"), R16(Z80Register16.DE) }, bytes, address, 2),
            0x29 => Success("ADD", new List<IOperand> { SR("IX"), SR("IX") }, bytes, address, 2),
            0x39 => Success("ADD", new List<IOperand> { SR("IX"), R16(Z80Register16.SP) }, bytes, address, 2),
            0x21 => bytes.Length < 4 ? DecodeResult.NeedMoreBytes(4) :
                Success("LD", new List<IOperand> { SR("IX"), Imm(ReadU16(bytes, 2), 2) }, bytes, address, 4),
            0x22 => bytes.Length < 4 ? DecodeResult.NeedMoreBytes(4) :
                Success("LD", new List<IOperand> { IndAddr(ReadU16(bytes, 2)), SR("IX") }, bytes, address, 4),
            0x23 => Success("INC", new List<IOperand> { SR("IX") }, bytes, address, 2),
            0x24 => Success("INC", new List<IOperand> { SR("IXH") }, bytes, address, 2),
            0x25 => Success("DEC", new List<IOperand> { SR("IXH") }, bytes, address, 2),
            0x26 => bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success("LD", new List<IOperand> { SR("IXH"), Imm(bytes[2]) }, bytes, address, 3),
            0x2A => bytes.Length < 4 ? DecodeResult.NeedMoreBytes(4) :
                Success("LD", new List<IOperand> { SR("IX"), IndAddr(ReadU16(bytes, 2)) }, bytes, address, 4),
            0x2B => Success("DEC", new List<IOperand> { SR("IX") }, bytes, address, 2),
            0x2C => Success("INC", new List<IOperand> { SR("IXL") }, bytes, address, 2),
            0x2D => Success("DEC", new List<IOperand> { SR("IXL") }, bytes, address, 2),
            0x2E => bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success("LD", new List<IOperand> { SR("IXL"), Imm(bytes[2]) }, bytes, address, 3),
            0xE1 => Success("POP", new List<IOperand> { SR("IX") }, bytes, address, 2),
            0xE3 => Success("EX", new List<IOperand> { SR("(SP)"), SR("IX") }, bytes, address, 2),
            0xE5 => Success("PUSH", new List<IOperand> { SR("IX") }, bytes, address, 2),
            0xE9 => Success("JP", new List<IOperand> { SR("(IX)") }, bytes, address, 2, isBranch: true, isTerminator: true),
            0xF9 => Success("LD", new List<IOperand> { R16(Z80Register16.SP), SR("IX") }, bytes, address, 2),
            
            // IX+displacement operations
            0x34 or 0x35 => bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success(secondByte == 0x34 ? "INC" : "DEC", new List<IOperand> { Idx(Z80IndexRegister.IX, (sbyte)bytes[2]) }, bytes, address, 3),
            0x36 => bytes.Length < 4 ? DecodeResult.NeedMoreBytes(4) :
                Success("LD", new List<IOperand> { Idx(Z80IndexRegister.IX, (sbyte)bytes[2]), Imm(bytes[3]) }, bytes, address, 4),
            
            // LD operations with IXH/IXL
            0x44 => Success("LD", new List<IOperand> { R(Z80Register.B), SR("IXH") }, bytes, address, 2),
            0x45 => Success("LD", new List<IOperand> { R(Z80Register.B), SR("IXL") }, bytes, address, 2),
            0x4C => Success("LD", new List<IOperand> { R(Z80Register.C), SR("IXH") }, bytes, address, 2),
            0x4D => Success("LD", new List<IOperand> { R(Z80Register.C), SR("IXL") }, bytes, address, 2),
            0x54 => Success("LD", new List<IOperand> { R(Z80Register.D), SR("IXH") }, bytes, address, 2),
            0x55 => Success("LD", new List<IOperand> { R(Z80Register.D), SR("IXL") }, bytes, address, 2),
            0x5C => Success("LD", new List<IOperand> { R(Z80Register.E), SR("IXH") }, bytes, address, 2),
            0x5D => Success("LD", new List<IOperand> { R(Z80Register.E), SR("IXL") }, bytes, address, 2),
            0x60 => Success("LD", new List<IOperand> { SR("IXH"), R(Z80Register.B) }, bytes, address, 2),
            0x61 => Success("LD", new List<IOperand> { SR("IXH"), R(Z80Register.C) }, bytes, address, 2),
            0x62 => Success("LD", new List<IOperand> { SR("IXH"), R(Z80Register.D) }, bytes, address, 2),
            0x63 => Success("LD", new List<IOperand> { SR("IXH"), R(Z80Register.E) }, bytes, address, 2),
            0x64 => Success("LD", new List<IOperand> { SR("IXH"), SR("IXH") }, bytes, address, 2),
            0x65 => Success("LD", new List<IOperand> { SR("IXH"), SR("IXL") }, bytes, address, 2),
            0x67 => Success("LD", new List<IOperand> { SR("IXH"), R(Z80Register.A) }, bytes, address, 2),
            0x68 => Success("LD", new List<IOperand> { SR("IXL"), R(Z80Register.B) }, bytes, address, 2),
            0x69 => Success("LD", new List<IOperand> { SR("IXL"), R(Z80Register.C) }, bytes, address, 2),
            0x6A => Success("LD", new List<IOperand> { SR("IXL"), R(Z80Register.D) }, bytes, address, 2),
            0x6B => Success("LD", new List<IOperand> { SR("IXL"), R(Z80Register.E) }, bytes, address, 2),
            0x6C => Success("LD", new List<IOperand> { SR("IXL"), SR("IXH") }, bytes, address, 2),
            0x6D => Success("LD", new List<IOperand> { SR("IXL"), SR("IXL") }, bytes, address, 2),
            0x6F => Success("LD", new List<IOperand> { SR("IXL"), R(Z80Register.A) }, bytes, address, 2),
            0x7C => Success("LD", new List<IOperand> { R(Z80Register.A), SR("IXH") }, bytes, address, 2),
            0x7D => Success("LD", new List<IOperand> { R(Z80Register.A), SR("IXL") }, bytes, address, 2),
            
            // ALU operations with IXH/IXL
            0x84 => Success("ADD", new List<IOperand> { R(Z80Register.A), SR("IXH") }, bytes, address, 2),
            0x85 => Success("ADD", new List<IOperand> { R(Z80Register.A), SR("IXL") }, bytes, address, 2),
            0x8C => Success("ADC", new List<IOperand> { R(Z80Register.A), SR("IXH") }, bytes, address, 2),
            0x8D => Success("ADC", new List<IOperand> { R(Z80Register.A), SR("IXL") }, bytes, address, 2),
            0x94 => Success("SUB", new List<IOperand> { R(Z80Register.A), SR("IXH") }, bytes, address, 2),
            0x95 => Success("SUB", new List<IOperand> { R(Z80Register.A), SR("IXL") }, bytes, address, 2),
            0x9C => Success("SBC", new List<IOperand> { R(Z80Register.A), SR("IXH") }, bytes, address, 2),
            0x9D => Success("SBC", new List<IOperand> { R(Z80Register.A), SR("IXL") }, bytes, address, 2),
            0xA4 => Success("AND", new List<IOperand> { R(Z80Register.A), SR("IXH") }, bytes, address, 2),
            0xA5 => Success("AND", new List<IOperand> { R(Z80Register.A), SR("IXL") }, bytes, address, 2),
            0xAC => Success("XOR", new List<IOperand> { R(Z80Register.A), SR("IXH") }, bytes, address, 2),
            0xAD => Success("XOR", new List<IOperand> { R(Z80Register.A), SR("IXL") }, bytes, address, 2),
            0xB4 => Success("OR", new List<IOperand> { R(Z80Register.A), SR("IXH") }, bytes, address, 2),
            0xB5 => Success("OR", new List<IOperand> { R(Z80Register.A), SR("IXL") }, bytes, address, 2),
            0xBC => Success("CP", new List<IOperand> { R(Z80Register.A), SR("IXH") }, bytes, address, 2),
            0xBD => Success("CP", new List<IOperand> { R(Z80Register.A), SR("IXL") }, bytes, address, 2),
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
            0x09 => Success("ADD", new List<IOperand> { SR("IY"), R16(Z80Register16.BC) }, bytes, address, 2),
            0x19 => Success("ADD", new List<IOperand> { SR("IY"), R16(Z80Register16.DE) }, bytes, address, 2),
            0x29 => Success("ADD", new List<IOperand> { SR("IY"), SR("IY") }, bytes, address, 2),
            0x39 => Success("ADD", new List<IOperand> { SR("IY"), R16(Z80Register16.SP) }, bytes, address, 2),
            0x21 => bytes.Length < 4 ? DecodeResult.NeedMoreBytes(4) :
                Success("LD", new List<IOperand> { SR("IY"), Imm(ReadU16(bytes, 2), 2) }, bytes, address, 4),
            0x22 => bytes.Length < 4 ? DecodeResult.NeedMoreBytes(4) :
                Success("LD", new List<IOperand> { IndAddr(ReadU16(bytes, 2)), SR("IY") }, bytes, address, 4),
            0x23 => Success("INC", new List<IOperand> { SR("IY") }, bytes, address, 2),
            0x24 => Success("INC", new List<IOperand> { SR("IYH") }, bytes, address, 2),
            0x25 => Success("DEC", new List<IOperand> { SR("IYH") }, bytes, address, 2),
            0x26 => bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success("LD", new List<IOperand> { SR("IYH"), Imm(bytes[2], 1) }, bytes, address, 3),
            0x2A => bytes.Length < 4 ? DecodeResult.NeedMoreBytes(4) :
                Success("LD", new List<IOperand> { SR("IY"), IndAddr(ReadU16(bytes, 2)) }, bytes, address, 4),
            0x2B => Success("DEC", new List<IOperand> { SR("IY") }, bytes, address, 2),
            0x2C => Success("INC", new List<IOperand> { SR("IYL") }, bytes, address, 2),
            0x2D => Success("DEC", new List<IOperand> { SR("IYL") }, bytes, address, 2),
            0x2E => bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success("LD", new List<IOperand> { SR("IYL"), Imm(bytes[2], 1) }, bytes, address, 3),
            0xE1 => Success("POP", new List<IOperand> { SR("IY") }, bytes, address, 2),
            0xE3 => Success("EX", new List<IOperand> { SR("(SP)"), SR("IY") }, bytes, address, 2),
            0xE5 => Success("PUSH", new List<IOperand> { SR("IY") }, bytes, address, 2),
            0xE9 => Success("JP", new List<IOperand> { SR("(IY)") }, bytes, address, 2, isBranch: true, isTerminator: true),
            0xF9 => Success("LD", new List<IOperand> { R16(Z80Register16.SP), SR("IY") }, bytes, address, 2),
            
            // IY+displacement operations
            0x34 => bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success("INC", new List<IOperand> { Idx(Z80IndexRegister.IY, (sbyte)bytes[2]) }, bytes, address, 3),
            0x35 => bytes.Length < 3 ? DecodeResult.NeedMoreBytes(3) :
                Success("DEC", new List<IOperand> { Idx(Z80IndexRegister.IY, (sbyte)bytes[2]) }, bytes, address, 3),
            0x36 => bytes.Length < 4 ? DecodeResult.NeedMoreBytes(4) :
                Success("LD", new List<IOperand> { Idx(Z80IndexRegister.IY, (sbyte)bytes[2]), Imm(bytes[3], 1) }, bytes, address, 4),
            
            // LD operations with IYH/IYL
            0x44 => Success("LD", new List<IOperand> { R(Z80Register.B), SR("IYH") }, bytes, address, 2),
            0x45 => Success("LD", new List<IOperand> { R(Z80Register.B), SR("IYL") }, bytes, address, 2),
            0x4C => Success("LD", new List<IOperand> { R(Z80Register.C), SR("IYH") }, bytes, address, 2),
            0x4D => Success("LD", new List<IOperand> { R(Z80Register.C), SR("IYL") }, bytes, address, 2),
            0x54 => Success("LD", new List<IOperand> { R(Z80Register.D), SR("IYH") }, bytes, address, 2),
            0x55 => Success("LD", new List<IOperand> { R(Z80Register.D), SR("IYL") }, bytes, address, 2),
            0x5C => Success("LD", new List<IOperand> { R(Z80Register.E), SR("IYH") }, bytes, address, 2),
            0x5D => Success("LD", new List<IOperand> { R(Z80Register.E), SR("IYL") }, bytes, address, 2),
            0x60 => Success("LD", new List<IOperand> { SR("IYH"), R(Z80Register.B) }, bytes, address, 2),
            0x61 => Success("LD", new List<IOperand> { SR("IYH"), R(Z80Register.C) }, bytes, address, 2),
            0x62 => Success("LD", new List<IOperand> { SR("IYH"), R(Z80Register.D) }, bytes, address, 2),
            0x63 => Success("LD", new List<IOperand> { SR("IYH"), R(Z80Register.E) }, bytes, address, 2),
            0x64 => Success("LD", new List<IOperand> { SR("IYH"), SR("IYH") }, bytes, address, 2),
            0x65 => Success("LD", new List<IOperand> { SR("IYH"), SR("IYL") }, bytes, address, 2),
            0x67 => Success("LD", new List<IOperand> { SR("IYH"), R(Z80Register.A) }, bytes, address, 2),
            0x68 => Success("LD", new List<IOperand> { SR("IYL"), R(Z80Register.B) }, bytes, address, 2),
            0x69 => Success("LD", new List<IOperand> { SR("IYL"), R(Z80Register.C) }, bytes, address, 2),
            0x6A => Success("LD", new List<IOperand> { SR("IYL"), R(Z80Register.D) }, bytes, address, 2),
            0x6B => Success("LD", new List<IOperand> { SR("IYL"), R(Z80Register.E) }, bytes, address, 2),
            0x6C => Success("LD", new List<IOperand> { SR("IYL"), SR("IYH") }, bytes, address, 2),
            0x6D => Success("LD", new List<IOperand> { SR("IYL"), SR("IYL") }, bytes, address, 2),
            0x6F => Success("LD", new List<IOperand> { SR("IYL"), R(Z80Register.A) }, bytes, address, 2),
            0x7C => Success("LD", new List<IOperand> { R(Z80Register.A), SR("IYH") }, bytes, address, 2),
            0x7D => Success("LD", new List<IOperand> { R(Z80Register.A), SR("IYL") }, bytes, address, 2),
            
            // ALU operations with IYH/IYL
            0x84 => Success("ADD", new List<IOperand> { R(Z80Register.A), SR("IYH") }, bytes, address, 2),
            0x85 => Success("ADD", new List<IOperand> { R(Z80Register.A), SR("IYL") }, bytes, address, 2),
            0x8C => Success("ADC", new List<IOperand> { R(Z80Register.A), SR("IYH") }, bytes, address, 2),
            0x8D => Success("ADC", new List<IOperand> { R(Z80Register.A), SR("IYL") }, bytes, address, 2),
            0x94 => Success("SUB", new List<IOperand> { R(Z80Register.A), SR("IYH") }, bytes, address, 2),
            0x95 => Success("SUB", new List<IOperand> { R(Z80Register.A), SR("IYL") }, bytes, address, 2),
            0x9C => Success("SBC", new List<IOperand> { R(Z80Register.A), SR("IYH") }, bytes, address, 2),
            0x9D => Success("SBC", new List<IOperand> { R(Z80Register.A), SR("IYL") }, bytes, address, 2),
            0xA4 => Success("AND", new List<IOperand> { R(Z80Register.A), SR("IYH") }, bytes, address, 2),
            0xA5 => Success("AND", new List<IOperand> { R(Z80Register.A), SR("IYL") }, bytes, address, 2),
            0xAC => Success("XOR", new List<IOperand> { R(Z80Register.A), SR("IYH") }, bytes, address, 2),
            0xAD => Success("XOR", new List<IOperand> { R(Z80Register.A), SR("IYL") }, bytes, address, 2),
            0xB4 => Success("OR", new List<IOperand> { R(Z80Register.A), SR("IYH") }, bytes, address, 2),
            0xB5 => Success("OR", new List<IOperand> { R(Z80Register.A), SR("IYL") }, bytes, address, 2),
            0xBC => Success("CP", new List<IOperand> { R(Z80Register.A), SR("IYH") }, bytes, address, 2),
            0xBD => Success("CP", new List<IOperand> { R(Z80Register.A), SR("IYL") }, bytes, address, 2),
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
        var target = Idx(Z80IndexRegister.IX, displacement);
        
        if (operation == "BIT" || operation == "SET" || operation == "RES")
        {
            var bitNum = (opcode >> 3) & 7;
            return Success(operation, new List<IOperand> { new Z80LiteralOperand((ulong)bitNum), target }, bytes, address, 4);
        }
        
        return Success(operation, new List<IOperand> { target }, bytes, address, 4);
    }
    
    private DecodeResult DecodeIYCB(ReadOnlySpan<byte> bytes, ulong address)
    {
        var displacement = (sbyte)bytes[2];
        var opcode = bytes[3];
        var operation = GetCBOperation(opcode);
        var target = Idx(Z80IndexRegister.IY, displacement);
        
        if (operation == "BIT" || operation == "SET" || operation == "RES")
        {
            var bitNum = (opcode >> 3) & 7;
            return Success(operation, new List<IOperand> { new Z80LiteralOperand((ulong)bitNum), target }, bytes, address, 4);
        }
        
        return Success(operation, new List<IOperand> { target }, bytes, address, 4);
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

    private UInt16 ReadU16(ReadOnlySpan<byte> bytes, int offset)
    {
        return (UInt16)(bytes[offset] | (bytes[offset + 1] << 8));
    }

    private DecodeResult DecodeIXDisplacementLD(ReadOnlySpan<byte> bytes, ulong address, byte opcode)
    {
        var displacement = (sbyte)bytes[2];
        var indexedOperand = Idx(Z80IndexRegister.IX, displacement);
        
        var srcReg = R((Z80Register)(opcode & 7));
        var dstReg = R((Z80Register)((opcode >> 3) & 7));
        
        if ((opcode & 7) == 6) // Source is (HL) -> (IX+d)
            return Success("LD", new List<IOperand> { dstReg, indexedOperand }, bytes, address, 3);
        else if (((opcode >> 3) & 7) == 6) // Dest is (HL) -> (IX+d)
            return Success("LD", new List<IOperand> { indexedOperand, srcReg }, bytes, address, 3);
        else
            return Success("ILLEGAL", bytes, address, 2);
    }
    
    private DecodeResult DecodeIYDisplacementLD(ReadOnlySpan<byte> bytes, ulong address, byte opcode)
    {
        var displacement = (sbyte)bytes[2];
        var indexedOperand = Idx(Z80IndexRegister.IY, displacement);
        
        var srcReg = R((Z80Register)(opcode & 7));
        var dstReg = R((Z80Register)((opcode >> 3) & 7));
        
        if ((opcode & 7) == 6) // Source is (HL) -> (IY+d)
            return Success("LD", new List<IOperand> { dstReg, indexedOperand }, bytes, address, 3);
        else if (((opcode >> 3) & 7) == 6) // Dest is (HL) -> (IY+d)
            return Success("LD", new List<IOperand> { indexedOperand, srcReg }, bytes, address, 3);
        else
            return Success("ILLEGAL", bytes, address, 2);
    }
    
    private DecodeResult DecodeIXDisplacementALU(ReadOnlySpan<byte> bytes, ulong address, byte opcode)
    {
        var displacement = (sbyte)bytes[2];
        var indexedOperand = Idx(Z80IndexRegister.IX, displacement);
        var aluOp = GetAluMnemonic(opcode);
        
        return Success(aluOp, new List<IOperand> { R(Z80Register.A), indexedOperand }, bytes, address, 3);
    }
    
    private DecodeResult DecodeIYDisplacementALU(ReadOnlySpan<byte> bytes, ulong address, byte opcode)
    {
        var displacement = (sbyte)bytes[2];
        var indexedOperand = Idx(Z80IndexRegister.IY, displacement);
        var aluOp = GetAluMnemonic(opcode);
        
        return Success(aluOp, new List<IOperand> { R(Z80Register.A), indexedOperand }, bytes, address, 3);
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

    /// <summary>
    /// Success with mnemonic and operand list
    /// </summary>
    private DecodeResult Success(string mnemonic, List<IOperand> operands, ReadOnlySpan<byte> bytes, ulong address, int size, bool isBranch = false, bool isTerminator = false)
    {
        var instructionBytes = SliceBytes(bytes, size);
        var instruction = new Instruction(address, mnemonic, operands, instructionBytes, State.Clone());
        instruction.IsBranch = isBranch;
        instruction.IsBasicBlockTerminator = isTerminator;
        
        // Add next addresses based on instruction type
        AddNextAddresses(instruction, bytes, address, size, isBranch, isTerminator);
        
        return DecodeResult.CreateSuccess(instruction, size);
    }

    /// <summary>
    /// Success with mnemonic and single register operand
    /// </summary>
    private DecodeResult Success(string mnemonic, Z80Register register, ReadOnlySpan<byte> bytes, ulong address, int size)
    {
        var ops = new List<IOperand> { new Z80RegisterOperand(register) };
        return Success(mnemonic, ops, bytes, address, size);
    }

    /// <summary>
    /// Success with mnemonic and single 16-bit register operand
    /// </summary>
    private DecodeResult Success(string mnemonic, Z80Register16 register, ReadOnlySpan<byte> bytes, ulong address, int size)
    {
        var ops = new List<IOperand> { new Z80Register16Operand(register) };
        return Success(mnemonic, ops, bytes, address, size);
    }

    /// <summary>
    /// Success with mnemonic and two register operands
    /// </summary>
    private DecodeResult Success(string mnemonic, Z80Register reg1, Z80Register reg2, ReadOnlySpan<byte> bytes, ulong address, int size)
    {
        var ops = new List<IOperand> { new Z80RegisterOperand(reg1), new Z80RegisterOperand(reg2) };
        return Success(mnemonic, ops, bytes, address, size);
    }

    /// <summary>
    /// Success with mnemonic, 8-bit register and immediate value
    /// </summary>
    private DecodeResult Success(string mnemonic, Z80Register register, ulong value, ReadOnlySpan<byte> bytes, ulong address, int size)
    {
        var ops = new List<IOperand> { new Z80RegisterOperand(register), new Z80ImmediateOperand(value, 1) };
        return Success(mnemonic, ops, bytes, address, size);
    }

    /// <summary>
    /// Success with mnemonic, 16-bit register and immediate value
    /// </summary>
    private DecodeResult Success(string mnemonic, Z80Register16 register, ulong value, ReadOnlySpan<byte> bytes, ulong address, int size)
    {
        var ops = new List<IOperand> { new Z80Register16Operand(register), new Z80ImmediateOperand(value, 2) };
        return Success(mnemonic, ops, bytes, address, size);
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

    public override List<MemoryAccess> FetchMappedAccesses(Instruction ins, ICpuRegisterState registers, IMemoryMapper memoryMapper)
    {
        var z80Registers = (Z80RegisterState)registers;
        var accesses = new List<MemoryAccess>();
        var memoryAccesses = new List<(ulong address, uint size)>();

        switch (ins.Mnemonic)
        {
            case "PUSH":
            case "POP":
            case "CALL":
            case "RET":
            case "RST":
                memoryAccesses.Add((z80Registers.SP, 2));
                break;
            case "LDI":
            case "LDIR":
            case "LDD":
            case "LDDR":
                memoryAccesses.Add((z80Registers.HL, 1));
                memoryAccesses.Add((z80Registers.DE, 1));
                break;
            case "CPI":
            case "CPIR":
            case "CPD":
            case "CPDR":
                memoryAccesses.Add((z80Registers.HL, 1));
                break;
            case "INI":
            case "INIR":
            case "IND":
            case "INDR":
            case "OUTI":
            case "OTIR":
            case "OUTD":
            case "OTDR":
                memoryAccesses.Add((z80Registers.HL, 1));
                break;
            case "RRD":
            case "RLD":
                memoryAccesses.Add((z80Registers.HL, 1));
                break;
        }

        if (memoryAccesses.Count == 0)
        {
            uint size = DetermineMemoryAccessSize(ins);

            foreach (var operand in ins.Operands)
            {
                if (operand is Z80IndirectOperand indirect)
                {
                    var reg = (Z80Register16)indirect.Value;
                    memoryAccesses.Add((GetRegister16Value(z80Registers, reg), size));
                }
                else if (operand is Z80RegisterOperand regOperand && (Z80Register)regOperand.Value == Z80Register.IndirectHL)
                {
                    memoryAccesses.Add((z80Registers.HL, size));
                }
                else if (operand is Z80IndirectAddressOperand indirectAddress)
                {
                    memoryAccesses.Add((indirectAddress.Value, size));
                }
                else if (operand is Z80IndexedOperand indexed)
                {
                    var indexRegister = (Z80IndexRegister)indexed.Value;
                    var baseAddress = indexRegister == Z80IndexRegister.IX ? z80Registers.IX : z80Registers.IY;
                    var effectiveAddress = (ushort)(baseAddress + indexed.Displacement);
                    memoryAccesses.Add((effectiveAddress, size));
                }
                else if (operand is Z80SpecialRegisterOperand special)
                {
                    var name = special.Text();
                    if (name == "(SP)")
                    {
                        memoryAccesses.Add((z80Registers.SP, size));
                    }
                }
            }
        }

        foreach (var mem in memoryAccesses)
        {
            var mappedAddress = memoryMapper.MapCpuToRegion(mem.address, out var regionKey);
            if (regionKey.Key == (uint)MemoryInformationRegion.Invalid)
            {
                continue;
            }
            accesses.Add(new MemoryAccess(mappedAddress, mem.size, MemoryAccessDirection.ReadWrite, regionKey));
        }

        var ioAccesses = new List<(ulong address, uint size, MemoryAccessDirection direction)>();
        switch (ins.Mnemonic)
        {
            case "IN":
                foreach (var operand in ins.Operands)
                {
                    if (operand is Z80ImmediateOperand immediate)
                    {
                        ioAccesses.Add((immediate.Value, 1, MemoryAccessDirection.Read));
                        break;
                    }
                    if (operand is Z80SpecialRegisterOperand special && special.Text() == "(C)")
                    {
                        ioAccesses.Add(((ulong)z80Registers.BC, 1, MemoryAccessDirection.Read));
                        break;
                    }
                }
                break;
            case "OUT":
                foreach (var operand in ins.Operands)
                {
                    if (operand is Z80ImmediateOperand immediate)
                    {
                        ioAccesses.Add((immediate.Value, 1, MemoryAccessDirection.Write));
                        break;
                    }
                    if (operand is Z80SpecialRegisterOperand special && special.Text() == "(C)")
                    {
                        ioAccesses.Add(((ulong)z80Registers.BC, 1, MemoryAccessDirection.Write));
                        break;
                    }
                }
                break;
            case "INI":
            case "INIR":
            case "IND":
            case "INDR":
                ioAccesses.Add(((ulong)z80Registers.BC, 1, MemoryAccessDirection.Read));
                break;
            case "OUTI":
            case "OTIR":
            case "OUTD":
            case "OTDR":
                ioAccesses.Add(((ulong)z80Registers.BC, 1, MemoryAccessDirection.Write));
                break;
        }

        if (ioAccesses.Count > 0)
        {
            var ioRegion = new MemoryRegionKey((uint)MemoryInformationRegion.IO);
            foreach (var io in ioAccesses)
            {
                accesses.Add(new MemoryAccess(io.address, io.size, io.direction, ioRegion));
            }
        }

        return accesses;
    }

    private static ushort GetRegister16Value(Z80RegisterState registers, Z80Register16 register)
    {
        return register switch
        {
            Z80Register16.BC => registers.BC,
            Z80Register16.DE => registers.DE,
            Z80Register16.HL => registers.HL,
            Z80Register16.SP => registers.SP,
            _ => 0
        };
    }

    private static uint DetermineMemoryAccessSize(Instruction ins)
    {
        if (ins.Mnemonic == "EX" && ins.Operands.Any(o => o is Z80SpecialRegisterOperand special && special.Text() == "(SP)"))
            return 2;

        if (ins.Operands.Any(o => o is Z80Register16Operand))
            return 2;

        if (ins.Operands.Any(o => o is Z80SpecialRegisterOperand special && (special.Text() == "IX" || special.Text() == "IY")))
            return 2;

        return 1;
    }
}
