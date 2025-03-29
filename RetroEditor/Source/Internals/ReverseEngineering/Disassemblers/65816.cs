using RetroEditor.Plugins;

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
        "BRK", "ORA", "COP", "ORA", "TSB", "ORA", "ASL", "ORA", "PHP", "ORA", "ASL", "PHD", "TSB", "ORA", "ASL", "ORA",
        "BPL", "ORA", "ORA", "ORA", "TRB", "ORA", "ASL", "ORA", "CLC", "ORA", "INC", "TCS", "TRB", "ORA", "ASL", "ORA",
        "JSR", "AND", "JSL", "AND", "BIT", "AND", "ROL", "AND", "PLP", "AND", "ROL", "PLD", "BIT", "AND", "ROL", "AND",
        "BMI", "AND", "AND", "AND", "BIT", "AND", "ROL", "AND", "SEC", "AND", "DEC", "TSC", "BIT", "AND", "ROL", "AND",
        "RTI", "EOR", "WDM", "EOR", "MVP", "EOR", "LSR", "EOR", "PHA", "EOR", "LSR", "PHK", "JMP", "EOR", "LSR", "EOR",
        "BVC", "EOR", "EOR", "EOR", "MVN", "EOR", "LSR", "EOR", "CLI", "EOR", "PHY", "TCD", "JMP", "EOR", "LSR", "EOR",
        "RTS", "ADC", "PER", "ADC", "STZ", "ADC", "ROR", "ADC", "PLA", "ADC", "ROR", "RTL", "JMP", "ADC", "ROR", "ADC",
        "BVS", "ADC", "ADC", "ADC", "STZ", "ADC", "ROR", "ADC", "SEI", "ADC", "PLY", "TDC", "JMP", "ADC", "ROR", "ADC",
        "BRA", "STA", "BRL", "STA", "STY", "STA", "STX", "STA", "DEY", "BIT", "TXA", "PHB", "STY", "STA", "STX", "STA",
        "BCC", "STA", "STA", "STA", "STY", "STA", "STX", "STA", "TYA", "STA", "TXS", "TXY", "STZ", "STA", "STZ", "STA",
        "LDY", "LDA", "LDX", "LDA", "LDY", "LDA", "LDX", "LDA", "TAY", "LDA", "TAX", "PLB", "LDY", "LDA", "LDX", "LDA",
        "BCS", "LDA", "LDA", "LDA", "LDY", "LDA", "LDX", "LDA", "CLV", "LDA", "TSX", "TYX", "LDY", "LDA", "LDX", "LDA",
        "CPY", "CMP", "REP", "CMP", "CPY", "CMP", "DEC", "CMP", "INY", "CMP", "DEX", "WAI", "CPY", "CMP", "DEC", "CMP",
        "BNE", "CMP", "CMP", "CMP", "PEI", "CMP", "DEC", "CMP", "CLD", "CMP", "PHX", "STP", "JML", "CMP", "DEC", "CMP",
        "CPX", "SBC", "SEP", "SBC", "CPX", "SBC", "INC", "SBC", "INX", "SBC", "NOP", "XBA", "CPX", "SBC", "INC", "SBC",
        "BEQ", "SBC", "SBC", "SBC", "PEA", "SBC", "INC", "SBC", "SED", "SBC", "PLX", "XCE", "JSR", "SBC", "INC", "SBC"
    };

    private static readonly byte[] InstructionLengths = new byte[256]
    {
        2, 2, 2, 2, 2, 2, 2, 2, 1, 2, 1, 1, 3, 3, 3, 3,  // 0x00-0x0F
        2, 2, 2, 2, 2, 2, 2, 2, 1, 2, 1, 1, 3, 3, 3, 3,  // 0x10-0x1F
        3, 2, 4, 2, 2, 2, 2, 2, 1, 2, 1, 1, 3, 3, 3, 3,  // 0x20-0x2F
        2, 2, 2, 2, 2, 2, 2, 2, 1, 2, 1, 1, 3, 3, 3, 3,  // 0x30-0x3F
        1, 2, 2, 2, 3, 2, 2, 2, 1, 2, 1, 1, 3, 3, 3, 3,  // 0x40-0x4F
        2, 2, 2, 2, 2, 2, 2, 2, 1, 2, 1, 1, 3, 3, 3, 3,  // 0x50-0x5F
        1, 2, 3, 2, 2, 2, 2, 2, 1, 2, 1, 1, 3, 3, 3, 3,  // 0x60-0x6F
        2, 2, 2, 2, 2, 2, 2, 2, 1, 2, 1, 1, 3, 3, 3, 3,  // 0x70-0x7F
        2, 2, 3, 2, 2, 2, 2, 2, 1, 2, 1, 1, 3, 3, 3, 3,  // 0x80-0x8F
        2, 2, 2, 2, 2, 2, 2, 2, 1, 2, 1, 1, 3, 3, 3, 3,  // 0x90-0x9F
        2, 2, 2, 2, 2, 2, 2, 2, 1, 2, 1, 1, 3, 3, 3, 3,  // 0xA0-0xAF
        2, 2, 2, 2, 2, 2, 2, 2, 1, 2, 1, 1, 3, 3, 3, 3,  // 0xB0-0xBF
        2, 2, 2, 2, 2, 2, 2, 2, 1, 2, 1, 1, 3, 3, 3, 3,  // 0xC0-0xCF
        2, 2, 2, 2, 2, 2, 2, 2, 1, 2, 1, 1, 3, 3, 3, 3,  // 0xD0-0xDF
        2, 2, 2, 2, 2, 2, 2, 2, 1, 2, 1, 1, 3, 3, 3, 3,  // 0xE0-0xEF
        2, 2, 2, 2, 2, 2, 2, 2, 1, 2, 1, 1, 3, 3, 3, 3   // 0xF0-0xFF
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
                operands.Add(new Operand($"#${flags:X2}", value: flags));
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
                operands.Add(new Operand($"#${flags:X2}", value: flags));
                break;

            case 0xFB: // XCE
                if (baseLength != 1) return DecodeResult.CreateError("Invalid XCE instruction length");
                // WE can't change automatically switch because we don't know the state of carry
                break;
        }

        // Handle addressing modes
        switch (opcode)
        {
            case 0x00: // BRK
                if (baseLength != 2) return DecodeResult.CreateError("Invalid BRK instruction length");
                operands.Add(new Operand($"#${bytes[1]:X2}", value: bytes[1]));
                break;

            case 0xA9: // LDA immediate
                if (baseLength != 2) return DecodeResult.CreateError("Invalid LDA immediate instruction length");
                operands.Add(new Operand($"#${bytes[1]:X2}", value: bytes[1]));
                break;

            case 0x20: // JSR
                if (bytes.Length < 3) return DecodeResult.NeedMoreBytes(3 - bytes.Length);
                if (baseLength != 3) return DecodeResult.CreateError("Invalid JSR instruction length");
                var target = (ulong)bytes[1] + ((ulong)bytes[2] << 8);
                operands.Add(new Operand($"${target:X4}", value: target));
                break;

            case 0x22: // JSL
                if (bytes.Length < 4) return DecodeResult.NeedMoreBytes(4 - bytes.Length);
                if (baseLength != 4) return DecodeResult.CreateError("Invalid JSL instruction length");
                target = (ulong)bytes[1] + ((ulong)bytes[2] << 8) + ((ulong)bytes[3] << 16);
                operands.Add(new Operand($"${target:X6}", value: target));
                break;

            case 0x4C: // JMP
            case 0x5C: // JML
                if (opcode == 0x4C && baseLength != 3) return DecodeResult.CreateError("Invalid JMP instruction length");
                if (opcode == 0x5C && baseLength != 4) return DecodeResult.CreateError("Invalid JML instruction length");
                target = opcode == 0x4C ?
                    (ulong)bytes[1] + ((ulong)bytes[2] << 8) :
                    (ulong)bytes[1] + ((ulong)bytes[2] << 8) + ((ulong)bytes[3] << 16);
                operands.Add(new Operand($"${target:X4}", value: target));
                break;

            case 0x80: // BRA
            case 0x82: // BRL
                if (opcode == 0x80 && baseLength != 2) return DecodeResult.CreateError("Invalid BRA instruction length");
                if (opcode == 0x82 && baseLength != 3) return DecodeResult.CreateError("Invalid BRL instruction length");
                target = opcode == 0x80 ?
                    address + 2 + (ulong)(sbyte)bytes[1] :
                    address + 3 + (ulong)(sbyte)bytes[1] + ((ulong)bytes[2] << 8);
                operands.Add(new Operand($"${target:X4}", value: target));
                break;

            case 0xD0: // BNE
                if (baseLength != 2) return DecodeResult.CreateError("Invalid BNE instruction length");
                target = address + 2 + (ulong)(sbyte)bytes[1];
                operands.Add(new Operand($"${target:X4}", value: target));
                break;

                // Add more addressing mode handling here...
        }

        var instruction = new Instruction(address, mnemonic, operands, instructionBytes);

        // Set branch flags
        instruction.IsBranch = opcode == 0x00 || // BRK
                             opcode == 0x20 || // JSR
                             opcode == 0x22 || // JSL
                             opcode == 0x4C || // JMP
                             opcode == 0x5C || // JML
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

        instruction.IsBasicBlockTerminator = instruction.IsBranch ||
                                            opcode == 0x40 || // RTI
                                            opcode == 0x60 || // RTS
                                            opcode == 0x6B || // RTL
                                            opcode == 0x00;   // BRK

        // Add next addresses
        if (instruction.IsBranch)
        {
            instruction.NextAddresses.Add(address + (ulong)baseLength); // Next instruction
            if (opcode == 0x20 || opcode == 0x22) // JSR/JSL
            {
                if (operands[0].Value == null) return DecodeResult.CreateError("Invalid target address for JSR/JSL");  
                instruction.NextAddresses.Add(operands[0].Value.Value); // Target address
            }
            else if (opcode == 0x4C || opcode == 0x5C) // JMP/JML
            {
                var branchTarget = opcode == 0x4C ?
                    (ulong)bytes[1] + ((ulong)bytes[2] << 8) :
                    (ulong)bytes[1] + ((ulong)bytes[2] << 8) + ((ulong)bytes[3] << 16);
                instruction.NextAddresses.Add(branchTarget);
            }
            else if (opcode == 0x80 || opcode == 0x82) // BRA/BRL
            {
                var branchTarget = opcode == 0x80 ?
                    address + 2 + (ulong)(sbyte)bytes[1] :
                    address + 3 + (ulong)(sbyte)bytes[1] + ((ulong)bytes[2] << 8);
                instruction.NextAddresses.Add(branchTarget);
            }
            else // Conditional branches
            {
                if (bytes.Length < 2) return DecodeResult.NeedMoreBytes(2 - bytes.Length);
                var offset = (sbyte)bytes[1];
                instruction.NextAddresses.Add(address + 2 + (ulong)offset);
            }
        }
        else if (!instruction.IsBasicBlockTerminator)
        {
            instruction.NextAddresses.Add(address + (ulong)baseLength);
        }

        return DecodeResult.CreateSuccess(instruction, baseLength);
    }
}
