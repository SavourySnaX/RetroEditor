using System.Text.Json;
using RetroEditor.Plugins;
/// <summary>
/// Represents the addressing modes supported by the 68000 CPU
/// </summary>
internal enum M68000AddressingMode
{
    DataRegisterDirect,              // Dn
    AddressRegisterDirect,           // An
    AddressRegisterIndirect,         // (An)
    AddressRegisterIndirectPostInc,  // (An)+
    AddressRegisterIndirectPreDec,   // -(An)
    AddressRegisterIndirectDisp,     // d16(An)
    AddressRegisterIndirectIndex,    // d8(An,Xn)
    AbsoluteShort,                   // xxxx.w
    AbsoluteLong,                    // xxxxxxxx.l
    ProgramCounterDisp,              // d16(PC)
    ProgramCounterIndex,             // d8(PC,Xn)
    Immediate,                       // #<data>
    StatusRegister,                  // SR
    ConditionCodeRegister,           // CCR
    UserStackPointer,                // USP
}

/// <summary>
/// Represents the CPU state for the 68000
/// </summary>
internal struct Megadrive68000State : ICpuState
{
    /// <summary>
    /// The 68000 has minimal state that affects instruction decoding
    /// Unlike the 65816, the 68000 doesn't have variable-width instructions based on CPU state
    /// </summary>
    internal bool SupervisorMode { get; set; }

    public ICpuState Clone()
    {
        return new Megadrive68000State
        {
            SupervisorMode = this.SupervisorMode
        };
    }

    public ICpuState Load(Dictionary<string, object> dict)
    {
        if (dict == null)
            throw new ArgumentNullException(nameof(dict));
        
        var supervisorMode = ((JsonElement)dict["SupervisorMode"]).GetBoolean();

        return new Megadrive68000State
        {
            SupervisorMode = supervisorMode
        };
    }

    public Dictionary<string, object> Save()
    {
        var dict = new Dictionary<string, object>
        {
            { "SupervisorMode", SupervisorMode }
        };
        return dict;
    }

    public override string ToString()
    {
        return $"|{(SupervisorMode ? "S" : "U")}|";
    }
}

internal struct M68000RegisterState : ICpuRegisterState
{
    public UInt32[] DataRegisters;  // D0-D7
    public UInt32[] AddressRegisters;  // A0-A7
    public UInt16 SR;  // Status Register
    
    public M68000RegisterState()
    {
        DataRegisters = new UInt32[8];
        AddressRegisters = new UInt32[8];
        SR = 0;
    }

    public ICpuRegisterState Clone()
    {
        var clone = new M68000RegisterState
        {
            DataRegisters = new UInt32[8],
            AddressRegisters = new UInt32[8],
            SR = this.SR
        };
        Array.Copy(this.DataRegisters, clone.DataRegisters, 8);
        Array.Copy(this.AddressRegisters, clone.AddressRegisters, 8);
        return clone;
    }
}

/// <summary>
/// Disassembler for the 68000 CPU
/// </summary>
internal class Megadrive68000Disassembler : DisassemblerBase
{
    internal Megadrive68000Disassembler()
    {
    }

    private byte[] _decodeBuffer = Array.Empty<byte>();

    public override string ArchitectureName => "68000";
    public override MemoryEndian Endianness => MemoryEndian.Big;

    protected override ICpuState CreateInitialState()
    {
        return new Megadrive68000State
        {
            SupervisorMode = true  // Start in supervisor mode
        };
    }

    public override DecodeResult DecodeNext(ReadOnlySpan<byte> bytes, ulong address)
    {
        if (bytes.Length < 2)
            return DecodeResult.NeedMoreBytes(2 - bytes.Length);

        var state = (Megadrive68000State)State;
        
        // Cache the incoming bytes for constructing Instruction.Bytes later
        _decodeBuffer = bytes.ToArray();
        
        // Read the first word (big-endian)
        UInt16 opcode = (UInt16)((bytes[0] << 8) | bytes[1]);
        
        // Decode the instruction based on opcode patterns
        var result = DecodeInstruction(bytes, address, opcode);
        
        return result;
    }

    private DecodeResult DecodeInstruction(ReadOnlySpan<byte> bytes, ulong address, UInt16 opcode)
    {
        // The 68000 instruction set is decoded based on bit patterns
        // High nibble determines major instruction groups
        
        int highNibble = (opcode >> 12) & 0xF;
        
        // Bit manipulation/Move Byte/Move Long/Misc
        if ((opcode & 0xF000) == 0x0000)
        {
            return DecodeBitManipulationOrMove(bytes, address, opcode);
        }
        // Move Byte
        else if ((opcode & 0xF000) == 0x1000)
        {
            return DecodeMoveByte(bytes, address, opcode);
        }
        // Move Long
        else if ((opcode & 0xF000) == 0x2000)
        {
            return DecodeMoveLong(bytes, address, opcode);
        }
        // Move Word
        else if ((opcode & 0xF000) == 0x3000)
        {
            return DecodeMoveWord(bytes, address, opcode);
        }
        // Misc instructions
        else if ((opcode & 0xF000) == 0x4000)
        {
            return DecodeMiscellaneous(bytes, address, opcode);
        }
        // ADDQ/SUBQ/Scc/DBcc
        else if ((opcode & 0xF000) == 0x5000)
        {
            return DecodeAddqSubqSccDbcc(bytes, address, opcode);
        }
        // Bcc/BSR/BRA
        else if ((opcode & 0xF000) == 0x6000)
        {
            return DecodeBranch(bytes, address, opcode);
        }
        // MOVEQ
        else if ((opcode & 0xF100) == 0x7000)
        {
            return DecodeMoveq(bytes, address, opcode);
        }
        // OR/DIV/SBCD
        else if ((opcode & 0xF000) == 0x8000)
        {
            return DecodeOrDivSbcd(bytes, address, opcode);
        }
        // SUB/SUBX
        else if ((opcode & 0xF000) == 0x9000)
        {
            return DecodeSubSubx(bytes, address, opcode);
        }
        // (Unassigned/Line A)
        else if ((opcode & 0xF000) == 0xA000)
        {
            return CreateSimpleInstruction(address, "LINE_A", opcode, 2);
        }
        // CMP/EOR
        else if ((opcode & 0xF000) == 0xB000)
        {
            return DecodeCmpEor(bytes, address, opcode);
        }
        // AND/MUL/ABCD/EXG
        else if ((opcode & 0xF000) == 0xC000)
        {
            return DecodeAndMulAbcdExg(bytes, address, opcode);
        }
        // ADD/ADDX
        else if ((opcode & 0xF000) == 0xD000)
        {
            return DecodeAddAddx(bytes, address, opcode);
        }
        // Shift/Rotate
        else if ((opcode & 0xF000) == 0xE000)
        {
            return DecodeShiftRotate(bytes, address, opcode);
        }
        // (Unassigned/Line F)
        else if ((opcode & 0xF000) == 0xF000)
        {
            return CreateSimpleInstruction(address, "LINE_F", opcode, 2);
        }

        return DecodeResult.CreateError($"Unknown opcode: ${opcode:X4}");
    }

    private DecodeResult DecodeBitManipulationOrMove(ReadOnlySpan<byte> bytes, ulong address, UInt16 opcode)
    {
        // Check for bit manipulation instructions (BTST, BCHG, BCLR, BSET)
        // Pattern: 0000DDD1TTMMMRRR where DDD=data reg, TT=op type, MMM/RRR=EA
        if ((opcode & 0xF100) == 0x0100)
        {
            // Dynamic bit manipulation
            int bitNum = (opcode >> 9) & 7;
            int opType = (opcode >> 6) & 3;
            string mnemonic = opType switch
            {
                0 => "BTST",
                1 => "BCHG",
                2 => "BCLR",
                3 => "BSET",
                _ => "???"
            };
            
            var (ea, eaSize) = DecodeEffectiveAddress(bytes.Slice(2), opcode & 0x3F, SizeCode.Byte);
            if (ea == null) return DecodeResult.NeedMoreBytes(2);
            
            var operands = new List<IOperand>
            {
                new OM68000_DataRegister(bitNum),
                ea
            };
            
            return CreateInstruction(address, mnemonic, operands, 2 + eaSize);
        }
        
        // Check for static bit manipulation (BTST, BCHG, BCLR, BSET with immediate)
        if ((opcode & 0xFFC0) == 0x0800 || (opcode & 0xFFC0) == 0x0840 || 
            (opcode & 0xFFC0) == 0x0880 || (opcode & 0xFFC0) == 0x08C0)
        {
            string mnemonic = ((opcode >> 6) & 3) switch
            {
                0 => "BTST",
                1 => "BCHG",
                2 => "BCLR",
                3 => "BSET",
                _ => "???"
            };
            
            if (bytes.Length < 4) return DecodeResult.NeedMoreBytes(4 - bytes.Length);
            
            byte bitNum = bytes[3];
            var (ea, eaSize) = DecodeEffectiveAddress(bytes.Slice(4), opcode & 0x3F, SizeCode.Byte);
            if (ea == null) return DecodeResult.NeedMoreBytes(4);
            
            var operands = new List<IOperand>
            {
                new OM68000_Immediate(bitNum, SizeCode.Byte),
                ea
            };
            
            return CreateInstruction(address, mnemonic, operands, 4 + eaSize);
        }
        
        // ORI, ANDI, SUBI, ADDI, EORI, CMPI
        if ((opcode & 0xFF00) == 0x0000 || (opcode & 0xFF00) == 0x0200 ||
            (opcode & 0xFF00) == 0x0400 || (opcode & 0xFF00) == 0x0600 ||
            (opcode & 0xFF00) == 0x0A00 || (opcode & 0xFF00) == 0x0C00)
        {
            string mnemonic = ((opcode >> 9) & 7) switch
            {
                0 => "ORI",
                1 => "ANDI",
                2 => "SUBI",
                3 => "ADDI",
                5 => "EORI",
                6 => "CMPI",
                _ => "???"
            };
            
            var size = DecodeSizeCode((opcode >> 6) & 3);
            int immSize = size == SizeCode.Long ? 6 : 4;
            
            if (bytes.Length < immSize) return DecodeResult.NeedMoreBytes(immSize - bytes.Length);
            
            UInt32 immediate = size == SizeCode.Long 
                ? (UInt32)((bytes[2] << 24) | (bytes[3] << 16) | (bytes[4] << 8) | bytes[5])
                : (UInt32)((bytes[2] << 8) | bytes[3]);
                
            var (ea, eaSize) = DecodeEffectiveAddress(bytes.Slice(immSize), opcode & 0x3F, size);
            if (ea == null) return DecodeResult.NeedMoreBytes(immSize);
            
            var operands = new List<IOperand>
            {
                new OM68000_Immediate(immediate, size),
                ea
            };
            
            string suffix = size == SizeCode.Byte ? ".B" : size == SizeCode.Word ? ".W" : ".L";
            return CreateInstruction(address, mnemonic + suffix, operands, immSize + eaSize);
        }
        
        // MOVEP
        if ((opcode & 0xF138) == 0x0108)
        {
            if (bytes.Length < 4) return DecodeResult.NeedMoreBytes(4 - bytes.Length);
            
            int dataReg = (opcode >> 9) & 7;
            int addrReg = opcode & 7;
            bool isWord = ((opcode >> 6) & 1) == 0;
            bool toMemory = ((opcode >> 7) & 1) == 1;
            
            Int16 displacement = (Int16)((bytes[2] << 8) | bytes[3]);
            
            var operands = toMemory 
                ? new List<IOperand>
                {
                    new OM68000_DataRegister(dataReg),
                    new OM68000_AddressDisplacement(addrReg, displacement)
                }
                : new List<IOperand>
                {
                    new OM68000_AddressDisplacement(addrReg, displacement),
                    new OM68000_DataRegister(dataReg)
                };
            
            return CreateInstruction(address, isWord ? "MOVEP.W" : "MOVEP.L", operands, 4);
        }
        
        return DecodeResult.CreateError($"Unknown instruction in group 0: ${opcode:X4}");
    }

    private DecodeResult DecodeMoveByte(ReadOnlySpan<byte> bytes, ulong address, UInt16 opcode)
    {
        return DecodeMoveInstruction(bytes, address, opcode, SizeCode.Byte);
    }

    private DecodeResult DecodeMoveWord(ReadOnlySpan<byte> bytes, ulong address, UInt16 opcode)
    {
        return DecodeMoveInstruction(bytes, address, opcode, SizeCode.Word);
    }

    private DecodeResult DecodeMoveLong(ReadOnlySpan<byte> bytes, ulong address, UInt16 opcode)
    {
        return DecodeMoveInstruction(bytes, address, opcode, SizeCode.Long);
    }

    private DecodeResult DecodeMoveInstruction(ReadOnlySpan<byte> bytes, ulong address, UInt16 opcode, SizeCode size)
    {
        // Source EA (bits 0-5)
        var (srcEa, srcSize) = DecodeEffectiveAddress(bytes.Slice(2), opcode & 0x3F, size);
        if (srcEa == null) return DecodeResult.NeedMoreBytes(2);
        
        // Destination EA (bits 6-11, but rearranged)
        // Destination register is in bits 9-11, mode is in bits 6-8
        int destReg = (opcode >> 9) & 7;
        int destMode = (opcode >> 6) & 7;
        int destEaField = (destMode << 3) | destReg;
        
        var (destEa, destSize) = DecodeEffectiveAddress(bytes.Slice(2 + srcSize), destEaField, size);
        if (destEa == null) return DecodeResult.NeedMoreBytes(2 + srcSize);
        
        var operands = new List<IOperand> { srcEa, destEa };
        
        // Check if this is MOVEA (destination mode = 001 = address register direct)
        string mnemonic;
        if (destMode == 1 && size != SizeCode.Byte)
        {
            // MOVEA - only valid for word and long sizes
            mnemonic = "MOVEA";
        }
        else
        {
            mnemonic = "MOVE";
        }
        
        string suffix = size == SizeCode.Byte ? ".B" : size == SizeCode.Word ? ".W" : ".L";
        
        return CreateInstruction(address, mnemonic + suffix, operands, 2 + srcSize + destSize);
    }

    private DecodeResult DecodeMiscellaneous(ReadOnlySpan<byte> bytes, ulong address, UInt16 opcode)
    {
        // NEGX, CLR, NEG, NOT
        if ((opcode & 0xFF00) == 0x4000 || (opcode & 0xFF00) == 0x4200 ||
            (opcode & 0xFF00) == 0x4400 || (opcode & 0xFF00) == 0x4600)
        {
            string mnemonic = ((opcode >> 9) & 7) switch
            {
                0 => "NEGX",
                1 => "CLR",
                2 => "NEG",
                3 => "NOT",
                _ => "???"
            };
            
            var size = DecodeSizeCode((opcode >> 6) & 3);
            var (ea, eaSize) = DecodeEffectiveAddress(bytes.Slice(2), opcode & 0x3F, size);
            if (ea == null) return DecodeResult.NeedMoreBytes(2);
            
            string suffix = size == SizeCode.Byte ? ".B" : size == SizeCode.Word ? ".W" : ".L";
            var operands = new List<IOperand> { ea };
            
            return CreateInstruction(address, mnemonic + suffix, operands, 2 + eaSize);
        }
        
        // NBCD, SWAP, PEA
        if ((opcode & 0xFFC0) == 0x4800)
        {
            var (ea, eaSize) = DecodeEffectiveAddress(bytes.Slice(2), opcode & 0x3F, SizeCode.Byte);
            if (ea == null) return DecodeResult.NeedMoreBytes(2);
            
            var operands = new List<IOperand> { ea };
            return CreateInstruction(address, "NBCD", operands, 2 + eaSize);
        }
        
        if ((opcode & 0xFFF8) == 0x4840)
        {
            int reg = opcode & 7;
            var operands = new List<IOperand> { new OM68000_DataRegister(reg) };
            return CreateInstruction(address, "SWAP", operands, 2);
        }
        
        if ((opcode & 0xFFC0) == 0x4840)
        {
            var (ea, eaSize) = DecodeEffectiveAddress(bytes.Slice(2), opcode & 0x3F, SizeCode.Long);
            if (ea == null) return DecodeResult.NeedMoreBytes(2);
            
            var operands = new List<IOperand> { ea };
            return CreateInstruction(address, "PEA", operands, 2 + eaSize);
        }
        
        // EXT
        if ((opcode & 0xFEB8) == 0x4880)
        {
            bool isLong = ((opcode >> 6) & 1) == 1;
            int reg = opcode & 7;
            var operands = new List<IOperand> { new OM68000_DataRegister(reg) };
            return CreateInstruction(address, isLong ? "EXT.L" : "EXT.W", operands, 2);
        }
        
        // ILLEGAL - check before TAS since 0x4AFC matches TAS pattern (0x4AFC & 0xFFC0 = 0x4AC0)
        if (opcode == 0x4AFC)
        {
            return CreateSimpleInstruction(address, "ILLEGAL", opcode, 2, isBranch: true, isTerminator: true);
        }
        
        // TAS - check before TST since it's more specific (pattern 0100101011xxxxxx)
        if ((opcode & 0xFFC0) == 0x4AC0)
        {
            var (ea, eaSize) = DecodeEffectiveAddress(bytes.Slice(2), opcode & 0x3F, SizeCode.Byte);
            if (ea == null) return DecodeResult.NeedMoreBytes(2);
            
            var operands = new List<IOperand> { ea };
            return CreateInstruction(address, "TAS", operands, 2 + eaSize);
        }
        
        // TST
        if ((opcode & 0xFF00) == 0x4A00)
        {
            var size = DecodeSizeCode((opcode >> 6) & 3);
            var (ea, eaSize) = DecodeEffectiveAddress(bytes.Slice(2), opcode & 0x3F, size);
            if (ea == null) return DecodeResult.NeedMoreBytes(2);
            
            string suffix = size == SizeCode.Byte ? ".B" : size == SizeCode.Word ? ".W" : ".L";
            var operands = new List<IOperand> { ea };
            
            return CreateInstruction(address, "TST" + suffix, operands, 2 + eaSize);
        }
        
        // TRAP
        if ((opcode & 0xFFF0) == 0x4E40)
        {
            int vector = opcode & 0xF;
            var operands = new List<IOperand> { new OM68000_Immediate((UInt32)vector, SizeCode.Byte) };
            return CreateInstruction(address, "TRAP", operands, 2, isBranch: true, isTerminator: false);
        }
        
        // LINK, UNLK
        if ((opcode & 0xFFF8) == 0x4E50)
        {
            if (bytes.Length < 4) return DecodeResult.NeedMoreBytes(4 - bytes.Length);
            
            int reg = opcode & 7;
            Int16 displacement = (Int16)((bytes[2] << 8) | bytes[3]);
            
            var operands = new List<IOperand>
            {
                new OM68000_AddressRegister(reg),
                new OM68000_Immediate((UInt32)(UInt16)displacement, SizeCode.Word)
            };
            
            return CreateInstruction(address, "LINK", operands, 4);
        }
        
        if ((opcode & 0xFFF8) == 0x4E58)
        {
            int reg = opcode & 7;
            var operands = new List<IOperand> { new OM68000_AddressRegister(reg) };
            return CreateInstruction(address, "UNLK", operands, 2);
        }
        
        // MOVE USP
        if ((opcode & 0xFFF0) == 0x4E60)
        {
            int reg = opcode & 7;
            bool toUsp = ((opcode >> 3) & 1) == 0;
            
            var operands = toUsp
                ? new List<IOperand> { new OM68000_AddressRegister(reg), new OM68000_StatusRegister("USP") }
                : new List<IOperand> { new OM68000_StatusRegister("USP"), new OM68000_AddressRegister(reg) };
            
            return CreateInstruction(address, "MOVE", operands, 2);
        }
        
        // RESET, NOP, STOP, RTE, RTS, TRAPV, RTR
        if (opcode == 0x4E70) return CreateSimpleInstruction(address, "RESET", opcode, 2);
        if (opcode == 0x4E71) return CreateSimpleInstruction(address, "NOP", opcode, 2);
        if (opcode == 0x4E72)
        {
            if (bytes.Length < 4) return DecodeResult.NeedMoreBytes(4 - bytes.Length);
            UInt16 immediate = (UInt16)((bytes[2] << 8) | bytes[3]);
            var operands = new List<IOperand> { new OM68000_Immediate(immediate, SizeCode.Word) };
            return CreateInstruction(address, "STOP", operands, 4);
        }
        if (opcode == 0x4E73) return CreateSimpleInstruction(address, "RTE", opcode, 2, isTerminator: true);
        if (opcode == 0x4E75) return CreateSimpleInstruction(address, "RTS", opcode, 2, isTerminator: true);
        if (opcode == 0x4E76) return CreateSimpleInstruction(address, "TRAPV", opcode, 2);
        if (opcode == 0x4E77) return CreateSimpleInstruction(address, "RTR", opcode, 2, isTerminator: true);
        
        // JSR, JMP
        if ((opcode & 0xFFC0) == 0x4E80)
        {
            var (ea, eaSize) = DecodeEffectiveAddress(bytes.Slice(2), opcode & 0x3F, SizeCode.Long);
            if (ea == null) return DecodeResult.NeedMoreBytes(2);
            
            var operands = new List<IOperand> { ea };
            return CreateInstruction(address, "JSR", operands, 2 + eaSize, isBranch: true, isTerminator: true, nextAddress: operands[0].Value);
        }
        
        if ((opcode & 0xFFC0) == 0x4EC0)
        {
            var (ea, eaSize) = DecodeEffectiveAddress(bytes.Slice(2), opcode & 0x3F, SizeCode.Long);
            if (ea == null) return DecodeResult.NeedMoreBytes(2);
            
            var operands = new List<IOperand> { ea };
            return CreateInstruction(address, "JMP", operands, 2 + eaSize, isBranch: true, isTerminator: true, nextAddress: operands[0].Value);
        }
        
        // MOVEM
        if ((opcode & 0xFB80) == 0x4880)
        {
            if (bytes.Length < 4) return DecodeResult.NeedMoreBytes(4 - bytes.Length);
            
            bool isLong = ((opcode >> 6) & 1) == 1;
            bool toMemory = ((opcode >> 10) & 1) == 0;
            UInt16 registerMask = (UInt16)((bytes[2] << 8) | bytes[3]);
            
            var (ea, eaSize) = DecodeEffectiveAddress(bytes.Slice(4), opcode & 0x3F, isLong ? SizeCode.Long : SizeCode.Word);
            if (ea == null) return DecodeResult.NeedMoreBytes(4);
            
            var operands = toMemory
                ? new List<IOperand> { new OM68000_RegisterList(registerMask), ea }
                : new List<IOperand> { ea, new OM68000_RegisterList(registerMask) };
            
            string suffix = isLong ? ".L" : ".W";
            return CreateInstruction(address, "MOVEM" + suffix, operands, 4 + eaSize);
        }
        
        // LEA
        if ((opcode & 0xF1C0) == 0x41C0)
        {
            int reg = (opcode >> 9) & 7;
            var (ea, eaSize) = DecodeEffectiveAddress(bytes.Slice(2), opcode & 0x3F, SizeCode.Long);
            if (ea == null) return DecodeResult.NeedMoreBytes(2);
            
            var operands = new List<IOperand> { ea, new OM68000_AddressRegister(reg) };
            return CreateInstruction(address, "LEA", operands, 2 + eaSize);
        }
        
        // CHK
        if ((opcode & 0xF1C0) == 0x4180)
        {
            int reg = (opcode >> 9) & 7;
            var (ea, eaSize) = DecodeEffectiveAddress(bytes.Slice(2), opcode & 0x3F, SizeCode.Word);
            if (ea == null) return DecodeResult.NeedMoreBytes(2);
            
            var operands = new List<IOperand> { ea, new OM68000_DataRegister(reg) };
            return CreateInstruction(address, "CHK", operands, 2 + eaSize);
        }
        
        return DecodeResult.CreateError($"Unknown miscellaneous instruction: ${opcode:X4}");
    }

    private DecodeResult DecodeAddqSubqSccDbcc(ReadOnlySpan<byte> bytes, ulong address, UInt16 opcode)
    {
        // ADDQ/SUBQ (bits 12-15 = 0101, bits 6-7 != 11)
        int sizeField = (opcode >> 6) & 3;
        if ((opcode & 0xF000) == 0x5000 && sizeField != 3)
        {
            bool isSub = ((opcode >> 8) & 1) == 1;
            int data = (opcode >> 9) & 7;
            if (data == 0) data = 8;
            var size = DecodeSizeCode(sizeField);
            
            var (ea, eaSize) = DecodeEffectiveAddress(bytes.Slice(2), opcode & 0x3F, size);
            if (ea == null) return DecodeResult.NeedMoreBytes(2);
            
            var operands = new List<IOperand>
            {
                new OM68000_Immediate((UInt32)data, SizeCode.Byte),
                ea
            };
            
            string mnemonic = isSub ? "SUBQ" : "ADDQ";
            string suffix = size == SizeCode.Byte ? ".B" : size == SizeCode.Word ? ".W" : ".L";
            
            return CreateInstruction(address, mnemonic + suffix, operands, 2 + eaSize);
        }
        
        // DBcc (check before Scc since it's more specific)
        if ((opcode & 0xF0F8) == 0x50C8)
        {
            if (bytes.Length < 4) return DecodeResult.NeedMoreBytes(4 - bytes.Length);
            
            int condition = (opcode >> 8) & 0xF;
            int reg = opcode & 7;
            Int16 displacement = (Int16)((bytes[2] << 8) | bytes[3]);
            UInt64 target = (UInt64)((long)address + 2 + displacement);
            
            var operands = new List<IOperand>
            {
                new OM68000_DataRegister(reg),
                new OM68000_BranchTarget(target)
            };
            
            string mnemonic = "DB" + GetConditionCode(condition);
            return CreateInstruction(address, mnemonic, operands, 4, isBranch: true, isTerminator: false, nextAddress: target);
        }
        
        // Scc
        if ((opcode & 0xF0C0) == 0x50C0)
        {
            int condition = (opcode >> 8) & 0xF;
            var (ea, eaSize) = DecodeEffectiveAddress(bytes.Slice(2), opcode & 0x3F, SizeCode.Byte);
            if (ea == null) return DecodeResult.NeedMoreBytes(2);
            
            var operands = new List<IOperand> { ea };
            string mnemonic = "S" + GetConditionCode(condition);
            
            return CreateInstruction(address, mnemonic, operands, 2 + eaSize);
        }
        
        return DecodeResult.CreateError($"Unknown ADDQ/SUBQ/Scc/DBcc instruction: ${opcode:X4}");
    }

    private DecodeResult DecodeBranch(ReadOnlySpan<byte> bytes, ulong address, UInt16 opcode)
    {
        int condition = (opcode >> 8) & 0xF;
        sbyte displacement = (sbyte)(opcode & 0xFF);
        
        int instrSize = 2;
        long actualDisplacement = displacement;
        
        if (displacement == 0)
        {
            // 16-bit displacement
            if (bytes.Length < 4) return DecodeResult.NeedMoreBytes(4 - bytes.Length);
            actualDisplacement = (Int16)((bytes[2] << 8) | bytes[3]);
            instrSize = 4;
        }
        
        UInt64 target = (UInt64)((long)address + 2 + actualDisplacement);
        
        var operands = new List<IOperand> { new OM68000_BranchTarget(target) };
        
        string mnemonic = condition == 0 ? "BRA" : condition == 1 ? "BSR" : "B" + GetConditionCode(condition);
        bool isTerminator = condition == 0;  // BRA is a terminator
        bool isBranch = true;
        
        return CreateInstruction(address, mnemonic, operands, instrSize, isBranch: isBranch, isTerminator: isTerminator, nextAddress: target);
    }

    private DecodeResult DecodeMoveq(ReadOnlySpan<byte> bytes, ulong address, UInt16 opcode)
    {
        int reg = (opcode >> 9) & 7;
        sbyte data = (sbyte)(opcode & 0xFF);
        
        var operands = new List<IOperand>
        {
            new OM68000_Immediate((UInt32)(Int32)data, SizeCode.Byte),
            new OM68000_DataRegister(reg)
        };
        
        return CreateInstruction(address, "MOVEQ", operands, 2);
    }

    private DecodeResult DecodeOrDivSbcd(ReadOnlySpan<byte> bytes, ulong address, UInt16 opcode)
    {
        // DIVU, DIVS
        if ((opcode & 0xF1C0) == 0x80C0 || (opcode & 0xF1C0) == 0x81C0)
        {
            bool isSigned = ((opcode >> 8) & 1) == 1;
            int reg = (opcode >> 9) & 7;
            
            var (ea, eaSize) = DecodeEffectiveAddress(bytes.Slice(2), opcode & 0x3F, SizeCode.Word);
            if (ea == null) return DecodeResult.NeedMoreBytes(2);
            
            var operands = new List<IOperand> { ea, new OM68000_DataRegister(reg) };
            string mnemonic = isSigned ? "DIVS" : "DIVU";
            
            return CreateInstruction(address, mnemonic, operands, 2 + eaSize);
        }
        
        // SBCD
        if ((opcode & 0xF1F0) == 0x8100)
        {
            bool isMemory = ((opcode >> 3) & 1) == 1;
            int regX = (opcode >> 9) & 7;
            int regY = opcode & 7;
            
            var operands = isMemory
                ? new List<IOperand>
                {
                    new OM68000_AddressPreDec(regY),
                    new OM68000_AddressPreDec(regX)
                }
                : new List<IOperand>
                {
                    new OM68000_DataRegister(regY),
                    new OM68000_DataRegister(regX)
                };
            
            return CreateInstruction(address, "SBCD", operands, 2);
        }
        
        // OR
        int directionOr = (opcode >> 8) & 1;
        int regOr = (opcode >> 9) & 7;
        var sizeOr = DecodeSizeCode((opcode >> 6) & 3);
        
        var (eaOr, eaSizeOr) = DecodeEffectiveAddress(bytes.Slice(2), opcode & 0x3F, sizeOr);
        if (eaOr == null) return DecodeResult.NeedMoreBytes(2);
        
        var operandsOr = directionOr == 0
            ? new List<IOperand> { eaOr, new OM68000_DataRegister(regOr) }
            : new List<IOperand> { new OM68000_DataRegister(regOr), eaOr };
        
        string suffixOr = sizeOr == SizeCode.Byte ? ".B" : sizeOr == SizeCode.Word ? ".W" : ".L";
        return CreateInstruction(address, "OR" + suffixOr, operandsOr, 2 + eaSizeOr);
    }

    private DecodeResult DecodeSubSubx(ReadOnlySpan<byte> bytes, ulong address, UInt16 opcode)
    {
        int reg = (opcode >> 9) & 7;
        var size = DecodeSizeCode((opcode >> 6) & 3);
        int direction = (opcode >> 8) & 1;
        
        // SUBA
        if ((opcode & 0xF0C0) == 0x90C0 || (opcode & 0xF0C0) == 0x91C0)
        {
            bool isLong = ((opcode >> 8) & 1) == 1;
            var (ea, eaSize) = DecodeEffectiveAddress(bytes.Slice(2), opcode & 0x3F, isLong ? SizeCode.Long : SizeCode.Word);
            if (ea == null) return DecodeResult.NeedMoreBytes(2);
            
            var operands = new List<IOperand> { ea, new OM68000_AddressRegister(reg) };
            string suffix = isLong ? ".L" : ".W";
            
            return CreateInstruction(address, "SUBA" + suffix, operands, 2 + eaSize);
        }
        
        // SUBX
        if ((opcode & 0xF130) == 0x9100)
        {
            bool isMemory = ((opcode >> 3) & 1) == 1;
            int regY = opcode & 7;
            
            var operands = isMemory
                ? new List<IOperand>
                {
                    new OM68000_AddressPreDec(regY),
                    new OM68000_AddressPreDec(reg)
                }
                : new List<IOperand>
                {
                    new OM68000_DataRegister(regY),
                    new OM68000_DataRegister(reg)
                };
            
            string suffix = size == SizeCode.Byte ? ".B" : size == SizeCode.Word ? ".W" : ".L";
            return CreateInstruction(address, "SUBX" + suffix, operands, 2);
        }
        
        // SUB
        var (eaSub, eaSizeSub) = DecodeEffectiveAddress(bytes.Slice(2), opcode & 0x3F, size);
        if (eaSub == null) return DecodeResult.NeedMoreBytes(2);
        
        var operandsSub = direction == 0
            ? new List<IOperand> { eaSub, new OM68000_DataRegister(reg) }
            : new List<IOperand> { new OM68000_DataRegister(reg), eaSub };
        
        string suffixSub = size == SizeCode.Byte ? ".B" : size == SizeCode.Word ? ".W" : ".L";
        return CreateInstruction(address, "SUB" + suffixSub, operandsSub, 2 + eaSizeSub);
    }

    private DecodeResult DecodeCmpEor(ReadOnlySpan<byte> bytes, ulong address, UInt16 opcode)
    {
        int reg = (opcode >> 9) & 7;
        var size = DecodeSizeCode((opcode >> 6) & 3);
        
        // CMPA
        if ((opcode & 0xF0C0) == 0xB0C0 || (opcode & 0xF0C0) == 0xB1C0)
        {
            bool isLong = ((opcode >> 8) & 1) == 1;
            var (ea, eaSize) = DecodeEffectiveAddress(bytes.Slice(2), opcode & 0x3F, isLong ? SizeCode.Long : SizeCode.Word);
            if (ea == null) return DecodeResult.NeedMoreBytes(2);
            
            var operands = new List<IOperand> { ea, new OM68000_AddressRegister(reg) };
            string suffix = isLong ? ".L" : ".W";
            
            return CreateInstruction(address, "CMPA" + suffix, operands, 2 + eaSize);
        }
        
        // CMPM - check before EOR (pattern 1011XXX1SS001XXX, bits 3-5 = 001)
        if ((opcode & 0xF138) == 0xB108)
        {
            int regY = opcode & 7;
            var operands = new List<IOperand>
            {
                new OM68000_AddressPostInc(regY),
                new OM68000_AddressPostInc(reg)
            };
            
            string suffix = size == SizeCode.Byte ? ".B" : size == SizeCode.Word ? ".W" : ".L";
            return CreateInstruction(address, "CMPM" + suffix, operands, 2);
        }
        
        // EOR - note: EOR does not support An (address register direct) as destination
        if ((opcode & 0xF100) == 0xB100)
        {
            var (ea, eaSize) = DecodeEffectiveAddress(bytes.Slice(2), opcode & 0x3F, size);
            if (ea == null) return DecodeResult.NeedMoreBytes(2);
            
            var operands = new List<IOperand> { new OM68000_DataRegister(reg), ea };
            string suffix = size == SizeCode.Byte ? ".B" : size == SizeCode.Word ? ".W" : ".L";
            
            return CreateInstruction(address, "EOR" + suffix, operands, 2 + eaSize);
        }
        
        // CMP
        var (eaCmp, eaSizeCmp) = DecodeEffectiveAddress(bytes.Slice(2), opcode & 0x3F, size);
        if (eaCmp == null) return DecodeResult.NeedMoreBytes(2);
        
        var operandsCmp = new List<IOperand> { eaCmp, new OM68000_DataRegister(reg) };
        string suffixCmp = size == SizeCode.Byte ? ".B" : size == SizeCode.Word ? ".W" : ".L";
        
        return CreateInstruction(address, "CMP" + suffixCmp, operandsCmp, 2 + eaSizeCmp);
    }

    private DecodeResult DecodeAndMulAbcdExg(ReadOnlySpan<byte> bytes, ulong address, UInt16 opcode)
    {
        // MULU, MULS
        if ((opcode & 0xF1C0) == 0xC0C0 || (opcode & 0xF1C0) == 0xC1C0)
        {
            bool isSigned = ((opcode >> 8) & 1) == 1;
            int reg = (opcode >> 9) & 7;
            
            var (ea, eaSize) = DecodeEffectiveAddress(bytes.Slice(2), opcode & 0x3F, SizeCode.Word);
            if (ea == null) return DecodeResult.NeedMoreBytes(2);
            
            var operands = new List<IOperand> { ea, new OM68000_DataRegister(reg) };
            string mnemonic = isSigned ? "MULS" : "MULU";
            
            return CreateInstruction(address, mnemonic, operands, 2 + eaSize);
        }
        
        // ABCD
        if ((opcode & 0xF1F0) == 0xC100)
        {
            bool isMemory = ((opcode >> 3) & 1) == 1;
            int regX = (opcode >> 9) & 7;
            int regY = opcode & 7;
            
            var operands = isMemory
                ? new List<IOperand>
                {
                    new OM68000_AddressPreDec(regY),
                    new OM68000_AddressPreDec(regX)
                }
                : new List<IOperand>
                {
                    new OM68000_DataRegister(regY),
                    new OM68000_DataRegister(regX)
                };
            
            return CreateInstruction(address, "ABCD", operands, 2);
        }
        
        // EXG
        if ((opcode & 0xF130) == 0xC100 && ((opcode >> 3) & 0x1F) >= 8)
        {
            int regX = (opcode >> 9) & 7;
            int regY = opcode & 7;
            int mode = (opcode >> 3) & 0x1F;
            
            var operands = mode switch
            {
                0x08 => new List<IOperand> { new OM68000_DataRegister(regX), new OM68000_DataRegister(regY) },
                0x09 => new List<IOperand> { new OM68000_AddressRegister(regX), new OM68000_AddressRegister(regY) },
                0x11 => new List<IOperand> { new OM68000_DataRegister(regX), new OM68000_AddressRegister(regY) },
                _ => new List<IOperand>()
            };
            
            if (operands.Count == 0)
                return DecodeResult.CreateError($"Invalid EXG mode: {mode}");
            
            return CreateInstruction(address, "EXG", operands, 2);
        }
        
        // AND
        int directionAnd = (opcode >> 8) & 1;
        int regAnd = (opcode >> 9) & 7;
        var sizeAnd = DecodeSizeCode((opcode >> 6) & 3);
        
        var (eaAnd, eaSizeAnd) = DecodeEffectiveAddress(bytes.Slice(2), opcode & 0x3F, sizeAnd);
        if (eaAnd == null) return DecodeResult.NeedMoreBytes(2);
        
        var operandsAnd = directionAnd == 0
            ? new List<IOperand> { eaAnd, new OM68000_DataRegister(regAnd) }
            : new List<IOperand> { new OM68000_DataRegister(regAnd), eaAnd };
        
        string suffixAnd = sizeAnd == SizeCode.Byte ? ".B" : sizeAnd == SizeCode.Word ? ".W" : ".L";
        return CreateInstruction(address, "AND" + suffixAnd, operandsAnd, 2 + eaSizeAnd);
    }

    private DecodeResult DecodeAddAddx(ReadOnlySpan<byte> bytes, ulong address, UInt16 opcode)
    {
        int reg = (opcode >> 9) & 7;
        var size = DecodeSizeCode((opcode >> 6) & 3);
        int direction = (opcode >> 8) & 1;
        
        // ADDA
        if ((opcode & 0xF0C0) == 0xD0C0 || (opcode & 0xF0C0) == 0xD1C0)
        {
            bool isLong = ((opcode >> 8) & 1) == 1;
            var (ea, eaSize) = DecodeEffectiveAddress(bytes.Slice(2), opcode & 0x3F, isLong ? SizeCode.Long : SizeCode.Word);
            if (ea == null) return DecodeResult.NeedMoreBytes(2);
            
            var operands = new List<IOperand> { ea, new OM68000_AddressRegister(reg) };
            string suffix = isLong ? ".L" : ".W";
            
            return CreateInstruction(address, "ADDA" + suffix, operands, 2 + eaSize);
        }
        
        // ADDX
        if ((opcode & 0xF130) == 0xD100)
        {
            bool isMemory = ((opcode >> 3) & 1) == 1;
            int regY = opcode & 7;
            
            var operands = isMemory
                ? new List<IOperand>
                {
                    new OM68000_AddressPreDec(regY),
                    new OM68000_AddressPreDec(reg)
                }
                : new List<IOperand>
                {
                    new OM68000_DataRegister(regY),
                    new OM68000_DataRegister(reg)
                };
            
            string suffix = size == SizeCode.Byte ? ".B" : size == SizeCode.Word ? ".W" : ".L";
            return CreateInstruction(address, "ADDX" + suffix, operands, 2);
        }
        
        // ADD
        var (eaAdd, eaSizeAdd) = DecodeEffectiveAddress(bytes.Slice(2), opcode & 0x3F, size);
        if (eaAdd == null) return DecodeResult.NeedMoreBytes(2);
        
        var operandsAdd = direction == 0
            ? new List<IOperand> { eaAdd, new OM68000_DataRegister(reg) }
            : new List<IOperand> { new OM68000_DataRegister(reg), eaAdd };
        
        string suffixAdd = size == SizeCode.Byte ? ".B" : size == SizeCode.Word ? ".W" : ".L";
        return CreateInstruction(address, "ADD" + suffixAdd, operandsAdd, 2 + eaSizeAdd);
    }

    private DecodeResult DecodeShiftRotate(ReadOnlySpan<byte> bytes, ulong address, UInt16 opcode)
    {
        // Memory shifts/rotates
        if ((opcode & 0xFEC0) == 0xE0C0 || (opcode & 0xFEC0) == 0xE2C0 ||
            (opcode & 0xFEC0) == 0xE4C0 || (opcode & 0xFEC0) == 0xE6C0)
        {
            int typeMem = (opcode >> 9) & 3;
            int directionMem = (opcode >> 8) & 1;
            
            string mnemonic = typeMem switch
            {
                0 => directionMem == 0 ? "ASR" : "ASL",
                1 => directionMem == 0 ? "LSR" : "LSL",
                2 => directionMem == 0 ? "ROXR" : "ROXL",
                3 => directionMem == 0 ? "ROR" : "ROL",
                _ => "???"
            };
            
            var (ea, eaSize) = DecodeEffectiveAddress(bytes.Slice(2), opcode & 0x3F, SizeCode.Word);
            if (ea == null) return DecodeResult.NeedMoreBytes(2);
            
            var operands = new List<IOperand> { ea };
            return CreateInstruction(address, mnemonic, operands, 2 + eaSize);
        }
        
        // Register shifts/rotates
        int count = (opcode >> 9) & 7;
        bool isRegister = ((opcode >> 5) & 1) == 1;
        int direction = (opcode >> 8) & 1;
        var size = DecodeSizeCode((opcode >> 6) & 3);
        int type = (opcode >> 3) & 3;
        int reg = opcode & 7;
        
        string mnemonicReg = type switch
        {
            0 => direction == 0 ? "ASR" : "ASL",
            1 => direction == 0 ? "LSR" : "LSL",
            2 => direction == 0 ? "ROXR" : "ROXL",
            3 => direction == 0 ? "ROR" : "ROL",
            _ => "???"
        };
        
        var operandsReg = isRegister
            ? new List<IOperand>
            {
                new OM68000_DataRegister(count),
                new OM68000_DataRegister(reg)
            }
            : new List<IOperand>
            {
                new OM68000_Immediate((UInt32)(count == 0 ? 8 : count), SizeCode.Byte),
                new OM68000_DataRegister(reg)
            };
        
        string suffixReg = size == SizeCode.Byte ? ".B" : size == SizeCode.Word ? ".W" : ".L";
        return CreateInstruction(address, mnemonicReg + suffixReg, operandsReg, 2);
    }

    private (IOperand? ea, int size) DecodeEffectiveAddress(ReadOnlySpan<byte> bytes, int eaField, SizeCode size)
    {
        int mode = (eaField >> 3) & 7;
        int reg = eaField & 7;
        
        switch (mode)
        {
            case 0: // Data register direct
                return (new OM68000_DataRegister(reg), 0);
            
            case 1: // Address register direct
                return (new OM68000_AddressRegister(reg), 0);
            
            case 2: // Address register indirect
                return (new OM68000_AddressIndirect(reg), 0);
            
            case 3: // Address register indirect with postincrement
                return (new OM68000_AddressPostInc(reg), 0);
            
            case 4: // Address register indirect with predecrement
                return (new OM68000_AddressPreDec(reg), 0);
            
            case 5: // Address register indirect with displacement
                if (bytes.Length < 2) return (null, 0);
                Int16 disp = (Int16)((bytes[0] << 8) | bytes[1]);
                return (new OM68000_AddressDisplacement(reg, disp), 2);
            
            case 6: // Address register indirect with index
                if (bytes.Length < 2) return (null, 0);
                byte extension = bytes[0];
                sbyte offset = (sbyte)bytes[1];
                int indexReg = (extension >> 4) & 7;
                bool isAddress = ((extension >> 3) & 1) == 1;
                bool isLong = ((extension >> 3) & 1) == 1;
                return (new OM68000_AddressIndex(reg, indexReg, isAddress, isLong, offset), 2);
            
            case 7: // Absolute and immediate
                switch (reg)
                {
                    case 0: // Absolute short
                        if (bytes.Length < 2) return (null, 0);
                        UInt16 absShort = (UInt16)((bytes[0] << 8) | bytes[1]);
                        return (new OM68000_AbsoluteShort(absShort), 2);
                    
                    case 1: // Absolute long
                        if (bytes.Length < 4) return (null, 0);
                        UInt32 absLong = (UInt32)((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]);
                        return (new OM68000_AbsoluteLong(absLong), 4);
                    
                    case 2: // PC with displacement
                        if (bytes.Length < 2) return (null, 0);
                        Int16 pcDisp = (Int16)((bytes[0] << 8) | bytes[1]);
                        return (new OM68000_PCDisplacement(pcDisp), 2);
                    
                    case 3: // PC with index
                        if (bytes.Length < 2) return (null, 0);
                        byte pcExt = bytes[0];
                        sbyte pcOff = (sbyte)bytes[1];
                        int pcIndexReg = (pcExt >> 4) & 7;
                        bool pcIsAddress = ((pcExt >> 3) & 1) == 1;
                        bool pcIsLong = ((pcExt >> 3) & 1) == 1;
                        return (new OM68000_PCIndex(pcIndexReg, pcIsAddress, pcIsLong, pcOff), 2);
                    
                    case 4: // Immediate
                        if (size == SizeCode.Long)
                        {
                            if (bytes.Length < 4) return (null, 0);
                            UInt32 immLong = (UInt32)((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]);
                            return (new OM68000_Immediate(immLong, size), 4);
                        }
                        else
                        {
                            if (bytes.Length < 2) return (null, 0);
                            UInt16 immWord = (UInt16)((bytes[0] << 8) | bytes[1]);
                            return (new OM68000_Immediate(immWord, size), 2);
                        }
                    
                    default:
                        return (null, 0);
                }
            
            default:
                return (null, 0);
        }
    }

    private SizeCode DecodeSizeCode(int sizeField)
    {
        return sizeField switch
        {
            0 => SizeCode.Byte,
            1 => SizeCode.Word,
            2 => SizeCode.Long,
            _ => SizeCode.Word
        };
    }

    private string GetConditionCode(int condition)
    {
        return condition switch
        {
            0x0 => "T",   // True
            0x1 => "F",   // False
            0x2 => "HI",  // High
            0x3 => "LS",  // Low or Same
            0x4 => "CC",  // Carry Clear
            0x5 => "CS",  // Carry Set
            0x6 => "NE",  // Not Equal
            0x7 => "EQ",  // Equal
            0x8 => "VC",  // Overflow Clear
            0x9 => "VS",  // Overflow Set
            0xA => "PL",  // Plus
            0xB => "MI",  // Minus
            0xC => "GE",  // Greater or Equal
            0xD => "LT",  // Less Than
            0xE => "GT",  // Greater Than
            0xF => "LE",  // Less or Equal
            _ => "??"
        };
    }

    private DecodeResult CreateSimpleInstruction(ulong address, string mnemonic, UInt16 opcode, int size, bool isBranch = false, bool isTerminator = false)
    {
        var operands = new List<IOperand>();
        var instructionBytes = SliceBytes(size);
        var instruction = new Instruction(address, mnemonic, operands, instructionBytes, State);
        instruction.IsBranch = isBranch;
        instruction.IsBasicBlockTerminator = isTerminator;
        
        if (!isTerminator)
        {
            instruction.NextAddresses.Add(address + (ulong)size);
        }
        
        return DecodeResult.CreateSuccess(instruction, size);
    }

    private DecodeResult CreateInstruction(ulong address, string mnemonic, List<IOperand> operands, int size, bool isBranch = false, bool isTerminator = false, UInt64? nextAddress = null)
    {
        var instructionBytes = SliceBytes(size);
        var instruction = new Instruction(address, mnemonic, operands, instructionBytes, State);
        instruction.IsBranch = isBranch;
        instruction.IsBasicBlockTerminator = isTerminator;
        
        if (nextAddress.HasValue)
        {
            instruction.NextAddresses.Add(nextAddress.Value);
        }
        
        if (!isTerminator)
        {
            instruction.NextAddresses.Add(address + (ulong)size);
        }
        
        return DecodeResult.CreateSuccess(instruction, size);
    }

    private byte[] SliceBytes(int size)
    {
        var result = new byte[size];
        if (_decodeBuffer.Length >= size)
            Array.Copy(_decodeBuffer, 0, result, 0, size);
        return result;
    }

    public override List<(UInt64 address, uint size)> FetchMemoryAccesses(Instruction ins, ICpuRegisterState registerState)
    {
        M68000RegisterState registers = (M68000RegisterState)registerState;
        var accesses = new List<(UInt64 address, uint size)>();
        
        // Extract the size from the mnemonic (e.g., "MOVE.L" -> 4 bytes)
        uint size = ExtractSizeFromMnemonic(ins.Mnemonic);
        
        // Process each operand to find memory accesses
        foreach (var operand in ins.Operands)
        {
            UInt64 effectiveAddress = 0;
            
            if (operand is OM68000_AddressIndirect indirect)
            {
                // (An) - memory at address register
                int regNum = (int)operand.Value;
                effectiveAddress = registers.AddressRegisters[regNum];
                accesses.Add((effectiveAddress, size));
            }
            else if (operand is OM68000_AddressPostInc postInc)
            {
                // (An)+ - memory at address register
                int regNum = (int)operand.Value;
                effectiveAddress = registers.AddressRegisters[regNum];
                accesses.Add((effectiveAddress, size));
            }
            else if (operand is OM68000_AddressPreDec preDec)
            {
                // -(An) - memory at address register
                int regNum = (int)operand.Value;
                effectiveAddress = registers.AddressRegisters[regNum];
                accesses.Add((effectiveAddress, size));
            }
            else if (operand is OM68000_AddressDisplacement disp)
            {
                // d16(An) - displacement from address register
                int regNum = (int)(operand.Value >> 16);  // High 16 bits = register
                int displacement = (short)(operand.Value & 0xFFFF);  // Low 16 bits = displacement (signed)
                effectiveAddress = (UInt64)((long)registers.AddressRegisters[regNum] + displacement);
                accesses.Add((effectiveAddress, size));
            }
            else if (operand is OM68000_AddressIndex index)
            {
                // d8(An,Xn) - displacement + index from address register
                int regNum = (int)(operand.Value >> 24);  // Bits 24-31 = base register
                int displacement = (sbyte)((operand.Value >> 16) & 0xFF);  // Bits 16-23 = displacement (signed)
                int indexReg = (int)((operand.Value >> 8) & 0x0F);  // Bits 8-11 = index register
                bool useAddressReg = ((operand.Value >> 12) & 1) == 1;  // Bit 12 = A/D flag
                bool is32Bit = ((operand.Value >> 13) & 1) == 1;  // Bit 13 = L/W flag
                
                UInt32 indexValue = 0;
                if (useAddressReg)
                {
                    indexValue = registers.AddressRegisters[indexReg];
                }
                else
                {
                    indexValue = registers.DataRegisters[indexReg];
                }
                
                // Use only lower 16 bits if 16-bit index
                if (!is32Bit)
                {
                    indexValue = (UInt16)indexValue;
                }
                
                effectiveAddress = (UInt64)((long)registers.AddressRegisters[regNum] + displacement + indexValue);
                accesses.Add((effectiveAddress, size));
            }
            else if (operand is OM68000_AbsoluteShort absShort)
            {
                // xxxx.W - absolute short (16-bit address, sign-extended)
                effectiveAddress = (UInt16)operand.Value;
                accesses.Add((effectiveAddress, size));
            }
            else if (operand is OM68000_AbsoluteLong absLong)
            {
                // xxxxxxxx.L - absolute long (32-bit address)
                effectiveAddress = operand.Value;
                accesses.Add((effectiveAddress, size));
            }
            else if (operand is OM68000_PCDisplacement pcDisp)
            {
                // d16(PC) - displacement from PC (PC = next instruction address)
                int displacement = (short)operand.Value;
                UInt64 pc = ins.Address + 2;  // PC points to next instruction
                effectiveAddress = (UInt64)((long)pc + displacement);
                accesses.Add((effectiveAddress, size));
            }
            else if (operand is OM68000_PCIndex pcIndex)
            {
                // d8(PC,Xn) - displacement + index from PC
                int displacement = (sbyte)((operand.Value >> 16) & 0xFF);  // High byte of value
                int indexReg = (int)((operand.Value >> 8) & 0x0F);  // Bits 8-11 = index register
                bool useAddressReg = ((operand.Value >> 12) & 1) == 1;  // Bit 12 = A/D flag
                bool is32Bit = ((operand.Value >> 13) & 1) == 1;  // Bit 13 = L/W flag
                
                UInt32 indexValue = 0;
                if (useAddressReg)
                {
                    indexValue = registers.AddressRegisters[indexReg];
                }
                else
                {
                    indexValue = registers.DataRegisters[indexReg];
                }
                
                // Use only lower 16 bits if 16-bit index
                if (!is32Bit)
                {
                    indexValue = (UInt16)indexValue;
                }
                
                UInt64 pc = ins.Address + 2;  // PC points to next instruction
                effectiveAddress = (UInt64)((long)pc + displacement + indexValue);
                accesses.Add((effectiveAddress, size));
            }
            // Skip non-memory addressing modes:
            // OM68000_DataRegister, OM68000_AddressRegister, OM68000_Immediate, 
            // OM68000_StatusRegister, OM68000_ConditionCodeRegister, OM68000_UserStackPointer,
            // OM68000_BranchTarget (not a memory access), OM68000_RegisterList
        }
        
        return accesses;
    }
    
    private uint ExtractSizeFromMnemonic(string mnemonic)
    {
        // Extract size from mnemonic like "MOVE.L", "MOVE.W", "MOVE.B"
        if (mnemonic.Contains(".L"))
            return 4;
        if (mnemonic.Contains(".W"))
            return 2;
        if (mnemonic.Contains(".B"))
            return 1;
        
        // Default to word size if no size suffix found
        return 2;
    }
}

internal enum SizeCode
{
    Byte,
    Word,
    Long
}
