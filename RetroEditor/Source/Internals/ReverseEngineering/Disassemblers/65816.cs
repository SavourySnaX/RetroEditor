using RetroEditor.Plugins;

/// <summary>
/// Represents the addressing modes supported by the 65816 CPU
/// </summary>
internal enum AddressingMode
{
    Implicit,                       // No operand (e.g., RTS)
    ImmediateShort,                 // Immediate Short (always 1 byte immediate) (e.g., BRK,SEP)
    Immediate,                      // Operand is next byte(s) (e.g., LDA #$42)
    Absolute,                       // 16-bit address (e.g., LDA $1234)
    AbsoluteIndexedX,               // Absolute indexed by X (e.g., LDA $1234,X)
    AbsoluteIndexedY,               // Absolute indexed by Y (e.g., LDA $1234,Y)
    AbsoluteLong,                   // 24-bit address (e.g., JML $123456)
    AbsoluteLongIndexedX,           // 24-bit address indexed by X (e.g., STA $123456,X)
    AbsoluteIndirect,               // Absolute indirect (e.g., JMP ($1234))
    AbsoluteIndirectIndexedX,       // Absolute indirect indexed by X (e.g., LDA ($1234,X))
    AbsoluteLongIndirect,           // Absolute long indirect (e.g., JMP [$123456])
    DirectPage,         // Operand is in direct page (e.g., LDA $42)
    DirectPageIndexedX, // Direct page indexed by X (e.g., LDA $42,X)
    DirectPageIndexedY, // Direct page indexed by Y (e.g., LDA $42,X)
    DirectPageIndirect,// Direct page indirect (e.g., JMP ($42))
    DirectPageIndirectX,// Direct page indirect indexed by X (e.g., LDA ($42,X))
    DirectPageIndirectIndexedY,// Direct page indirect indexed by Y (e.g., LDA ($42),Y)
    DirectPageIndirectLong,// Direct page indirect long (e.g., JMP [$42])
    DirectPageIndirectLongIndexedY,// Direct page indirect long indexed by Y (e.g., LDA [$42],Y)
    StackRelative,     // Stack relative (e.g., STA $42,S)
    StackRelativeIndirectY, // Stack relative indirect indexed by Y (e.g., STA ($42,S),Y)
    BlockMove,         // Block move (e.g., MVP $12->$56)
    ProgramCounterRelative, // Relative branch (e.g., BRA $42)
    ProgramCounterRelativeLong // Long relative branch (e.g., BRL $1234)
}

/// <summary>
/// Represents the CPU state for the 65816
/// </summary>
internal class SNES65816State : CpuState
{
    /// <summary>
    /// Whether the CPU is in emulation mode (E=1) or native mode (E=0)
    /// </summary>
    internal bool EmulationMode { get; set; }

    /// <summary>
    /// Whether the accumulator is in 16-bit mode (M=0) or 8-bit mode (M=1)
    /// </summary>
    internal bool Accumulator8Bit { get; set; }

    /// <summary>
    /// Whether the index registers are in 16-bit mode (X=0) or 8-bit mode (X=1)
    /// </summary>
    internal bool Index8Bit { get; set; }

    public override CpuState Clone()
    {
        return new SNES65816State
        {
            EmulationMode = this.EmulationMode,
            Accumulator8Bit = this.Accumulator8Bit,
            Index8Bit = this.Index8Bit
        };
    }

    public void SetEmulationMode(bool emulationMode)
    {
        if (EmulationMode == emulationMode)
            return; // No change
        EmulationMode = emulationMode;
        Accumulator8Bit = true; // Set to 8-bit mode in emulation
        Index8Bit = true;      // Set to 8-bit mode in emulation
    }
}

/// <summary>
/// Disassembler for the 65816 CPU
/// </summary>
internal class SNES65816Disassembler : DisassemblerBase
{
    private static readonly string[] Mnemonics = new string[256]
    {
        "BRK", "ORA", "COP", "ORA", "TSB", "ORA", "ASL", "ORA", "PHP", "ORA", "ASL", "PHD", "TSB", "ORA", "ASL", "ORA", // 0x00-0x0F
        "BPL", "ORA", "ORA", "ORA", "TRB", "ORA", "ASL", "ORA", "CLC", "ORA", "INC", "TCS", "TRB", "ORA", "ASL", "ORA", // 0x10-0x1F
        "JSR", "AND", "JSL", "AND", "BIT", "AND", "ROL", "AND", "PLP", "AND", "ROL", "PLD", "BIT", "AND", "ROL", "AND", // 0x20-0x2F
        "BMI", "AND", "AND", "AND", "BIT", "AND", "ROL", "AND", "SEC", "AND", "DEC", "TSC", "BIT", "AND", "ROL", "AND", // 0x30-0x3F
        "RTI", "EOR", "WDM", "EOR", "MVP", "EOR", "LSR", "EOR", "PHA", "EOR", "LSR", "PHK", "JMP", "EOR", "LSR", "EOR", // 0x40-0x4F
        "BVC", "EOR", "EOR", "EOR", "MVN", "EOR", "LSR", "EOR", "CLI", "EOR", "PHY", "TCD", "JMP", "EOR", "LSR", "EOR", // 0x50-0x5F
        "RTS", "ADC", "PER", "ADC", "STZ", "ADC", "ROR", "ADC", "PLA", "ADC", "ROR", "RTL", "JMP", "ADC", "ROR", "ADC", // 0x60-0x6F
        "BVS", "ADC", "ADC", "ADC", "STZ", "ADC", "ROR", "ADC", "SEI", "ADC", "PLY", "TDC", "JMP", "ADC", "ROR", "ADC", // 0x70-0x7F
        "BRA", "STA", "BRL", "STA", "STY", "STA", "STX", "STA", "DEY", "BIT", "TXA", "PHB", "STY", "STA", "STX", "STA", // 0x80-0x8F
        "BCC", "STA", "STA", "STA", "STY", "STA", "STX", "STA", "TYA", "STA", "TXS", "TXY", "STZ", "STA", "STZ", "STA", // 0x90-0x9F
        "LDY", "LDA", "LDX", "LDA", "LDY", "LDA", "LDX", "LDA", "TAY", "LDA", "TAX", "PLB", "LDY", "LDA", "LDX", "LDA", // 0xA0-0xAF
        "BCS", "LDA", "LDA", "LDA", "LDY", "LDA", "LDX", "LDA", "CLV", "LDA", "TSX", "TYX", "LDY", "LDA", "LDX", "LDA", // 0xB0-0xBF
        "CPY", "CMP", "REP", "CMP", "CPY", "CMP", "DEC", "CMP", "INY", "CMP", "DEX", "WAI", "CPY", "CMP", "DEC", "CMP", // 0xC0-0xCF
        "BNE", "CMP", "CMP", "CMP", "PEI", "CMP", "DEC", "CMP", "CLD", "CMP", "PHX", "STP", "JMP", "CMP", "DEC", "CMP", // 0xD0-0xDF
        "CPX", "SBC", "SEP", "SBC", "CPX", "SBC", "INC", "SBC", "INX", "SBC", "NOP", "XBA", "CPX", "SBC", "INC", "SBC", // 0xE0-0xEF
        "BEQ", "SBC", "SBC", "SBC", "PEA", "SBC", "INC", "SBC", "SED", "SBC", "PLX", "XCE", "JSR", "SBC", "INC", "SBC"  // 0xF0-0xFF
    };

    private static readonly byte[] InstructionLengths = new byte[256]
    {
        2, 2, 2, 2, 2, 2, 2, 2, 1, 2, 1, 1, 3, 3, 3, 4,  // 0x00-0x0F
        2, 2, 2, 2, 2, 2, 2, 2, 1, 3, 1, 1, 3, 3, 3, 4,  // 0x10-0x1F
        3, 2, 4, 2, 2, 2, 2, 2, 1, 2, 1, 1, 3, 3, 3, 4,  // 0x20-0x2F
        2, 2, 2, 2, 2, 2, 2, 2, 1, 3, 1, 1, 3, 3, 3, 4,  // 0x30-0x3F
        1, 2, 2, 2, 3, 2, 2, 2, 1, 2, 1, 1, 3, 3, 3, 4,  // 0x40-0x4F
        2, 2, 2, 2, 3, 2, 2, 2, 1, 3, 1, 1, 4, 3, 3, 4,  // 0x50-0x5F
        1, 2, 3, 2, 2, 2, 2, 2, 1, 2, 1, 1, 3, 3, 3, 4,  // 0x60-0x6F
        2, 2, 2, 2, 2, 2, 2, 2, 1, 3, 1, 1, 3, 3, 3, 4,  // 0x70-0x7F
        2, 2, 3, 2, 2, 2, 2, 2, 1, 2, 1, 1, 3, 3, 3, 4,  // 0x80-0x8F
        2, 2, 2, 2, 2, 2, 2, 2, 1, 3, 1, 1, 3, 3, 3, 4,  // 0x90-0x9F
        2, 2, 2, 2, 2, 2, 2, 2, 1, 2, 1, 1, 3, 3, 3, 4,  // 0xA0-0xAF
        2, 2, 2, 2, 2, 2, 2, 2, 1, 3, 1, 1, 3, 3, 3, 4,  // 0xB0-0xBF
        2, 2, 2, 2, 2, 2, 2, 2, 1, 2, 1, 1, 3, 3, 3, 4,  // 0xC0-0xCF
        2, 2, 2, 2, 2, 2, 2, 2, 1, 3, 1, 1, 4, 3, 3, 4,  // 0xD0-0xDF
        2, 2, 2, 2, 2, 2, 2, 2, 1, 2, 1, 1, 3, 3, 3, 4,  // 0xE0-0xEF
        2, 2, 2, 2, 3, 2, 2, 2, 1, 3, 1, 1, 3, 3, 3, 4   // 0xF0-0xFF
    };

    private static readonly AddressingMode[] AddressingModes = new AddressingMode[256]
    {
        AddressingMode.ImmediateShort,                  // BRK   0x00
        AddressingMode.DirectPageIndirectX,             // ORA   0x01
        AddressingMode.ImmediateShort,                  // COP   0x02
        AddressingMode.StackRelative,                   // ORA   0x03
        AddressingMode.DirectPage,                      // TSB   0x04
        AddressingMode.DirectPage,                      // ORA   0x05
        AddressingMode.DirectPage,                      // ASL   0x06
        AddressingMode.DirectPageIndirectLong,          // ORA   0x07
        AddressingMode.Implicit,                        // PHP   0x08
        AddressingMode.Immediate,                       // ORA   0x09
        AddressingMode.Implicit,                        // ASL   0x0A
        AddressingMode.Implicit,                        // PHD   0x0B
        AddressingMode.Absolute,                        // TSB   0x0C
        AddressingMode.Absolute,                        // ORA   0x0D
        AddressingMode.Absolute,                        // ASL   0x0E
        AddressingMode.AbsoluteLong,                    // ORA   0x0F

        AddressingMode.ProgramCounterRelative,          // BPL  0x10
        AddressingMode.DirectPageIndirectIndexedY,      // ORA
        AddressingMode.DirectPageIndirect,              // ORA
        AddressingMode.StackRelativeIndirectY,          // ORA
        AddressingMode.DirectPage,                      // TRB
        AddressingMode.DirectPageIndexedX,              // ORA
        AddressingMode.DirectPageIndexedX,              // ASL
        AddressingMode.DirectPageIndirectLongIndexedY,  // ORA
        AddressingMode.Implicit,                        // CLC
        AddressingMode.AbsoluteIndexedY,                // ORA
        AddressingMode.Implicit,                        // INC
        AddressingMode.Implicit,                        // TCS
        AddressingMode.Absolute,                        // TRB
        AddressingMode.AbsoluteIndexedX,                // ORA
        AddressingMode.AbsoluteIndexedX,                // ASL
        AddressingMode.AbsoluteLongIndexedX,            // ORA

        AddressingMode.Absolute,                        // JSR   0x20
        AddressingMode.DirectPageIndirectX,             // AND
        AddressingMode.AbsoluteLong,                    // JSL
        AddressingMode.StackRelative,                   // AND
        AddressingMode.DirectPage,                      // BIT
        AddressingMode.DirectPage,                      // AND
        AddressingMode.DirectPage,                      // ROL
        AddressingMode.DirectPageIndirectLong,          // AND
        AddressingMode.Implicit,                        // PLP
        AddressingMode.Immediate,                       // AND
        AddressingMode.Implicit,                        // ROL
        AddressingMode.Implicit,                        // PLD
        AddressingMode.Absolute,                        // BIT
        AddressingMode.Absolute,                        // AND
        AddressingMode.Absolute,                        // ROL
        AddressingMode.AbsoluteLong,                    // AND

        AddressingMode.ProgramCounterRelative,          // BMI  0x30
        AddressingMode.DirectPageIndirectIndexedY,      // AND
        AddressingMode.DirectPageIndirect,              // AND
        AddressingMode.StackRelativeIndirectY,          // AND
        AddressingMode.DirectPageIndexedX,              // BIT
        AddressingMode.DirectPageIndexedX,              // AND
        AddressingMode.DirectPageIndexedX,              // ROL
        AddressingMode.DirectPageIndirectLongIndexedY,  // AND
        AddressingMode.Implicit,                        // SEC
        AddressingMode.AbsoluteIndexedY,                // AND
        AddressingMode.Implicit,                        // DEC
        AddressingMode.Implicit,                        // TSC
        AddressingMode.AbsoluteIndexedX,                // BIT
        AddressingMode.AbsoluteIndexedX,                // AND
        AddressingMode.AbsoluteIndexedX,                // ROL
        AddressingMode.AbsoluteLongIndexedX,            // AND

        AddressingMode.Implicit,                        // RTI   0x40
        AddressingMode.DirectPageIndirectX,             // EOR
        AddressingMode.ImmediateShort,                  // WDM
        AddressingMode.StackRelative,                   // EOR
        AddressingMode.BlockMove,                       // MVP
        AddressingMode.DirectPage,                      // EOR
        AddressingMode.DirectPage,                      // LSR
        AddressingMode.DirectPageIndirectLong,          // EOR
        AddressingMode.Implicit,                        // PHA
        AddressingMode.Immediate,                       // EOR
        AddressingMode.Implicit,                        // LSR
        AddressingMode.Implicit,                        // PHK
        AddressingMode.Absolute,                        // JMP
        AddressingMode.Absolute,                        // EOR
        AddressingMode.Absolute,                        // LSR
        AddressingMode.AbsoluteLong,                    // EOR

        AddressingMode.ProgramCounterRelative,          // BVC  0x50
        AddressingMode.DirectPageIndirectIndexedY,      // EOR
        AddressingMode.DirectPageIndirect,              // EOR
        AddressingMode.StackRelativeIndirectY,          // EOR
        AddressingMode.BlockMove,                       // MVN
        AddressingMode.DirectPageIndexedX,              // EOR
        AddressingMode.DirectPageIndexedX,              // LSR
        AddressingMode.DirectPageIndirectLongIndexedY,  // EOR
        AddressingMode.Implicit,                        // CLI
        AddressingMode.AbsoluteIndexedY,                // EOR
        AddressingMode.Implicit,                        // PHY
        AddressingMode.Implicit,                        // TCD
        AddressingMode.AbsoluteLong,                    // JML
        AddressingMode.AbsoluteIndexedX,                // EOR
        AddressingMode.AbsoluteIndexedX,                // LSR
        AddressingMode.AbsoluteLongIndexedX,            // EOR

        AddressingMode.Implicit,                        // RTS  0x60
        AddressingMode.DirectPageIndirectX,             // ADC
        AddressingMode.ProgramCounterRelativeLong,      // PER
        AddressingMode.StackRelative,                   // ADC
        AddressingMode.DirectPage,                      // STZ
        AddressingMode.DirectPage,                      // ADC
        AddressingMode.DirectPage,                      // ROR
        AddressingMode.DirectPageIndirectLong,          // ADC
        AddressingMode.Implicit,                        // PLA
        AddressingMode.Immediate,                       // ADC
        AddressingMode.Implicit,                        // ROR
        AddressingMode.Implicit,                        // RTL
        AddressingMode.AbsoluteIndirect,                // JMP
        AddressingMode.Absolute,                        // ADC
        AddressingMode.Absolute,                        // ROR
        AddressingMode.AbsoluteLong,                    // ADC

        AddressingMode.ProgramCounterRelative,          // BVS  0x70
        AddressingMode.DirectPageIndirectIndexedY,      // ADC
        AddressingMode.DirectPageIndirect,              // ADC
        AddressingMode.StackRelativeIndirectY,          // ADC
        AddressingMode.DirectPageIndexedX,              // STZ
        AddressingMode.DirectPageIndexedX,              // ADC
        AddressingMode.DirectPageIndexedX,              // ROR
        AddressingMode.DirectPageIndirectLongIndexedY,  // ADC
        AddressingMode.Implicit,                        // SEI
        AddressingMode.AbsoluteIndexedY,                // ADC
        AddressingMode.Implicit,                        // PLY
        AddressingMode.Implicit,                        // TDC
        AddressingMode.AbsoluteIndirectIndexedX,        // JMP
        AddressingMode.AbsoluteIndexedX,                // ADC
        AddressingMode.AbsoluteIndexedX,                // ROR
        AddressingMode.AbsoluteLongIndexedX,            // ADC

        AddressingMode.ProgramCounterRelative,          // BRA  0x80
        AddressingMode.DirectPageIndirectX,             // STA
        AddressingMode.ProgramCounterRelativeLong,      // BRL
        AddressingMode.StackRelative,                   // STA
        AddressingMode.DirectPage,                      // STY
        AddressingMode.DirectPage,                      // STA
        AddressingMode.DirectPage,                      // STX
        AddressingMode.DirectPageIndirectLong,          // STA
        AddressingMode.Implicit,                        // DEY
        AddressingMode.Immediate,                       // BIT
        AddressingMode.Implicit,                        // TXA
        AddressingMode.Implicit,                        // PHB
        AddressingMode.Absolute,                        // STY
        AddressingMode.Absolute,                        // STA
        AddressingMode.Absolute,                        // STX
        AddressingMode.AbsoluteLong,                    // STA

        AddressingMode.ProgramCounterRelative,          // BCC  0x90
        AddressingMode.DirectPageIndirectIndexedY,      // STA
        AddressingMode.DirectPageIndirect,              // STA
        AddressingMode.StackRelativeIndirectY,          // STA
        AddressingMode.DirectPageIndexedX,              // STY
        AddressingMode.DirectPageIndexedX,              // STA
        AddressingMode.DirectPageIndexedY,              // STX
        AddressingMode.DirectPageIndirectLongIndexedY,  // STA
        AddressingMode.Implicit,                        // TYA
        AddressingMode.AbsoluteIndexedY,                // STA
        AddressingMode.Implicit,                        // TXS
        AddressingMode.Implicit,                        // TXY
        AddressingMode.Absolute,                        // STZ
        AddressingMode.AbsoluteIndexedX,                // STA
        AddressingMode.AbsoluteIndexedX,                // STZ
        AddressingMode.AbsoluteLongIndexedX,            // STA

        AddressingMode.Immediate,                       // LDY  0xA0
        AddressingMode.DirectPageIndirectX,             // LDA
        AddressingMode.Immediate,                       // LDX
        AddressingMode.StackRelative,                   // LDA
        AddressingMode.DirectPage,                      // LDY
        AddressingMode.DirectPage,                      // LDA
        AddressingMode.DirectPage,                      // LDX
        AddressingMode.DirectPageIndirectLong,          // LDA
        AddressingMode.Implicit,                        // TAY
        AddressingMode.Immediate,                       // LDA
        AddressingMode.Implicit,                        // TAX
        AddressingMode.Implicit,                        // PLB
        AddressingMode.Absolute,                        // LDY
        AddressingMode.Absolute,                        // LDA
        AddressingMode.Absolute,                        // LDX
        AddressingMode.AbsoluteLong,                    // LDA

        AddressingMode.ProgramCounterRelative,          // BCS  0xB0
        AddressingMode.DirectPageIndirectIndexedY,      // LDA
        AddressingMode.DirectPageIndirect,              // LDA
        AddressingMode.StackRelativeIndirectY,          // LDA
        AddressingMode.DirectPageIndexedX,              // LDY
        AddressingMode.DirectPageIndexedX,              // LDA
        AddressingMode.DirectPageIndexedY,              // LDX
        AddressingMode.DirectPageIndirectLongIndexedY,  // LDA
        AddressingMode.Implicit,                        // CLV
        AddressingMode.AbsoluteIndexedY,                // LDA
        AddressingMode.Implicit,                        // TSX
        AddressingMode.Implicit,                        // TYX
        AddressingMode.AbsoluteIndexedX,                // LDY
        AddressingMode.AbsoluteIndexedX,                // LDA
        AddressingMode.AbsoluteIndexedY,                // LDX
        AddressingMode.AbsoluteLongIndexedX,            // LDA

        AddressingMode.Immediate,                       // CPY  0xC0
        AddressingMode.DirectPageIndirectX,             // CMP
        AddressingMode.ImmediateShort,                  // REP
        AddressingMode.StackRelative,                   // CMP
        AddressingMode.DirectPage,                      // CPY
        AddressingMode.DirectPage,                      // CMP
        AddressingMode.DirectPage,                      // DEC
        AddressingMode.DirectPageIndirectLong,          // CMP
        AddressingMode.Implicit,                        // INY
        AddressingMode.Immediate,                       // CMP
        AddressingMode.Implicit,                        // DEX
        AddressingMode.Implicit,                        // WAI
        AddressingMode.Absolute,                        // CPY
        AddressingMode.Absolute,                        // CMP
        AddressingMode.Absolute,                        // DEC
        AddressingMode.AbsoluteLong,                    // CMP

        AddressingMode.ProgramCounterRelative,          // BNE  0xD0
        AddressingMode.DirectPageIndirectIndexedY,      // CMP
        AddressingMode.DirectPageIndirect,              // CMP
        AddressingMode.StackRelativeIndirectY,          // CMP
        AddressingMode.DirectPageIndirect,              // PEI
        AddressingMode.DirectPageIndexedX,              // CMP
        AddressingMode.DirectPageIndexedX,              // DEC
        AddressingMode.DirectPageIndirectLongIndexedY,  // CMP
        AddressingMode.Implicit,                        // CLD
        AddressingMode.AbsoluteIndexedY,                // CMP
        AddressingMode.Implicit,                        // PHX
        AddressingMode.Implicit,                        // STP
        AddressingMode.AbsoluteLongIndirect,            // JML
        AddressingMode.AbsoluteIndexedX,                // CMP
        AddressingMode.AbsoluteIndexedX,                // DEC
        AddressingMode.AbsoluteLongIndexedX,            // CMP

        AddressingMode.Immediate,                       // CPX  0xE0
        AddressingMode.DirectPageIndirectX,             // SBC
        AddressingMode.ImmediateShort,                  // SEP
        AddressingMode.StackRelative,                   // SBC
        AddressingMode.DirectPage,                      // CPX
        AddressingMode.DirectPage,                      // SBC
        AddressingMode.DirectPage,                      // INC
        AddressingMode.DirectPageIndirectLong,          // SBC
        AddressingMode.Implicit,                        // INX
        AddressingMode.Immediate,                       // SBC
        AddressingMode.Implicit,                        // NOP
        AddressingMode.Implicit,                        // XBA
        AddressingMode.Absolute,                        // CPX
        AddressingMode.Absolute,                        // SBC
        AddressingMode.Absolute,                        // INC
        AddressingMode.AbsoluteLong,                    // SBC

        AddressingMode.ProgramCounterRelative,          // BEQ  0xF0
        AddressingMode.DirectPageIndirectIndexedY,      // SBC
        AddressingMode.DirectPageIndirect,              // SBC
        AddressingMode.StackRelativeIndirectY,          // SBC
        AddressingMode.Absolute,                        // PEA
        AddressingMode.DirectPageIndexedX,              // SBC
        AddressingMode.DirectPageIndexedX,              // INC
        AddressingMode.DirectPageIndirectLongIndexedY,  // SBC
        AddressingMode.Implicit,                        // SED
        AddressingMode.AbsoluteIndexedY,                // SBC
        AddressingMode.Implicit,                        // PLX
        AddressingMode.Implicit,                        // XCE
        AddressingMode.AbsoluteIndirectIndexedX,        // JSR
        AddressingMode.AbsoluteIndexedX,                // SBC
        AddressingMode.AbsoluteIndexedX,                // INC
        AddressingMode.AbsoluteLongIndexedX             // SBC
    };

    internal SNES65816Disassembler()
    {
    }

    public override string ArchitectureName => "65816";
    public override MemoryEndian Endianness => MemoryEndian.Little;

    protected override CpuState CreateInitialState()
    {
        return new SNES65816State
        {
            EmulationMode = true,  // Start in emulation mode
            Accumulator8Bit = true,
            Index8Bit = true
        };
    }

    public override DecodeResult DecodeNext(ReadOnlySpan<byte> bytes, ulong address)
    {
        if (bytes.Length < 1)
            return DecodeResult.NeedMoreBytes(1);

        var state = (SNES65816State)State;
        var opcode = bytes[0];
        var mnemonic = Mnemonics[opcode];
        var baseLength = InstructionLengths[opcode];
        var addressingMode = AddressingModes[opcode];
        if (addressingMode == AddressingMode.Immediate)
        {
            if (!(state.EmulationMode || state.Accumulator8Bit))
                baseLength++;
        }
        var immediateLength = baseLength;

        // Check if we have enough bytes for the instruction
        if (bytes.Length < baseLength)
            return DecodeResult.NeedMoreBytes(baseLength - bytes.Length);

        var operands = new List<Operand>();
        var instructionBytes = new byte[baseLength];
        bytes.Slice(0, baseLength).CopyTo(instructionBytes);

        // Handle special instructions that modify CPU state
        switch (opcode)
        {
            case 0xC2: // REP
                if (baseLength != 2) return DecodeResult.CreateError("Invalid REP instruction length");
                var flags = bytes[1];
                var newState = (SNES65816State)state.Clone();
                if (!newState.EmulationMode)  // Only modify flags in native mode
                {
                    newState.Accumulator8Bit = (flags & 0x20) == 0; // M=0 means 16-bit accumulator
                    newState.Index8Bit = (flags & 0x10) == 0;      // X=0 means 16-bit index
                }
                State = newState;
                break;

            case 0xE2: // SEP
                if (baseLength != 2) return DecodeResult.CreateError("Invalid SEP instruction length");
                flags = bytes[1];
                newState = (SNES65816State)state.Clone();
                if (!newState.EmulationMode)  // Only modify flags in native mode
                {
                    newState.Accumulator8Bit = (flags & 0x20) != 0; // M=1 means 8-bit accumulator
                    newState.Index8Bit = (flags & 0x10) != 0;      // X=1 means 8-bit index
                }
                State = newState;
                break;

            case 0xFB: // XCE
                if (baseLength != 1) return DecodeResult.CreateError("Invalid XCE instruction length");
                // We can't change automatically switch because we don't know the state of carry
                break;
        }

        // Handle addressing modes
        switch (addressingMode)
        {
            case AddressingMode.Implicit:
                // No operands needed
                break;
            case AddressingMode.ImmediateShort:
                if (baseLength != 2) return DecodeResult.CreateError($"Invalid {mnemonic} instruction length");
                operands.Add(new Operand($"#${bytes[1]:X2}", value: bytes[1]));
                break;

            case AddressingMode.Immediate:
                if (baseLength != immediateLength) return DecodeResult.CreateError($"Invalid {mnemonic} immediate instruction length");
                if (immediateLength == 2)
                {
                    operands.Add(new Operand($"#${bytes[1]:X2}", value: bytes[1]));
                }
                else
                {
                    var immediateValue = (ulong)(bytes[1] + (bytes[2] << 8));
                    operands.Add(new Operand($"#${immediateValue:X4}", value: immediateValue));
                }
                break;

            case AddressingMode.Absolute:
                if (baseLength != 3) return DecodeResult.CreateError($"Invalid {mnemonic} absolute instruction length");
                var target = (ulong)bytes[1] + ((ulong)bytes[2] << 8);
                operands.Add(new Operand($"${target:X4}", value: target));
                break;

            case AddressingMode.AbsoluteIndexedX:
                if (baseLength != 3) return DecodeResult.CreateError($"Invalid {mnemonic} absolute X instruction length");
                target = (ulong)bytes[1] + ((ulong)bytes[2] << 8);
                operands.Add(new Operand($"${target:X4},X", value: target));
                break;

            case AddressingMode.AbsoluteIndexedY:
                if (baseLength != 3) return DecodeResult.CreateError($"Invalid {mnemonic} absolute Y instruction length");
                target = (ulong)bytes[1] + ((ulong)bytes[2] << 8);
                operands.Add(new Operand($"${target:X4},Y", value: target));
                break;

            case AddressingMode.AbsoluteLong:
                if (baseLength != 4) return DecodeResult.CreateError($"Invalid {mnemonic} absolute long instruction length");
                target = (ulong)bytes[1] + ((ulong)bytes[2] << 8) + ((ulong)bytes[3] << 16);
                operands.Add(new Operand($"${target:X6}", value: target));
                break;

            case AddressingMode.AbsoluteLongIndexedX:
                if (baseLength != 4) return DecodeResult.CreateError($"Invalid {mnemonic} absolute long X instruction length");
                target = (ulong)bytes[1] + ((ulong)bytes[2] << 8) + ((ulong)bytes[3] << 16);
                operands.Add(new Operand($"${target:X6},X", value: target));
                break;

            case AddressingMode.AbsoluteIndirect:
                if (baseLength != 3) return DecodeResult.CreateError($"Invalid {mnemonic} absolute indirect instruction length");
                target = (ulong)bytes[1] + ((ulong)bytes[2] << 8);
                operands.Add(new Operand($"(${target:X4})", value: target));
                break;

            case AddressingMode.AbsoluteIndirectIndexedX:
                if (baseLength != 3) return DecodeResult.CreateError($"Invalid {mnemonic} absolute indirect X instruction length");
                target = (ulong)bytes[1] + ((ulong)bytes[2] << 8);
                operands.Add(new Operand($"(${target:X4},X)", value: target));
                break;

            case AddressingMode.AbsoluteLongIndirect:
                if (baseLength != 4) return DecodeResult.CreateError($"Invalid {mnemonic} absolute long indirect instruction length");
                target = (ulong)bytes[1] + ((ulong)bytes[2] << 8) + ((ulong)bytes[3] << 16);
                operands.Add(new Operand($"[${target:X6}]", value: target));
                break;

            case AddressingMode.DirectPage:
                if (baseLength != 2) return DecodeResult.CreateError($"Invalid {mnemonic} direct page instruction length");
                operands.Add(new Operand($"${bytes[1]:X2}", value: bytes[1]));
                break;

            case AddressingMode.DirectPageIndexedX:
                if (baseLength != 2) return DecodeResult.CreateError($"Invalid {mnemonic} direct page X instruction length");
                operands.Add(new Operand($"${bytes[1]:X2},X", value: bytes[1]));
                break;
            
            case AddressingMode.DirectPageIndexedY:
                if (baseLength != 2) return DecodeResult.CreateError($"Invalid {mnemonic} direct page Y instruction length");
                operands.Add(new Operand($"${bytes[1]:X2},Y", value: bytes[1]));
                break;

            case AddressingMode.DirectPageIndirect:
                if (baseLength != 2) return DecodeResult.CreateError($"Invalid {mnemonic} direct page indirect instruction length");
                operands.Add(new Operand($"(${bytes[1]:X2})", value: bytes[1]));
                break;

            case AddressingMode.DirectPageIndirectX:
                if (baseLength != 2) return DecodeResult.CreateError($"Invalid {mnemonic} direct page indirect X instruction length");
                operands.Add(new Operand($"(${bytes[1]:X2},X)", value: bytes[1]));
                break;

            case AddressingMode.DirectPageIndirectIndexedY:
                if (baseLength != 2) return DecodeResult.CreateError($"Invalid {mnemonic} direct page indirect Y instruction length");
                operands.Add(new Operand($"(${bytes[1]:X2}),Y", value: bytes[1]));
                break;

            case AddressingMode.DirectPageIndirectLong:
                if (baseLength != 2) return DecodeResult.CreateError($"Invalid {mnemonic} direct page indirect long instruction length");
                target = (ulong)bytes[1];
                operands.Add(new Operand($"[${target:X2}]", value: target));
                break;
            
            case AddressingMode.DirectPageIndirectLongIndexedY:
                if (baseLength != 2) return DecodeResult.CreateError($"Invalid {mnemonic} direct page indirect long indexed Y instruction length");
                target = (ulong)bytes[1];
                operands.Add(new Operand($"[${target:X2}],Y", value: target));
                break;

            case AddressingMode.StackRelative:
                if (baseLength != 2) return DecodeResult.CreateError($"Invalid {mnemonic} stack relative instruction length");
                operands.Add(new Operand($"${bytes[1]:X2},S", value: bytes[1]));
                break;

            case AddressingMode.StackRelativeIndirectY:
                if (baseLength != 2) return DecodeResult.CreateError($"Invalid {mnemonic} stack relative indirect Y instruction length");
                operands.Add(new Operand($"(${bytes[1]:X2},S),Y", value: bytes[1]));
                break;

            case AddressingMode.BlockMove:
                if (baseLength != 3) return DecodeResult.CreateError($"Invalid {mnemonic} block move instruction length");
                var source = (ulong)bytes[1];
                var dest = (ulong)bytes[2];
                operands.Add(new Operand($"${source:X2}", value: source, isSource: true));
                operands.Add(new Operand($"${dest:X2}", value: source, isDestination: true));
                break;

            case AddressingMode.ProgramCounterRelative:
                if (baseLength != 2) return DecodeResult.CreateError($"Invalid {mnemonic} program counter relative instruction length");
                target = address + 2 + (ulong)(sbyte)bytes[1];
                target&=0xFFFF;
                operands.Add(new Operand($"${target:X4}", value: target));
                break;

            case AddressingMode.ProgramCounterRelativeLong:
                if (baseLength != 3) return DecodeResult.CreateError($"Invalid {mnemonic} program counter relative long instruction length");
                target = address + 3 + (ulong)(short)((ushort)bytes[1] + (ushort)(bytes[2] << 8));
                target&=0xFFFF;
                operands.Add(new Operand($"${target:X4}", value: target));
                break;
        }

        var instruction = new Instruction(address, mnemonic, operands, instructionBytes);

        // Set branch flags
        instruction.IsBranch = 
                             opcode == 0x20 || // JSR
                             opcode == 0x22 || // JSR
                             opcode == 0xFC || // JSL
                             opcode == 0x4C || // JMP
                             opcode == 0x5C || // JML
                             opcode == 0x6C || // JMP
                             opcode == 0x7C || // JMP
                             opcode == 0xDC || // JML
                             opcode == 0x80 || // BRA
                             opcode == 0x82 || // BRL
                             opcode == 0x10 || // BPL
                             opcode == 0x30 || // BMI
                             opcode == 0x50 || // BVC
                             opcode == 0x70 || // BVS
                             opcode == 0x90 || // BCC
                             opcode == 0xB0 || // BCS
                             opcode == 0xD0 || // BNE
                             opcode == 0xF0;   // BEQ

        instruction.IsBasicBlockTerminator = opcode == 0x82 || // BRL
                                            opcode == 0x80 || // BRA
                                            opcode == 0x4C || // JMP
                                            opcode == 0x5C || // JML
                                            opcode == 0x6C || // JMP
                                            opcode == 0x7C || // JMP
                                            opcode == 0xDC || // JML
                                            opcode == 0x40 || // RTI
                                            opcode == 0x60 || // RTS
                                            opcode == 0x6B || // RTL
                                            opcode == 0x20 || // JSR
                                            opcode == 0x22 || // JSR
                                            opcode == 0xFC || // JSL
                                            opcode == 0x02 || // COP
                                            opcode == 0xDB || // STP
                                            opcode == 0x00;   // BRK

        // Add next addresses
        bool nextInstruction=!instruction.IsBasicBlockTerminator;
        if (instruction.IsBranch)
        {
            var value = operands[0].Value.GetValueOrDefault();
            if (operands.Count > 0 && operands[0].Value != null)
            {
                instruction.NextAddresses.Add(value);
            }
            else
            {
                return DecodeResult.CreateError($"Branch instruction {mnemonic} has no target address");
            }
        }
        if (nextInstruction)
        {
            instruction.NextAddresses.Add(address + (ulong)baseLength);
        }

        return DecodeResult.CreateSuccess(instruction, baseLength);
    }
}
