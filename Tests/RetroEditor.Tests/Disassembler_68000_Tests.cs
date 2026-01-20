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
    
    private void AssertInstruction(
        DecodeResult result,
        string mnemonic,
        int bytesConsumed,
        int operandCount = 0,
        string[] operandText = null,
        bool isBranch = false,
        bool isTerminator = false)
    {
        Assert.IsTrue(result.Success);
        
        var instruction = result.Instruction;
        Assert.AreEqual(mnemonic, instruction.Mnemonic);
        Assert.AreEqual(bytesConsumed, result.BytesConsumed);
        
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
    }
    
    // Test basic move instructions
    [TestMethod]
    public void Test68000_MOVE_DataRegister_To_DataRegister()
    {
        // MOVE.L D0,D1
        byte[] bytes = { 0x22, 0x00 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "MOVE.L", 2, 2, new[] { "D0", "D1" });
    }
    
    [TestMethod]
    public void Test68000_MOVE_Immediate_To_DataRegister()
    {
        // MOVE.W #$1234,D0
        byte[] bytes = { 0x30, 0x3C, 0x12, 0x34 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "MOVE.W", 4, 2, new[] { "#$1234", "D0" });
    }
    
    [TestMethod]
    public void Test68000_MOVE_AbsoluteLong_To_DataRegister()
    {
        // MOVE.L $FF0000,D0
        byte[] bytes = { 0x20, 0x39, 0x00, 0xFF, 0x00, 0x00 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "MOVE.L", 6, 2, new[] { "$00FF0000.L", "D0" });
    }
    
    // Test arithmetic instructions
    [TestMethod]
    public void Test68000_ADD_DataRegister_To_DataRegister()
    {
        // ADD.L D1,D0
        byte[] bytes = { 0xD0, 0x81 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "ADD.L", 2, 2, new[] { "D1", "D0" });
    }
    
    [TestMethod]
    public void Test68000_ADDI_Immediate_To_DataRegister()
    {
        // ADDI.W #$10,D0
        byte[] bytes = { 0x06, 0x40, 0x00, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "ADDI.W", 4, 2, new[] { "#$0010", "D0" });
    }
    
    [TestMethod]
    public void Test68000_ADDQ_Quick_To_DataRegister()
    {
        // ADDQ.L #1,D0
        byte[] bytes = { 0x52, 0x80 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "ADDQ.L", 2, 2, new[] { "#$01", "D0" });
    }
    
    [TestMethod]
    public void Test68000_SUB_DataRegister_From_DataRegister()
    {
        // SUB.L D1,D0
        byte[] bytes = { 0x90, 0x81 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "SUB.L", 2, 2, new[] { "D1", "D0" });
    }
    
    [TestMethod]
    public void Test68000_SUBQ_Quick_From_DataRegister()
    {
        // SUBQ.L #1,D0
        byte[] bytes = { 0x53, 0x80 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "SUBQ.L", 2, 2, new[] { "#$01", "D0" });
    }
    
    // Test logical instructions
    [TestMethod]
    public void Test68000_AND_DataRegister_To_DataRegister()
    {
        // AND.L D1,D0
        byte[] bytes = { 0xC0, 0x81 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "AND.L", 2, 2, new[] { "D1", "D0" });
    }
    
    [TestMethod]
    public void Test68000_OR_DataRegister_To_DataRegister()
    {
        // OR.L D1,D0
        byte[] bytes = { 0x80, 0x81 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "OR.L", 2, 2, new[] { "D1", "D0" });
    }
    
    [TestMethod]
    public void Test68000_EOR_DataRegister_To_DataRegister()
    {
        // EOR.L D0,D1
        byte[] bytes = { 0xB1, 0x81 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "EOR.L", 2, 2, new[] { "D0", "D1" });
    }
    
    [TestMethod]
    public void Test68000_NOT_DataRegister()
    {
        // NOT.L D0
        byte[] bytes = { 0x46, 0x80 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "NOT.L", 2, 1, new[] { "D0" });
    }
    
    // Test compare instructions
    [TestMethod]
    public void Test68000_CMP_DataRegister_To_DataRegister()
    {
        // CMP.L D1,D0
        byte[] bytes = { 0xB0, 0x81 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "CMP.L", 2, 2, new[] { "D1", "D0" });
    }
    
    [TestMethod]
    public void Test68000_CMPI_Immediate_To_DataRegister()
    {
        // CMPI.L #$1234,D0
        byte[] bytes = { 0x0C, 0x80, 0x00, 0x00, 0x12, 0x34 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "CMPI.L", 6, 2, new[] { "#$00001234", "D0" });
    }
    
    [TestMethod]
    public void Test68000_TST_DataRegister()
    {
        // TST.L D0
        byte[] bytes = { 0x4A, 0x80 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "TST.L", 2, 1, new[] { "D0" });
    }
    
    // Test branch instructions
    [TestMethod]
    public void Test68000_BRA_Short()
    {
        // BRA.S $10 (relative)
        byte[] bytes = { 0x60, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "BRA", 2, 1, new[] { "$001012.L" }, isBranch: true, isTerminator: true);
    }
    
    [TestMethod]
    public void Test68000_BRA_Long()
    {
        // BRA.W $1234 (relative)
        byte[] bytes = { 0x60, 0x00, 0x12, 0x34 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "BRA", 4, 1, new[] { "$002236.L" }, isBranch: true, isTerminator: true);
    }
    
    [TestMethod]
    public void Test68000_BEQ_Short()
    {
        // BEQ.S $10 (relative)
        byte[] bytes = { 0x67, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "BEQ", 2, 1, new[] { "$001012.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_BNE_Short()
    {
        // BNE.S $10 (relative)
        byte[] bytes = { 0x66, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "BNE", 2, 1, new[] { "$001012.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_BSR()
    {
        // BSR.S $10 (relative)
        byte[] bytes = { 0x61, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "BSR", 2, 1, new[] { "$001012.L" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_DBcc()
    {
        // DBRA D0,$10 (relative)
        byte[] bytes = { 0x51, 0xC8, 0x00, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "DBF", 4, 2, new[] { "D0", "$001012.L" }, isBranch: true, isTerminator: false);
    }
    
    // Test jump instructions
    [TestMethod]
    public void Test68000_JMP_AbsoluteLong()
    {
        // JMP $1234.L
        byte[] bytes = { 0x4E, 0xF9, 0x00, 0x00, 0x12, 0x34 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "JMP", 6, 1, new[] { "$00001234.L" }, isBranch: true, isTerminator: true);
    }
    
    [TestMethod]
    public void Test68000_JSR_AbsoluteLong()
    {
        // JSR $1234.L
        byte[] bytes = { 0x4E, 0xB9, 0x00, 0x00, 0x12, 0x34 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "JSR", 6, 1, new[] { "$00001234.L" }, isBranch: true, isTerminator: false);
    }
    
    // Test return instructions
    [TestMethod]
    public void Test68000_RTS()
    {
        // RTS
        byte[] bytes = { 0x4E, 0x75 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "RTS", 2, 0, null, isBranch: true, isTerminator: true);
    }
    
    [TestMethod]
    public void Test68000_RTE()
    {
        // RTE
        byte[] bytes = { 0x4E, 0x73 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "RTE", 2, 0, null, isBranch: true, isTerminator: true);
    }
    
    [TestMethod]
    public void Test68000_RTR()
    {
        // RTR
        byte[] bytes = { 0x4E, 0x77 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "RTR", 2, 0, null, isBranch: true, isTerminator: true);
    }
    
    // Test shift/rotate instructions
    [TestMethod]
    public void Test68000_LSL_Immediate()
    {
        // LSL.L #1,D0
        byte[] bytes = { 0xE3, 0x88 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "LSL.L", 2, 2, new[] { "#$01", "D0" });
    }
    
    [TestMethod]
    public void Test68000_LSR_Immediate()
    {
        // LSR.L #1,D0
        byte[] bytes = { 0xE2, 0x88 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "LSR.L", 2, 2, new[] { "#$01", "D0" });
    }
    
    [TestMethod]
    public void Test68000_ASL_Immediate()
    {
        // ASL.L #1,D0
        byte[] bytes = { 0xE3, 0x80 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "ASL.L", 2, 2, new[] { "#$01", "D0" });
    }
    
    [TestMethod]
    public void Test68000_ASR_Immediate()
    {
        // ASR.L #1,D0
        byte[] bytes = { 0xE2, 0x80 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "ASR.L", 2, 2, new[] { "#$01", "D0" });
    }
    
    // Test multiply and divide
    [TestMethod]
    public void Test68000_MULU()
    {
        // MULU.W D1,D0
        byte[] bytes = { 0xC0, 0xC1 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "MULU", 2, 2, new[] { "D1", "D0" });
    }
    
    [TestMethod]
    public void Test68000_MULS()
    {
        // MULS.W D1,D0
        byte[] bytes = { 0xC1, 0xC1 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "MULS", 2, 2, new[] { "D1", "D0" });
    }
    
    [TestMethod]
    public void Test68000_DIVU()
    {
        // DIVU.W D1,D0
        byte[] bytes = { 0x80, 0xC1 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "DIVU", 2, 2, new[] { "D1", "D0" });
    }
    
    [TestMethod]
    public void Test68000_DIVS()
    {
        // DIVS.W D1,D0
        byte[] bytes = { 0x81, 0xC1 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "DIVS", 2, 2, new[] { "D1", "D0" });
    }
    
    // Test addressing modes
    [TestMethod]
    public void Test68000_AddressIndirect()
    {
        // MOVE.L (A0),D0
        byte[] bytes = { 0x20, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "MOVE.L", 2, 2, new[] { "(A0)", "D0" });
    }
    
    [TestMethod]
    public void Test68000_AddressPostIncrement()
    {
        // MOVE.L (A0)+,D0
        byte[] bytes = { 0x20, 0x18 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "MOVE.L", 2, 2, new[] { "(A0)+", "D0" });
    }
    
    [TestMethod]
    public void Test68000_AddressPreDecrement()
    {
        // MOVE.L -(A0),D0
        byte[] bytes = { 0x20, 0x20 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "MOVE.L", 2, 2, new[] { "-(A0)", "D0" });
    }
    
    [TestMethod]
    public void Test68000_AddressDisplacement()
    {
        // MOVE.L $10(A0),D0
        byte[] bytes = { 0x20, 0x28, 0x00, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "MOVE.L", 4, 2, new[] { "16(A0)", "D0" });
    }
    
    // Test miscellaneous instructions
    [TestMethod]
    public void Test68000_NOP()
    {
        // NOP
        byte[] bytes = { 0x4E, 0x71 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "NOP", 2, 0);
    }
    
    [TestMethod]
    public void Test68000_MOVEQ()
    {
        // MOVEQ #$12,D0
        byte[] bytes = { 0x70, 0x12 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "MOVEQ", 2, 2, new[] { "#$12", "D0" });
    }
    
    [TestMethod]
    public void Test68000_LEA()
    {
        // LEA $1234.L,A0
        byte[] bytes = { 0x41, 0xF9, 0x00, 0x00, 0x12, 0x34 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "LEA", 6, 2, new[] { "$00001234.L", "A0" });
    }
    
    [TestMethod]
    public void Test68000_CLR()
    {
        // CLR.L D0
        byte[] bytes = { 0x42, 0x80 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "CLR.L", 2, 1, new[] { "D0" });
    }
    
    [TestMethod]
    public void Test68000_NEG()
    {
        // NEG.L D0
        byte[] bytes = { 0x44, 0x80 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "NEG.L", 2, 1, new[] { "D0" });
    }
    
    [TestMethod]
    public void Test68000_EXT_Word()
    {
        // EXT.W D0
        byte[] bytes = { 0x48, 0x80 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "EXT.W", 2, 1, new[] { "D0" });
    }
    
    [TestMethod]
    public void Test68000_EXT_Long()
    {
        // EXT.L D0
        byte[] bytes = { 0x48, 0xC0 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "EXT.L", 2, 1, new[] { "D0" });
    }
    
    [TestMethod]
    public void Test68000_SWAP()
    {
        // SWAP D0
        byte[] bytes = { 0x48, 0x40 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "SWAP", 2, 1, new[] { "D0" });
    }
    
    [TestMethod]
    public void Test68000_EXG_DataRegisters()
    {
        // EXG D0,D1
        byte[] bytes = { 0xC1, 0x41 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "EXG", 2, 2, new[] { "D0", "D1" });
    }
    
    [TestMethod]
    public void Test68000_TRAP()
    {
        // TRAP #0
        byte[] bytes = { 0x4E, 0x40 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "TRAP", 2, 1, new[] { "#$00" }, isBranch: true, isTerminator: false);
    }
    
    [TestMethod]
    public void Test68000_LINK()
    {
        // LINK A6,#-$10
        byte[] bytes = { 0x4E, 0x56, 0xFF, 0xF0 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "LINK", 4, 2, new[] { "A6", "#$FFF0" });
    }
    
    [TestMethod]
    public void Test68000_UNLK()
    {
        // UNLK A6
        byte[] bytes = { 0x4E, 0x5E };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "UNLK", 2, 1, new[] { "A6" });
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
}
