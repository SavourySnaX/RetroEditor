using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroEditor.Plugins;

namespace RetroEditor.Tests;

/// <summary>
/// Unit tests for the Zilog Z80 disassembler
/// </summary>
[TestClass]
public class Disassembler_Z80_Tests
{
    private Z80Disassembler _disassembler;
    
    [TestInitialize]
    public void Setup()
    {
        _disassembler = new Z80Disassembler();
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

    // Test basic instructions
    [TestMethod]
    public void TestZ80_NOP()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0x00 }, 0x0000);
        AssertInstruction(result, "NOP", 1);
    }

    [TestMethod]
    public void TestZ80_HALT()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0x76 }, 0x0000);
        AssertInstruction(result, "HALT", 1, isTerminator: true);
    }

    [TestMethod]
    public void TestZ80_EX_DE_HL()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0xEB }, 0x0000);
        AssertInstruction(result, "EX", 1, 2, new[] { "DE", "HL" });
    }

    // Test 8-bit register loads
    [TestMethod]
    public void TestZ80_LD_B_Immediate()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0x06, 0x42 }, 0x0000);
        AssertInstruction(result, "LD", 2, 2, new[] { "B", "#42" });
    }

    [TestMethod]
    public void TestZ80_LD_A_Immediate()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0x3E, 0xFF }, 0x0000);
        AssertInstruction(result, "LD", 2, 2, new[] { "A", "#FF" });
    }

    [TestMethod]
    public void TestZ80_LD_B_C()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0x41 }, 0x0000);
        AssertInstruction(result, "LD", 1, 2, new[] { "B", "C" });
    }

    // Test 16-bit register loads
    [TestMethod]
    public void TestZ80_LD_BC_Immediate()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0x01, 0x34, 0x12 }, 0x0000);
        AssertInstruction(result, "LD", 3, 2, new[] { "BC", "#1234" });
    }

    [TestMethod]
    public void TestZ80_LD_HL_Immediate()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0x21, 0xAB, 0xCD }, 0x0000);
        AssertInstruction(result, "LD", 3, 2, new[] { "HL", "#CDAB" });
    }

    // Test arithmetic operations
    [TestMethod]
    public void TestZ80_ADD_A_B()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0x80 }, 0x0000);
        AssertInstruction(result, "ADD", 1, 2, new[] { "A", "B" });
    }

    [TestMethod]
    public void TestZ80_ADD_A_Immediate()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0xC6, 0x20 }, 0x0000);
        AssertInstruction(result, "ADD", 2, 2, new[] { "A", "#20" });
    }

    [TestMethod]
    public void TestZ80_SUB_A_C()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0x91 }, 0x0000);
        AssertInstruction(result, "SUB", 1, 2, new[] { "A", "C" });
    }

    [TestMethod]
    public void TestZ80_CP_A_D()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0xBA }, 0x0000);
        AssertInstruction(result, "CP", 1, 2, new[] { "A", "D" });
    }

    // Test increment/decrement
    [TestMethod]
    public void TestZ80_INC_A()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0x3C }, 0x0000);
        AssertInstruction(result, "INC", 1, 1, new[] { "A" });
    }

    [TestMethod]
    public void TestZ80_DEC_B()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0x05 }, 0x0000);
        AssertInstruction(result, "DEC", 1, 1, new[] { "B" });
    }

    [TestMethod]
    public void TestZ80_INC_BC()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0x03 }, 0x0000);
        AssertInstruction(result, "INC", 1, 1, new[] { "BC" });
    }

    // Test rotates
    [TestMethod]
    public void TestZ80_RLCA()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0x07 }, 0x0000);
        AssertInstruction(result, "RLCA", 1);
    }

    [TestMethod]
    public void TestZ80_RRCA()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0x0F }, 0x0000);
        AssertInstruction(result, "RRCA", 1);
    }

    // Test jumps
    [TestMethod]
    public void TestZ80_JP_Absolute()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0xC3, 0x00, 0x20 }, 0x0000);
        AssertInstruction(result, "JP", 3, 1, new[] { "$2000" }, isBranch: true, isTerminator: true);
    }

    [TestMethod]
    public void TestZ80_JR_Relative()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0x18, 0x10 }, 0x1000);
        AssertInstruction(result, "JR", 2, 1, isBranch: true);
    }

    [TestMethod]
    public void TestZ80_DJNZ()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0x10, 0x05 }, 0x0000);
        AssertInstruction(result, "DJNZ", 2, 1, isBranch: true);
    }

    // Test calls
    [TestMethod]
    public void TestZ80_CALL_Absolute()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0xCD, 0x50, 0x10 }, 0x0000);
        AssertInstruction(result, "CALL", 3, 1, new[] { "$1050" }, isBranch: true);
    }

    [TestMethod]
    public void TestZ80_RET()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0xC9 }, 0x0000);
        AssertInstruction(result, "RET", 1, isTerminator: true);
    }

    // Test RST (Restart)
    [TestMethod]
    public void TestZ80_RST_00()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0xC7 }, 0x0000);
        AssertInstruction(result, "RST", 1, 1, new[] { "$0000" }, isBranch: true);
    }

    [TestMethod]
    public void TestZ80_RST_38()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0xFF }, 0x0000);
        AssertInstruction(result, "RST", 1, 1, new[] { "$0038" }, isBranch: true);
    }

    // Test push/pop
    [TestMethod]
    public void TestZ80_PUSH_BC()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0xC5 }, 0x0000);
        AssertInstruction(result, "PUSH", 1, 1, new[] { "BC" });
    }

    [TestMethod]
    public void TestZ80_POP_HL()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0xE1 }, 0x0000);
        AssertInstruction(result, "POP", 1, 1, new[] { "HL" });
    }

    // Test I/O
    [TestMethod]
    public void TestZ80_IN_A_Port()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0xDB, 0x50 }, 0x0000);
        AssertInstruction(result, "IN", 2, 2, new[] { "A", "#50" });
    }

    [TestMethod]
    public void TestZ80_OUT_Port_A()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0xD3, 0xFE }, 0x0000);
        AssertInstruction(result, "OUT", 2, 2, new[] { "#FE", "A" });
    }

    // Test control
    [TestMethod]
    public void TestZ80_DI()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0xF3 }, 0x0000);
        AssertInstruction(result, "DI", 1);
    }

    [TestMethod]
    public void TestZ80_EI()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0xFB }, 0x0000);
        AssertInstruction(result, "EI", 1);
    }

    // Test CB-prefixed bit manipulation
    [TestMethod]
    public void TestZ80_BIT_0_A()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0xCB, 0x47 }, 0x0000);
        AssertInstruction(result, "BIT", 2, 2, new[] { "0", "A" });
    }

    [TestMethod]
    public void TestZ80_SET_3_B()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0xCB, 0xD8 }, 0x0000);
        AssertInstruction(result, "SET", 2, 2, new[] { "3", "B" });
    }

    [TestMethod]
    public void TestZ80_RES_5_C()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0xCB, 0xA9 }, 0x0000);
        AssertInstruction(result, "RES", 2, 2, new[] { "5", "C" });
    }

    [TestMethod]
    public void TestZ80_RLC_D()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0xCB, 0x02 }, 0x0000);
        AssertInstruction(result, "RLC", 2, 1, new[] { "D" });
    }

    // Test ED-prefixed extended instructions
    [TestMethod]
    public void TestZ80_NEG()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0xED, 0x44 }, 0x0000);
        AssertInstruction(result, "NEG", 2);
    }

    [TestMethod]
    public void TestZ80_LDI()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0xED, 0xA0 }, 0x0000);
        AssertInstruction(result, "LDI", 2);
    }

    [TestMethod]
    public void TestZ80_LDIR()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0xED, 0xB0 }, 0x0000);
        AssertInstruction(result, "LDIR", 2, isTerminator: true);
    }

    // Test edge cases
    [TestMethod]
    public void TestZ80_InsufficientData()
    {
        var result = _disassembler.DecodeNext(new byte[] { 0x01 }, 0x0000);
        Assert.IsFalse(result.Success);
    }

    [TestMethod]
    public void TestZ80_EmptyData()
    {
        var result = _disassembler.DecodeNext(new byte[] { }, 0x0000);
        Assert.IsFalse(result.Success);
    }

    // Test architecture properties
    [TestMethod]
    public void TestZ80_ArchitectureName()
    {
        Assert.AreEqual("Z80", _disassembler.ArchitectureName);
    }
}
