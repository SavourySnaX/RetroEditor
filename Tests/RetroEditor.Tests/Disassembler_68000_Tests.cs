using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroEditor.Plugins;

namespace RetroEditor.Tests;

/// <summary>
/// Unit tests for the Motorola 68000 disassembler
/// </summary>
[TestClass]
public class Disassembler_68000_Tests
{
    private Megadrive68000Disassembler _disassembler;
    
    [TestInitialize]
    public void Setup()
    {
        _disassembler = new Megadrive68000Disassembler();
    }

    internal List<(ulong address, uint size)> FetchMemoryAccesses(Instruction instruction, ICpuRegisterState registers)
    {
        return _disassembler.FetchMemoryAccesses(instruction, registers);
    }

    private void AssertInstruction(
        DecodeResult result,
        string mnemonic,
        int bytesConsumed,
        byte[] expectedBytes,
        int operandCount = 0,
        string[] operandText = null,
        bool isBranch = false,
        bool isTerminator = false,
        List<ulong> nextAddresses = null
        )
    {
        Assert.IsTrue(result.Success);
        
        var instruction = result.Instruction;
        Assert.AreEqual(mnemonic, instruction.Mnemonic);
        Assert.AreEqual(bytesConsumed, result.BytesConsumed);
        
        // Verify the bytes match
        Assert.IsNotNull(expectedBytes);
        Assert.AreEqual(bytesConsumed, instruction.Bytes.Length, "Instruction bytes length mismatch");
        CollectionAssert.AreEqual(expectedBytes, instruction.Bytes, "Instruction bytes mismatch");

        var operands = instruction.Operands;
        Assert.AreEqual(operandCount, operands.Count);
        
        if (operandText != null)
        {
            for (int i = 0; i < operandText.Length; i++)
            {
                Assert.IsNotNull(operands[i]);
                Assert.AreEqual(operandText[i], operands[i].Text());
            }
        }
        
        Assert.AreEqual(isBranch, instruction.IsBranch);
        Assert.AreEqual(isTerminator, instruction.IsBasicBlockTerminator);
        
        if (nextAddresses == null && !(instruction.IsBranch || instruction.IsBasicBlockTerminator))
        {
            Assert.AreEqual(1, instruction.NextAddresses.Count);
            // For regular instructions, we expect fall-through but can't validate exact address in this context
        }

        if (nextAddresses != null)
        {
            Assert.AreEqual(nextAddresses.Count, instruction.NextAddresses.Count);
            for (int i = 0; i < nextAddresses.Count; i++)
            {
                Assert.IsTrue(nextAddresses.Contains(instruction.NextAddresses[i]));
            }
        }
    }
    
    // Test basic move instructions
    [TestMethod]
    public void Test68000_MOVE_DataRegister_To_DataRegister()
    {
        // MOVE.L D0,D1
        byte[] bytes = { 0x22, 0x00 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "MOVE.L", 2, bytes, 2, new[] { "D0", "D1" });
    }
    
    [TestMethod]
    public void Test68000_MOVE_Immediate_To_DataRegister()
    {
        // MOVE.W #$1234,D0
        byte[] bytes = { 0x30, 0x3C, 0x12, 0x34 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "MOVE.W", 4, bytes, 2, new[] { "#$1234", "D0" });
    }
    
    [TestMethod]
    public void Test68000_MOVE_AbsoluteLong_To_DataRegister()
    {
        // MOVE.L $FF0000,D0
        byte[] bytes = { 0x20, 0x39, 0x00, 0xFF, 0x00, 0x00 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "MOVE.L", 6, bytes, 2, new[] { "$00FF0000.L", "D0" });
    }
    
    // Test arithmetic instructions
    [TestMethod]
    public void Test68000_ADD_DataRegister_To_DataRegister()
    {
        // ADD.L D1,D0
        byte[] bytes = { 0xD0, 0x81 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "ADD.L", 2, bytes, 2, new[] { "D1", "D0" });
    }
    
    [TestMethod]
    public void Test68000_ADDI_Immediate_To_DataRegister()
    {
        // ADDI.W #$10,D0
        byte[] bytes = { 0x06, 0x40, 0x00, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "ADDI.W", 4, bytes, 2, new[] { "#$0010", "D0" });
    }
    
    [TestMethod]
    public void Test68000_ADDQ_Quick_To_DataRegister()
    {
        // ADDQ.L #1,D0
        byte[] bytes = { 0x52, 0x80 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "ADDQ.L", 2, bytes, 2, new[] { "#$01", "D0" });
    }
    
    [TestMethod]
    public void Test68000_SUB_DataRegister_From_DataRegister()
    {
        // SUB.L D1,D0
        byte[] bytes = { 0x90, 0x81 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "SUB.L", 2, bytes, 2, new[] { "D1", "D0" });
    }
    
    [TestMethod]
    public void Test68000_SUBQ_Quick_From_DataRegister()
    {
        // SUBQ.L #1,D0
        byte[] bytes = { 0x53, 0x80 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "SUBQ.L", 2, bytes, 2, new[] { "#$01", "D0" });
    }
    
    // Test logical instructions
    [TestMethod]
    public void Test68000_AND_DataRegister_To_DataRegister()
    {
        // AND.L D1,D0
        byte[] bytes = { 0xC0, 0x81 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "AND.L", 2, bytes, 2, new[] { "D1", "D0" });
    }
    
    [TestMethod]
    public void Test68000_OR_DataRegister_To_DataRegister()
    {
        // OR.L D1,D0
        byte[] bytes = { 0x80, 0x81 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "OR.L", 2, bytes, 2, new[] { "D1", "D0" });
    }
    
    [TestMethod]
    public void Test68000_EOR_DataRegister_To_DataRegister()
    {
        // EOR.L D0,D1
        byte[] bytes = { 0xB1, 0x81 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "EOR.L", 2, bytes, 2, new[] { "D0", "D1" });
    }
    
    [TestMethod]
    public void Test68000_NOT_DataRegister()
    {
        // NOT.L D0
        byte[] bytes = { 0x46, 0x80 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "NOT.L", 2, bytes, 1, new[] { "D0" });
    }
    
    // Test compare instructions
    [TestMethod]
    public void Test68000_CMP_DataRegister_To_DataRegister()
    {
        // CMP.L D1,D0
        byte[] bytes = { 0xB0, 0x81 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "CMP.L", 2, bytes, 2, new[] { "D1", "D0" });
    }
    
    [TestMethod]
    public void Test68000_CMPI_Immediate_To_DataRegister()
    {
        // CMPI.L #$1234,D0
        byte[] bytes = { 0x0C, 0x80, 0x00, 0x00, 0x12, 0x34 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "CMPI.L", 6, bytes, 2, new[] { "#$00001234", "D0" });
    }
    
    [TestMethod]
    public void Test68000_TST_DataRegister()
    {
        // TST.L D0
        byte[] bytes = { 0x4A, 0x80 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "TST.L", 2, bytes, 1, new[] { "D0" });
    }
    
    // Test branch instructions
    [TestMethod]
    public void Test68000_BRA_Short()
    {
        // BRA.S $10 (relative)
        byte[] bytes = { 0x60, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "BRA", 2, bytes, 1, new[] { "$001012.L" }, isBranch: true, isTerminator: true);
    }
    
    [TestMethod]
    public void Test68000_BRA_Long()
    {
        // BRA.W $1234 (relative)
        byte[] bytes = { 0x60, 0x00, 0x12, 0x34 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "BRA", 4, bytes, 1, new[] { "$002236.L" }, isBranch: true, isTerminator: true);
    }
    
    [TestMethod]
    public void Test68000_BEQ_Short()
    {
        // BEQ.S $10 (relative)
        byte[] bytes = { 0x67, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "BEQ", 2, bytes, 1, new[] { "$001012.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_BNE_Short()
    {
        // BNE.S $10 (relative)
        byte[] bytes = { 0x66, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "BNE", 2, bytes, 1, new[] { "$001012.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_BSR()
    {
        // BSR.S $10 (relative)
        byte[] bytes = { 0x61, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "BSR", 2, bytes, 1, new[] { "$001012.L" }, isBranch: true, isTerminator: false);
    }
    
    // Test DBcc instructions - all condition codes
    [TestMethod]
    public void Test68000_DBT()
    {
        // DBT D0,$10 (condition = 0000 - True, never terminates normally)
        byte[] bytes = { 0x50, 0xC8, 0x00, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "DBT", 4, bytes, 2, new[] { "D0", "$001012.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_DBF()
    {
        // DBF D0,$10 (condition = 0001 - False/Always, also known as DBRA)
        byte[] bytes = { 0x51, 0xC8, 0x00, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "DBF", 4, bytes, 2, new[] { "D0", "$001012.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_DBHI()
    {
        // DBHI D1,$20 (condition = 0010 - High)
        byte[] bytes = { 0x52, 0xC9, 0x00, 0x20 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "DBHI", 4, bytes, 2, new[] { "D1", "$001022.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_DBLS()
    {
        // DBLS D2,$30 (condition = 0011 - Low or Same)
        byte[] bytes = { 0x53, 0xCA, 0x00, 0x30 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "DBLS", 4, bytes, 2, new[] { "D2", "$001032.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_DBCC()
    {
        // DBCC D3,$40 (condition = 0100 - Carry Clear)
        byte[] bytes = { 0x54, 0xCB, 0x00, 0x40 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "DBCC", 4, bytes, 2, new[] { "D3", "$001042.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_DBCS()
    {
        // DBCS D4,$50 (condition = 0101 - Carry Set)
        byte[] bytes = { 0x55, 0xCC, 0x00, 0x50 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "DBCS", 4, bytes, 2, new[] { "D4", "$001052.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_DBNE()
    {
        // DBNE D5,$60 (condition = 0110 - Not Equal)
        byte[] bytes = { 0x56, 0xCD, 0x00, 0x60 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "DBNE", 4, bytes, 2, new[] { "D5", "$001062.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_DBEQ()
    {
        // DBEQ D6,$70 (condition = 0111 - Equal)
        byte[] bytes = { 0x57, 0xCE, 0x00, 0x70 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "DBEQ", 4, bytes, 2, new[] { "D6", "$001072.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_DBVC()
    {
        // DBVC D7,$80 (condition = 1000 - Overflow Clear)
        byte[] bytes = { 0x58, 0xCF, 0x00, 0x80 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "DBVC", 4, bytes, 2, new[] { "D7", "$001082.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_DBVS()
    {
        // DBVS D0,$90 (condition = 1001 - Overflow Set)
        byte[] bytes = { 0x59, 0xC8, 0x00, 0x90 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "DBVS", 4, bytes, 2, new[] { "D0", "$001092.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_DBPL()
    {
        // DBPL D1,$A0 (condition = 1010 - Plus)
        byte[] bytes = { 0x5A, 0xC9, 0x00, 0xA0 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "DBPL", 4, bytes, 2, new[] { "D1", "$0010A2.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_DBMI()
    {
        // DBMI D2,$B0 (condition = 1011 - Minus)
        byte[] bytes = { 0x5B, 0xCA, 0x00, 0xB0 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "DBMI", 4, bytes, 2, new[] { "D2", "$0010B2.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_DBGE()
    {
        // DBGE D3,$C0 (condition = 1100 - Greater or Equal)
        byte[] bytes = { 0x5C, 0xCB, 0x00, 0xC0 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "DBGE", 4, bytes, 2, new[] { "D3", "$0010C2.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_DBLT()
    {
        // DBLT D4,$D0 (condition = 1101 - Less Than)
        byte[] bytes = { 0x5D, 0xCC, 0x00, 0xD0 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "DBLT", 4, bytes, 2, new[] { "D4", "$0010D2.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_DBGT()
    {
        // DBGT D5,$E0 (condition = 1110 - Greater Than)
        byte[] bytes = { 0x5E, 0xCD, 0x00, 0xE0 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "DBGT", 4, bytes, 2, new[] { "D5", "$0010E2.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_DBLE()
    {
        // DBLE D6,$F0 (condition = 1111 - Less or Equal)
        byte[] bytes = { 0x5F, 0xCE, 0x00, 0xF0 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "DBLE", 4, bytes, 2, new[] { "D6", "$0010F2.L" }, isBranch: true, isTerminator: false);
    }
    
    // Test jump instructions
    [TestMethod]
    public void Test68000_JMP_AbsoluteLong()
    {
        // JMP $1234.L
        byte[] bytes = { 0x4E, 0xF9, 0x00, 0x00, 0x12, 0x34 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "JMP", 6, bytes, 1, new[] { "$00001234.L" }, isBranch: true, isTerminator: true);
    }
    
    [TestMethod]
    public void Test68000_JSR_AbsoluteLong()
    {
        // JSR $1234.L
        byte[] bytes = { 0x4E, 0xB9, 0x00, 0x00, 0x12, 0x34 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "JSR", 6, bytes, 1, new[] { "$00001234.L" }, isBranch: true, isTerminator: true, new List<ulong> { 0x00001234 });
    }
    
    [TestMethod]
    public void Test68000_JSR_PCRelativeWithIndex()
    {
        // JSR 4(PC,D0.W)
        byte[] bytes = { 0x4E, 0xBB, 0x00, 0x04 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "JSR", 4, bytes, 1, new[] { "4(PC,D0.W)" }, isBranch: true, isTerminator: true);
    }
    
    [TestMethod]
    public void Test68000_JSR_PCRelativeWithIndexLong()
    {
        // JSR 10(PC,A1.L)
        byte[] bytes = { 0x4E, 0xBB, 0x98, 0x0A };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "JSR", 4, bytes, 1, new[] { "10(PC,A1.L)" }, isBranch: true, isTerminator: true);
    }
    
    [TestMethod]
    public void Test68000_JMP_PCRelativeWithIndex()
    {
        // JMP 8(PC,D1.W)
        byte[] bytes = { 0x4E, 0xFB, 0x10, 0x08 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "JMP", 4, bytes, 1, new[] { "8(PC,D1.W)" }, isBranch: true, isTerminator: true);
    }
    
    // Test return instructions
    [TestMethod]
    public void Test68000_RTS()
    {
        // RTS
        byte[] bytes = { 0x4E, 0x75 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "RTS", 2, bytes, 0, null, isTerminator: true);
    }
    
    [TestMethod]
    public void Test68000_RTE()
    {
        // RTE
        byte[] bytes = { 0x4E, 0x73 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "RTE", 2, bytes, 0, null, isTerminator: true);
    }
    
    [TestMethod]
    public void Test68000_RTR()
    {
        // RTR
        byte[] bytes = { 0x4E, 0x77 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "RTR", 2, bytes, 0, null, isTerminator: true);
    }
    
    // Test shift/rotate instructions
    [TestMethod]
    public void Test68000_LSL_Immediate()
    {
        // LSL.L #1,D0
        byte[] bytes = { 0xE3, 0x88 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "LSL.L", 2, bytes, 2, new[] { "#$01", "D0" });
    }
    
    [TestMethod]
    public void Test68000_LSR_Immediate()
    {
        // LSR.L #1,D0
        byte[] bytes = { 0xE2, 0x88 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "LSR.L", 2, bytes, 2, new[] { "#$01", "D0" });
    }
    
    [TestMethod]
    public void Test68000_ASL_Immediate()
    {
        // ASL.L #1,D0
        byte[] bytes = { 0xE3, 0x80 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "ASL.L", 2, bytes, 2, new[] { "#$01", "D0" });
    }
    
    [TestMethod]
    public void Test68000_ASR_Immediate()
    {
        // ASR.L #1,D0
        byte[] bytes = { 0xE2, 0x80 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "ASR.L", 2, bytes, 2, new[] { "#$01", "D0" });
    }
    
    // Test multiply and divide
    [TestMethod]
    public void Test68000_MULU()
    {
        // MULU.W D1,D0
        byte[] bytes = { 0xC0, 0xC1 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "MULU", 2, bytes, 2, new[] { "D1", "D0" });
    }
    
    [TestMethod]
    public void Test68000_MULS()
    {
        // MULS.W D1,D0
        byte[] bytes = { 0xC1, 0xC1 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "MULS", 2, bytes, 2, new[] { "D1", "D0" });
    }
    
    [TestMethod]
    public void Test68000_DIVU()
    {
        // DIVU.W D1,D0
        byte[] bytes = { 0x80, 0xC1 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "DIVU", 2, bytes, 2, new[] { "D1", "D0" });
    }
    
    [TestMethod]
    public void Test68000_DIVS()
    {
        // DIVS.W D1,D0
        byte[] bytes = { 0x81, 0xC1 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "DIVS", 2, bytes, 2, new[] { "D1", "D0" });
    }
    
    // Test addressing modes
    [TestMethod]
    public void Test68000_AddressIndirect()
    {
        // MOVE.L (A0),D0
        byte[] bytes = { 0x20, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "MOVE.L", 2, bytes, 2, new[] { "(A0)", "D0" });
    }
    
    [TestMethod]
    public void Test68000_AddressPostIncrement()
    {
        // MOVE.L (A0)+,D0
        byte[] bytes = { 0x20, 0x18 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "MOVE.L", 2, bytes, 2, new[] { "(A0)+", "D0" });
    }
    
    [TestMethod]
    public void Test68000_AddressPreDecrement()
    {
        // MOVE.L -(A0),D0
        byte[] bytes = { 0x20, 0x20 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "MOVE.L", 2, bytes, 2, new[] { "-(A0)", "D0" });
    }
    
    [TestMethod]
    public void Test68000_AddressDisplacement()
    {
        // MOVE.L $10(A0),D0
        byte[] bytes = { 0x20, 0x28, 0x00, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "MOVE.L", 4, bytes, 2, new[] { "16(A0)", "D0" });
    }
    
    [TestMethod]
    public void Test68000_AddressIndirectWithIndex()
    {
        // MOVE.L 0(A0,D0.W),D1
        byte[] bytes = { 0x22, 0x30, 0x00, 0x00 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "MOVE.L", 4, bytes, 2, new[] { "0(A0,D0.W)", "D1" });
    }
    
    [TestMethod]
    public void Test68000_AddressIndirectWithIndexAndDisplacement()
    {
        // MOVE.L 20(A1,A2.L),D3
        byte[] bytes = { 0x26, 0x31, 0xA8, 0x14 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "MOVE.L", 4, bytes, 2, new[] { "20(A1,A2.L)", "D3" });
    }
    
    [TestMethod]
    public void Test68000_PCRelativeWithDisplacement()
    {
        // MOVE.L $10(PC),D0
        byte[] bytes = { 0x20, 0x3A, 0x00, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "MOVE.L", 4, bytes, 2, new[] { "16(PC)", "D0" });
    }
    
    [TestMethod]
    public void Test68000_PCRelativeWithIndex()
    {
        // MOVE.L 6(PC,D1.W),D2
        byte[] bytes = { 0x24, 0x3B, 0x10, 0x06 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "MOVE.L", 4, bytes, 2, new[] { "6(PC,D1.W)", "D2" });
    }
    
    [TestMethod]
    public void Test68000_AbsoluteShort()
    {
        // MOVE.L $1234.W,D0
        byte[] bytes = { 0x20, 0x38, 0x12, 0x34 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "MOVE.L", 4, bytes, 2, new[] { "$1234.W", "D0" });
    }
    
    // Test miscellaneous instructions
    [TestMethod]
    public void Test68000_NOP()
    {
        // NOP
        byte[] bytes = { 0x4E, 0x71 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "NOP", 2, bytes, 0);
    }
    
    [TestMethod]
    public void Test68000_MOVEQ()
    {
        // MOVEQ #$12,D0
        byte[] bytes = { 0x70, 0x12 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "MOVEQ", 2, bytes, 2, new[] { "#$12", "D0" });
    }
    
    [TestMethod]
    public void Test68000_LEA()
    {
        // LEA $1234.L,A0
        byte[] bytes = { 0x41, 0xF9, 0x00, 0x00, 0x12, 0x34 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "LEA", 6, bytes, 2, new[] { "$00001234.L", "A0" });
    }
    
    [TestMethod]
    public void Test68000_CLR()
    {
        // CLR.L D0
        byte[] bytes = { 0x42, 0x80 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "CLR.L", 2, bytes, 1, new[] { "D0" });
    }
    
    [TestMethod]
    public void Test68000_NEG()
    {
        // NEG.L D0
        byte[] bytes = { 0x44, 0x80 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "NEG.L", 2, bytes, 1, new[] { "D0" });
    }
    
    [TestMethod]
    public void Test68000_EXT_Word()
    {
        // EXT.W D0
        byte[] bytes = { 0x48, 0x80 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "EXT.W", 2, bytes, 1, new[] { "D0" });
    }
    
    [TestMethod]
    public void Test68000_EXT_Long()
    {
        // EXT.L D0
        byte[] bytes = { 0x48, 0xC0 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "EXT.L", 2, bytes, 1, new[] { "D0" });
    }
    
    // Test additional branch conditions
    [TestMethod]
    public void Test68000_BCC_Short()
    {
        // BCC.S $10 (relative)
        byte[] bytes = { 0x64, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "BCC", 2, bytes, 1, new[] { "$001012.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_BCS_Short()
    {
        // BCS.S $10 (relative)
        byte[] bytes = { 0x65, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "BCS", 2, bytes, 1, new[] { "$001012.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_BPL_Short()
    {
        // BPL.S $10 (relative)
        byte[] bytes = { 0x6A, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "BPL", 2, bytes, 1, new[] { "$001012.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_BMI_Short()
    {
        // BMI.S $10 (relative)
        byte[] bytes = { 0x6B, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "BMI", 2, bytes, 1, new[] { "$001012.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_BGE_Short()
    {
        // BGE.S $10 (relative)
        byte[] bytes = { 0x6C, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "BGE", 2, bytes, 1, new[] { "$001012.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_BLT_Short()
    {
        // BLT.S $10 (relative)
        byte[] bytes = { 0x6D, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "BLT", 2, bytes, 1, new[] { "$001012.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_BGT_Short()
    {
        // BGT.S $10 (relative)
        byte[] bytes = { 0x6E, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "BGT", 2, bytes, 1, new[] { "$001012.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_BLE_Short()
    {
        // BLE.S $10 (relative)
        byte[] bytes = { 0x6F, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "BLE", 2, bytes, 1, new[] { "$001012.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_BHI_Short()
    {
        // BHI.S $10 (relative)
        byte[] bytes = { 0x62, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "BHI", 2, bytes, 1, new[] { "$001012.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_BLS_Short()
    {
        // BLS.S $10 (relative)
        byte[] bytes = { 0x63, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "BLS", 2, bytes, 1, new[] { "$001012.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_BVC_Short()
    {
        // BVC.S $10 (relative)
        byte[] bytes = { 0x68, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "BVC", 2, bytes, 1, new[] { "$001012.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_BVS_Short()
    {
        // BVS.S $10 (relative)
        byte[] bytes = { 0x69, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "BVS", 2, bytes, 1, new[] { "$001012.L" }, isBranch: true, isTerminator: false);
    }
    
    // Test Scc instructions
    [TestMethod]
    public void Test68000_Scc_DataRegister()
    {
        // SEQ D0
        byte[] bytes = { 0x57, 0xC0 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "SEQ", 2, bytes, 1, new[] { "D0" });
    }
    
    [TestMethod]
    public void Test68000_Scc_Memory()
    {
        // SNE (A0)
        byte[] bytes = { 0x56, 0xD0 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "SNE", 2, bytes, 1, new[] { "(A0)" });
    }
    
    // Test MOVEM
    [TestMethod]
    public void Test68000_MOVEM_ToMemory()
    {
        // MOVEM.L D0-D7/A0-A6,-(A7)
        byte[] bytes = { 0x48, 0xE7, 0xFF, 0xFE };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "MOVEM.L", 4, bytes, 2, new[] { "D1/D2/D3/D4/D5/D6/D7/A0/A1/A2/A3/A4/A5/A6/A7", "-(A7)" });
    }
    
    [TestMethod]
    public void Test68000_MOVEM_FromMemory()
    {
        // MOVEM.L (A7)+,D0-D7/A0-A6
        byte[] bytes = { 0x4C, 0xDF, 0x7F, 0xFF };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "MOVEM.L", 4, bytes, 2, new[] { "(A7)+", "D0/D1/D2/D3/D4/D5/D6/D7/A0/A1/A2/A3/A4/A5/A6" });
    }
    
    // Test bit manipulation
    
    [TestMethod]
    public void Test68000_BSET_Dynamic()
    {
        // BSET D0,D1
        byte[] bytes = { 0x01, 0xC1 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "BSET", 2, bytes, 2, new[] { "D0", "D1" });
    }
    
    [TestMethod]
    public void Test68000_BSET_Static()
    {
        // BSET #7,D0
        byte[] bytes = { 0x08, 0xC0, 0x00, 0x07 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "BSET", 4, bytes, 2, new[] { "#$07", "D0" });
    }
    
    [TestMethod]
    public void Test68000_BCLR_Static()
    {
        // BCLR #7,D0
        byte[] bytes = { 0x08, 0x80, 0x00, 0x07 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "BCLR", 4, bytes, 2, new[] { "#$07", "D0" });
    }
    
    [TestMethod]
    public void Test68000_BCLR_Dynamic()
    {
        // BCLR D0,D1
        byte[] bytes = { 0x01, 0x81 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "BCLR", 2, bytes, 2, new[] { "D0", "D1" });
    }
    
    [TestMethod]
    public void Test68000_BCHG_Dynamic()
    {
        // BCHG D0,D1
        byte[] bytes = { 0x01, 0x41 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "BCHG", 2, bytes, 2, new[] { "D0", "D1" });
    }
    
    [TestMethod]
    public void Test68000_BTST_Dynamic()
    {
        // BTST D0,D1
        byte[] bytes = { 0x01, 0x01 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "BTST", 2, bytes, 2, new[] { "D0", "D1" });
    }
    
    [TestMethod]
    public void Test68000_BTST_Static()
    {
        // BTST #7,D0
        byte[] bytes = { 0x08, 0x00, 0x00, 0x07 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "BTST", 4, bytes, 2, new[] { "#$07", "D0" });
    }
    
    // Test MOVEA
    [TestMethod]
    public void Test68000_MOVEA_Word()
    {
        // MOVEA.W D0,A0 - opcode 0x3040
        byte[] bytes = { 0x30, 0x40 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "MOVEA.W", 2, bytes, 2, new[] { "D0", "A0" });
    }
    
    [TestMethod]
    public void Test68000_MOVEA_Long()
    {
        // MOVEA.L D0,A0 - opcode 0x2040
        byte[] bytes = { 0x20, 0x40 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "MOVEA.L", 2, bytes, 2, new[] { "D0", "A0" });
    }
    
    // Test ADDA and SUBA
    [TestMethod]
    public void Test68000_ADDA_Word()
    {
        // ADDA.W D0,A0
        byte[] bytes = { 0xD0, 0xC0 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "ADDA.W", 2, bytes, 2, new[] { "D0", "A0" });
    }
    
    [TestMethod]
    public void Test68000_ADDA_Long()
    {
        // ADDA.L D0,A0
        byte[] bytes = { 0xD1, 0xC0 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "ADDA.L", 2, bytes, 2, new[] { "D0", "A0" });
    }
    
    [TestMethod]
    public void Test68000_SUBA_Word()
    {
        // SUBA.W D0,A0
        byte[] bytes = { 0x90, 0xC0 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "SUBA.W", 2, bytes, 2, new[] { "D0", "A0" });
    }
    
    [TestMethod]
    public void Test68000_SUBA_Long()
    {
        // SUBA.L D0,A0
        byte[] bytes = { 0x91, 0xC0 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "SUBA.L", 2, bytes, 2, new[] { "D0", "A0" });
    }
    
    // Test CMPA
    [TestMethod]
    public void Test68000_CMPA_Word()
    {
        // CMPA.W D0,A0
        byte[] bytes = { 0xB0, 0xC0 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "CMPA.W", 2, bytes, 2, new[] { "D0", "A0" });
    }
    
    [TestMethod]
    public void Test68000_CMPA_Long()
    {
        // CMPA.L D0,A0
        byte[] bytes = { 0xB1, 0xC0 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "CMPA.L", 2, bytes, 2, new[] { "D0", "A0" });
    }
    
    // Test PEA
    [TestMethod]
    public void Test68000_PEA()
    {
        // PEA $1234.L
        byte[] bytes = { 0x48, 0x79, 0x00, 0x00, 0x12, 0x34 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "PEA", 6, bytes, 1, new[] { "$00001234.L" });
    }
    
    // Test ADDX and SUBX
    [TestMethod]
    public void Test68000_ADDX_DataRegister()
    {
        // ADDX.L D1,D0
        byte[] bytes = { 0xD1, 0x81 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "ADDX.L", 2, bytes, 2, new[] { "D1", "D0" });
    }
    
    [TestMethod]
    public void Test68000_ADDX_PreDecrement()
    {
        // ADDX.L -(A1),-(A0)
        byte[] bytes = { 0xD1, 0x89 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "ADDX.L", 2, bytes, 2, new[] { "-(A1)", "-(A0)" });
    }
    
    [TestMethod]
    public void Test68000_SUBX_DataRegister()
    {
        // SUBX.L D1,D0
        byte[] bytes = { 0x91, 0x81 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "SUBX.L", 2, bytes, 2, new[] { "D1", "D0" });
    }
    
    // Test CMPM
    [TestMethod]
    public void Test68000_CMPM()
    {
        // CMPM.L (A1)+,(A0)+ - opcode 0xB189
        byte[] bytes = { 0xB1, 0x89 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "CMPM.L", 2, bytes, 2, new[] { "(A1)+", "(A0)+" });
    }
    
    // Test ABCD and SBCD
    [TestMethod]
    public void Test68000_ABCD_DataRegister()
    {
        // ABCD D1,D0
        byte[] bytes = { 0xC1, 0x01 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "ABCD", 2, bytes, 2, new[] { "D1", "D0" });
    }
    
    [TestMethod]
    public void Test68000_SBCD_DataRegister()
    {
        // SBCD D1,D0
        byte[] bytes = { 0x81, 0x01 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "SBCD", 2, bytes, 2, new[] { "D1", "D0" });
    }
    
    // Test NBCD
    [TestMethod]
    public void Test68000_NBCD()
    {
        // NBCD D0
        byte[] bytes = { 0x48, 0x00 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "NBCD", 2, bytes, 1, new[] { "D0" });
    }
    
    // Test CHK
    [TestMethod]
    public void Test68000_CHK()
    {
        // CHK D1,D0
        byte[] bytes = { 0x41, 0x81 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "CHK", 2, bytes, 2, new[] { "D1", "D0" });
    }
    
    // Test TAS
    [TestMethod]
    public void Test68000_TAS()
    {
        // TAS D0 - opcode 0x4AC0
        byte[] bytes = { 0x4A, 0xC0 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "TAS", 2, bytes, 1, new[] { "D0" });
    }
    
    // Test NEGX
    [TestMethod]
    public void Test68000_NEGX()
    {
        // NEGX.L D0
        byte[] bytes = { 0x40, 0x80 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "NEGX.L", 2, bytes, 1, new[] { "D0" });
    }
    
    // Test rotate instructions with register count
    [TestMethod]
    public void Test68000_ROL_Register()
    {
        // ROL.L D1,D0
        byte[] bytes = { 0xE3, 0xB8 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "ROL.L", 2, bytes, 2, new[] { "D1", "D0" });
    }
    
    [TestMethod]
    public void Test68000_ROR_Register()
    {
        // ROR.L D1,D0
        byte[] bytes = { 0xE2, 0xB8 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "ROR.L", 2, bytes, 2, new[] { "D1", "D0" });
    }
    
    [TestMethod]
    public void Test68000_ROXL_Register()
    {
        // ROXL.L D1,D0
        byte[] bytes = { 0xE3, 0xB0 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "ROXL.L", 2, bytes, 2, new[] { "D1", "D0" });
    }
    
    [TestMethod]
    public void Test68000_ROXR_Register()
    {
        // ROXR.L D1,D0
        byte[] bytes = { 0xE2, 0xB0 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "ROXR.L", 2, bytes, 2, new[] { "D1", "D0" });
    }
    
    // Test TRAPV and illegal
    [TestMethod]
    public void Test68000_TRAPV()
    {
        // TRAPV
        byte[] bytes = { 0x4E, 0x76 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "TRAPV", 2, bytes, 0, null, isBranch: false, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_ILLEGAL()
    {
        // ILLEGAL instruction (0x4AFC)
        byte[] bytes = { 0x4A, 0xFC };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "ILLEGAL", 2, bytes, 0, null, isBranch: true, isTerminator: true);
    }
    
    // Test RESET, STOP
    [TestMethod]
    public void Test68000_RESET()
    {
        // RESET
        byte[] bytes = { 0x4E, 0x70 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "RESET", 2, bytes, 0);
    }
    
    [TestMethod]
    public void Test68000_STOP()
    {
        // STOP #$2700
        byte[] bytes = { 0x4E, 0x72, 0x27, 0x00 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "STOP", 4, bytes, 1, new[] { "#$2700" }, isBranch: false, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_SWAP()
    {
        // SWAP D0
        byte[] bytes = { 0x48, 0x40 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "SWAP", 2, bytes, 1, new[] { "D0" });
    }
    
    [TestMethod]
    public void Test68000_EXG_DataRegisters()
    {
        // EXG D0,D1
        byte[] bytes = { 0xC1, 0x41 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "EXG", 2, bytes, 2, new[] { "D0", "D1" });
    }
    
    [TestMethod]
    public void Test68000_TRAP()
    {
        // TRAP #0
        byte[] bytes = { 0x4E, 0x40 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "TRAP", 2, bytes, 1, new[] { "#$00" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_LINK()
    {
        // LINK A6,#-$10
        byte[] bytes = { 0x4E, 0x56, 0xFF, 0xF0 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "LINK", 4, bytes, 2, new[] { "A6", "#$FFF0" });
    }
    
    [TestMethod]
    public void Test68000_UNLK()
    {
        // UNLK A6
        byte[] bytes = { 0x4E, 0x5E };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "UNLK", 2, bytes, 1, new[] { "A6" });
    }
    
    // Test state management
    [TestMethod]
    public void Test68000_StateManagement()
    {
        var state = (Megadrive68000State)_disassembler.State;
        
        // Test initial state
        Assert.IsTrue(state.SupervisorMode);
        
        // Test state cloning
        var clonedState = (Megadrive68000State)state.Clone();
        Assert.AreEqual(state.SupervisorMode, clonedState.SupervisorMode);
        
        // Modify clone
        clonedState.SupervisorMode = false;
        Assert.IsTrue(state.SupervisorMode);  // Original unchanged
        Assert.IsFalse(clonedState.SupervisorMode);
    }
    
    // Next Address Tests
    [TestMethod]
    public void Test68000_NextAddresses_RegularInstruction()
    {
        // Regular instruction should have fall-through address only
        var bytes = new byte[] { 0x42, 0x40 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);  // CLR.W D0
        AssertInstruction(result, "CLR.W", 2, bytes, 1, new[] { "D0" },
            nextAddresses: new List<ulong> { 0x1002 });
    }
    
    [TestMethod]
    public void Test68000_NextAddresses_ConditionalBranch()
    {
        // Conditional branch should have both fall-through and target addresses
        var bytes = new byte[] { 0x67, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x2000);  // BEQ +16
        var fallThrough = 0x2000UL + 2;  // Address after instruction
        var target = 0x2000UL + 2 + 0x10; // Branch target
        AssertInstruction(result, "BEQ", 2, bytes, 1, isBranch: true,
            nextAddresses: new List<ulong> { fallThrough, target });
    }
    
    [TestMethod]
    public void Test68000_NextAddresses_ConditionalBranch_Negative()
    {
        // Conditional branch with negative offset
        var bytes = new byte[] { 0x65, 0xF0 };
        var result = _disassembler.DecodeNext(bytes, 0x2000);  // BCS -16
        var fallThrough = 0x2000UL + 2;  // Address after instruction
        var target = (ulong)((long)(0x2000UL + 2) + unchecked((sbyte)0xF0)); // Branch target (negative)
        AssertInstruction(result, "BCS", 2, bytes, 1, isBranch: true,
            nextAddresses: new List<ulong> { fallThrough, target });
    }
    
    [TestMethod]
    public void Test68000_NextAddresses_UnconditionalBranch()
    {
        // Unconditional branch should have target address only
        var bytes = new byte[] { 0x60, 0x20 };
        var result = _disassembler.DecodeNext(bytes, 0x3000);  // BRA +32
        var target = 0x3000UL + 2 + 0x20; // Branch target
        AssertInstruction(result, "BRA", 2, bytes, 1, isBranch: true, isTerminator: true,
            nextAddresses: new List<ulong> { target });
    }
    
    [TestMethod]
    public void Test68000_NextAddresses_LongBranch()
    {
        // Long branch should have target address only
        var bytes = new byte[] { 0x60, 0x00, 0x10, 0x00 };
        var result = _disassembler.DecodeNext(bytes, 0x4000);  // BRA.L +4096
        var target = 0x4000UL + 2 + 0x1000; // Branch target
        AssertInstruction(result, "BRA", 4, bytes, 1, isBranch: true, isTerminator: true,
            nextAddresses: new List<ulong> { target });
    }
    
    [TestMethod]
    public void Test68000_NextAddresses_Jump()
    {
        // Jump should have target address only
        var bytes = new byte[] { 0x4E, 0xF9, 0x00, 0x00, 0x80, 0x00 };
        var result = _disassembler.DecodeNext(bytes, 0x5000);  // JMP $8000
        AssertInstruction(result, "JMP", 6, bytes, 1, isBranch: true, isTerminator: true,
            nextAddresses: new List<ulong> { 0x8000 });
    }
    
    [TestMethod]
    public void Test68000_NextAddresses_Call()
    {
        // Call should have target address only
        var bytes = new byte[] { 0x4E, 0xB9, 0x00, 0x00, 0x90, 0x00 };
        var result = _disassembler.DecodeNext(bytes, 0x6000);  // JSR $9000
        AssertInstruction(result, "JSR", 6, bytes, 1, isBranch: true, isTerminator: true,
            nextAddresses: new List<ulong> { 0x9000 });
    }
    
    [TestMethod]
    public void Test68000_NextAddresses_DBcc()
    {
        // DBcc should have both fall-through and target addresses
        var bytes = new byte[] { 0x51, 0xC8, 0x00, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x7000);  // DBRA D0,+16
        var fallThrough = 0x7000UL + 4;  // Address after instruction
        var target = 0x7000UL + 2 + 0x10; // Branch target
        AssertInstruction(result, "DBF", 4, bytes, 2, isBranch: true, 
            nextAddresses: new List<ulong> { fallThrough, target });
    }
    
    [TestMethod]
    public void Test68000_NextAddresses_Return()
    {
        // Return should have no next addresses
        var bytes = new byte[] { 0x4E, 0x75 };
        var result = _disassembler.DecodeNext(bytes, 0x8000);  // RTS
        AssertInstruction(result, "RTS", 2, bytes, isTerminator: true,
            nextAddresses: new List<ulong>());
    }
    
    [TestMethod]
    public void Test68000_NextAddresses_ReturnFromException()
    {
        // RTE should have no next addresses
        byte[] bytes = { 0x4E, 0x73 };
        var result = _disassembler.DecodeNext(bytes, 0x9000);  // RTE
        AssertInstruction(result, "RTE", 2, bytes, isTerminator: true,
            nextAddresses: new List<ulong>());
    }
    
    // Memory Access Tests
    [TestMethod]
    public void Test68000_MemoryAccesses_AddressIndirect()
    {
        // MOVE.L (A0),D1
        byte[] bytes = { 0x22, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x2000);
        Assert.IsTrue(result.Success);

        var registers = new M68000RegisterState();
        registers.AddressRegisters[0] = 0x12345678;
        registers.DataRegisters[1] = 0;

        var accesses = FetchMemoryAccesses(result.Instruction, registers);
        Assert.AreEqual(1, accesses.Count);
        Assert.AreEqual(0x12345678ul, accesses[0].address);
        Assert.AreEqual(4u, accesses[0].size);
    }

    [TestMethod]
    public void Test68000_MemoryAccesses_PostIncrement()
    {
        // MOVE.W (A1)+,D0
        byte[] bytes = { 0x30, 0x19 };
        var result = _disassembler.DecodeNext(bytes, 0x2000);
        Assert.IsTrue(result.Success);

        var registers = new M68000RegisterState();
        registers.AddressRegisters[1] = 0x5000;
        registers.DataRegisters[0] = 0;

        var accesses = FetchMemoryAccesses(result.Instruction, registers);
        Assert.AreEqual(1, accesses.Count);
        Assert.AreEqual(0x5000ul, accesses[0].address);
        Assert.AreEqual(2u, accesses[0].size);
    }

    [TestMethod]
    public void Test68000_MemoryAccesses_PreDecrement()
    {
        // MOVE.B -(A2),D0
        byte[] bytes = { 0x10, 0x22 };
        var result = _disassembler.DecodeNext(bytes, 0x2000);
        Assert.IsTrue(result.Success);

        var registers = new M68000RegisterState();
        registers.AddressRegisters[2] = 0x6000;
        registers.DataRegisters[0] = 0;

        var accesses = FetchMemoryAccesses(result.Instruction, registers);
        Assert.AreEqual(1, accesses.Count);
        Assert.AreEqual(0x6000ul, accesses[0].address);
        Assert.AreEqual(1u, accesses[0].size);
    }

    [TestMethod]
    public void Test68000_MemoryAccesses_Displacement_Positive()
    {
        // MOVE.L $10(A3),D0
        byte[] bytes = { 0x20, 0x2B, 0x00, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x2000);
        Assert.IsTrue(result.Success);

        var registers = new M68000RegisterState();
        registers.AddressRegisters[3] = 0x4000;
        registers.DataRegisters[0] = 0;

        var accesses = FetchMemoryAccesses(result.Instruction, registers);
        Assert.AreEqual(1, accesses.Count);
        Assert.AreEqual(0x4010ul, accesses[0].address);
        Assert.AreEqual(4u, accesses[0].size);
    }

    [TestMethod]
    public void Test68000_MemoryAccesses_Displacement_Negative()
    {
        // MOVE.L $FFF0(A4),D0 (negative displacement)
        byte[] bytes = { 0x20, 0x2C, 0xFF, 0xF0 };
        var result = _disassembler.DecodeNext(bytes, 0x2000);
        Assert.IsTrue(result.Success);

        var registers = new M68000RegisterState();
        registers.AddressRegisters[4] = 0x4100;
        registers.DataRegisters[0] = 0;

        var accesses = FetchMemoryAccesses(result.Instruction, registers);
        Assert.AreEqual(1, accesses.Count);
        Assert.AreEqual(0x40F0ul, accesses[0].address);
        Assert.AreEqual(4u, accesses[0].size);
    }

    [TestMethod]
    public void Test68000_MemoryAccesses_AbsoluteShort()
    {
        // MOVE.L $1234.W,D0
        byte[] bytes = { 0x20, 0x38, 0x12, 0x34 };
        var result = _disassembler.DecodeNext(bytes, 0x2000);
        Assert.IsTrue(result.Success);

        var registers = new M68000RegisterState();
        registers.DataRegisters[0] = 0;

        var accesses = FetchMemoryAccesses(result.Instruction, registers);
        Assert.AreEqual(1, accesses.Count);
        Assert.AreEqual(0x1234ul, accesses[0].address);
        Assert.AreEqual(4u, accesses[0].size);
    }

    [TestMethod]
    public void Test68000_MemoryAccesses_AbsoluteLong()
    {
        // MOVE.L $00FF0000.L,D0
        byte[] bytes = { 0x20, 0x39, 0x00, 0xFF, 0x00, 0x00 };
        var result = _disassembler.DecodeNext(bytes, 0x2000);
        Assert.IsTrue(result.Success);

        var registers = new M68000RegisterState();
        registers.DataRegisters[0] = 0;

        var accesses = FetchMemoryAccesses(result.Instruction, registers);
        Assert.AreEqual(1, accesses.Count);
        Assert.AreEqual(0x00FF0000ul, accesses[0].address);
        Assert.AreEqual(4u, accesses[0].size);
    }

    [TestMethod]
    public void Test68000_MemoryAccesses_PCRelativeDisplacement()
    {
        // MOVE.L $10(PC),D0
        byte[] bytes = { 0x20, 0x3A, 0x00, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x2000);
        Assert.IsTrue(result.Success);

        var registers = new M68000RegisterState();
        registers.DataRegisters[0] = 0;

        var accesses = FetchMemoryAccesses(result.Instruction, registers);
        Assert.AreEqual(1, accesses.Count);
        // PC = 0x2000 + 2 = 0x2002, EA = 0x2002 + 0x10 = 0x2012
        Assert.AreEqual(0x2012ul, accesses[0].address);
        Assert.AreEqual(4u, accesses[0].size);
    }

    [TestMethod]
    public void Test68000_MemoryAccesses_DataRegisterDirect_None()
    {
        // MOVE.L D1,D0 - no memory access
        byte[] bytes = { 0x20, 0x01 };
        var result = _disassembler.DecodeNext(bytes, 0x2000);
        Assert.IsTrue(result.Success);

        var registers = new M68000RegisterState();
        registers.DataRegisters[0] = 0;
        registers.DataRegisters[1] = 0x12345678;

        var accesses = FetchMemoryAccesses(result.Instruction, registers);
        Assert.AreEqual(0, accesses.Count);
    }

    [TestMethod]
    public void Test68000_MemoryAccesses_Immediate_None()
    {
        // MOVE.W #$1234,D0 - no memory access
        byte[] bytes = { 0x30, 0x3C, 0x12, 0x34 };
        var result = _disassembler.DecodeNext(bytes, 0x2000);
        Assert.IsTrue(result.Success);

        var registers = new M68000RegisterState();
        registers.DataRegisters[0] = 0;

        var accesses = FetchMemoryAccesses(result.Instruction, registers);
        Assert.AreEqual(0, accesses.Count);
    }

    [TestMethod]
    public void Test68000_MemoryAccesses_MOVE_SourceAndDestination()
    {
        // MOVE.L (A0),$1234.W - both source and destination are memory
        byte[] bytes = { 0x23, 0xD0, 0x12, 0x34 };
        var result = _disassembler.DecodeNext(bytes, 0x2000);
        Assert.IsTrue(result.Success);

        var registers = new M68000RegisterState();
        registers.AddressRegisters[0] = 0x7000;

        var accesses = FetchMemoryAccesses(result.Instruction, registers);
        Assert.AreEqual(2, accesses.Count);
        // Source: (A0) = 0x7000
        Assert.AreEqual(0x7000ul, accesses[0].address);
        Assert.AreEqual(4u, accesses[0].size);
        // Destination: $1234.W
        Assert.AreEqual(0x1234ul, accesses[1].address);
        Assert.AreEqual(4u, accesses[1].size);
    }

    [TestMethod]
    public void Test68000_MemoryAccesses_ByteSize()
    {
        // MOVE.B (A0),D0 - byte access
        byte[] bytes = { 0x10, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x2000);
        Assert.IsTrue(result.Success);

        var registers = new M68000RegisterState();
        registers.AddressRegisters[0] = 0x3000;

        var accesses = FetchMemoryAccesses(result.Instruction, registers);
        Assert.AreEqual(1, accesses.Count);
        Assert.AreEqual(0x3000ul, accesses[0].address);
        Assert.AreEqual(1u, accesses[0].size);
    }

    [TestMethod]
    public void Test68000_MemoryAccesses_WordSize()
    {
        // MOVE.W (A0),D0 - word access
        byte[] bytes = { 0x30, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x2000);
        Assert.IsTrue(result.Success);

        var registers = new M68000RegisterState();
        registers.AddressRegisters[0] = 0x4000;

        var accesses = FetchMemoryAccesses(result.Instruction, registers);
        Assert.AreEqual(1, accesses.Count);
        Assert.AreEqual(0x4000ul, accesses[0].address);
        Assert.AreEqual(2u, accesses[0].size);
    }
}

