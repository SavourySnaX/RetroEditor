using System.Collections.Generic;
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
        if (expectedBytes != null)
        {
            Assert.AreEqual(bytesConsumed, instruction.Bytes.Length, "Instruction bytes length mismatch");
            CollectionAssert.AreEqual(expectedBytes, instruction.Bytes, "Instruction bytes mismatch");
        }
        
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

    // Convenience overload for testing with byte arrays
    private void AssertInstruction(string expectedOutput, byte[] bytes)
    {
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        Assert.IsTrue(result.Success);
        
        // Remove address prefix for comparison
        var actualOutput = result.Instruction.ToString();
        if (actualOutput.StartsWith("00000000: "))
        {
            actualOutput = actualOutput.Substring("00000000: ".Length);
        }
        
        // Verify the bytes match
        if (bytes != null)
        {
            Assert.AreEqual(bytes.Length, result.Instruction.Bytes.Length, "Instruction bytes length mismatch");
            CollectionAssert.AreEqual(bytes, result.Instruction.Bytes, "Instruction bytes mismatch");
        }
        
        // Trim trailing spaces
        actualOutput = actualOutput.Trim();
        
        Assert.AreEqual(expectedOutput, actualOutput);
    }

    // Test basic instructions
    [TestMethod]
    public void TestZ80_NOP()
    {
        var bytes = new byte[] { 0x00 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "NOP", 1, bytes);
    }

    [TestMethod]
    public void TestZ80_HALT()
    {
        var bytes = new byte[] { 0x76 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "HALT", 1, bytes, isTerminator: true);
    }

    [TestMethod]
    public void TestZ80_EX_DE_HL()
    {
        var bytes = new byte[] { 0xEB };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "EX", 1, bytes, 2, new[] { "DE", "HL" });
    }

    // Test 8-bit register loads
    [TestMethod]
    public void TestZ80_LD_B_Immediate()
    {
        var bytes = new byte[] { 0x06, 0x42 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "LD", 2, bytes, 2, new[] { "B", "#42" });
    }

    [TestMethod]
    public void TestZ80_LD_A_Immediate()
    {
        var bytes = new byte[] { 0x3E, 0xFF };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "LD", 2, bytes, 2, new[] { "A", "#FF" });
    }

    [TestMethod]
    public void TestZ80_LD_HL_Memory_Immediate()
    {
        var bytes = new byte[] { 0x36, 0x02 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "LD", 2, bytes, 2, new[] { "(HL)", "#02" });
    }

    [TestMethod]
    public void TestZ80_LD_B_C()
    {
        var bytes = new byte[] { 0x41 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "LD", 1, bytes, 2, new[] { "B", "C" });
    }

    // Test 16-bit register loads
    [TestMethod]
    public void TestZ80_LD_BC_Immediate()
    {
        var bytes = new byte[] { 0x01, 0x34, 0x12 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "LD", 3, bytes, 2, new[] { "BC", "#1234" });
    }

    [TestMethod]
    public void TestZ80_LD_HL_Immediate()
    {
        var bytes = new byte[] { 0x21, 0xAB, 0xCD };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "LD", 3, bytes, 2, new[] { "HL", "#CDAB" });
    }

    // Test arithmetic operations
    [TestMethod]
    public void TestZ80_ADD_A_B()
    {
        var bytes = new byte[] { 0x80 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "ADD", 1, bytes, 2, new[] { "A", "B" });
    }

    [TestMethod]
    public void TestZ80_ADD_A_Immediate()
    {
        var bytes = new byte[] { 0xC6, 0x20 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "ADD", 2, bytes, 2, new[] { "A", "#20" });
    }

    [TestMethod]
    public void TestZ80_SUB_A_C()
    {
        var bytes = new byte[] { 0x91 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "SUB", 1, bytes, 2, new[] { "A", "C" });
    }

    [TestMethod]
    public void TestZ80_CP_A_D()
    {
        var bytes = new byte[] { 0xBA };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "CP", 1, bytes, 2, new[] { "A", "D" });
    }

    // Test increment/decrement
    [TestMethod]
    public void TestZ80_INC_A()
    {
        var bytes = new byte[] { 0x3C };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "INC", 1, bytes, 1, new[] { "A" });
    }

    [TestMethod]
    public void TestZ80_DEC_B()
    {
        var bytes = new byte[] { 0x05 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "DEC", 1, bytes, 1, new[] { "B" });
    }

    [TestMethod]
    public void TestZ80_INC_HL_Memory()
    {
        var bytes = new byte[] { 0x34 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "INC", 1, bytes, 1, new[] { "(HL)" });
    }

    [TestMethod]
    public void TestZ80_DEC_HL_Memory()
    {
        var bytes = new byte[] { 0x35 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "DEC", 1, bytes, 1, new[] { "(HL)" });
    }

    [TestMethod]
    public void TestZ80_INC_BC()
    {
        var bytes = new byte[] { 0x03 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "INC", 1, bytes, 1, new[] { "BC" });
    }

    // Test rotates
    [TestMethod]
    public void TestZ80_RLCA()
    {
        var bytes = new byte[] { 0x07 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "RLCA", 1, bytes);
    }

    [TestMethod]
    public void TestZ80_RRCA()
    {
        var bytes = new byte[] { 0x0F };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "RRCA", 1, bytes);
    }

    // Test jumps
    [TestMethod]
    public void TestZ80_JP_Absolute()
    {
        var bytes = new byte[] { 0xC3, 0x00, 0x20 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "JP", 3, bytes, 1, new[] { "$2000" }, isBranch: true, isTerminator: true);
    }

    [TestMethod]
    public void TestZ80_JR_Relative()
    {
        var bytes = new byte[] { 0x18, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);
        AssertInstruction(result, "JR", 2, bytes, 1, isBranch: true);
    }

    [TestMethod]
    public void TestZ80_DJNZ()
    {
        var bytes = new byte[] { 0x10, 0x05 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "DJNZ", 2, bytes, 1, isBranch: true);
    }

    // Test calls
    [TestMethod]
    public void TestZ80_CALL_Absolute()
    {
        var bytes = new byte[] { 0xCD, 0x50, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "CALL", 3, bytes, 1, new[] { "$1050" }, isBranch: true);
    }

    [TestMethod]
    public void TestZ80_RET()
    {
        var bytes = new byte[] { 0xC9 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "RET", 1, bytes, isTerminator: true);
    }

    // Test RST (Restart)
    [TestMethod]
    public void TestZ80_RST_00()
    {
        var bytes = new byte[] { 0xC7 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "RST", 1, bytes, 1, new[] { "$0000" }, isBranch: true, isTerminator: true);
    }

    [TestMethod]
    public void TestZ80_RST_38()
    {
        var bytes = new byte[] { 0xFF };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "RST", 1, bytes, 1, new[] { "$0038" }, isBranch: true, isTerminator: true);
    }

    // Test push/pop
    [TestMethod]
    public void TestZ80_PUSH_BC()
    {
        var bytes = new byte[] { 0xC5 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "PUSH", 1, bytes, 1, new[] { "BC" });
    }

    [TestMethod]
    public void TestZ80_POP_HL()
    {
        var bytes = new byte[] { 0xE1 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "POP", 1, bytes, 1, new[] { "HL" });
    }

    // Test I/O
    [TestMethod]
    public void TestZ80_IN_A_Port()
    {
        var bytes = new byte[] { 0xDB, 0x50 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "IN", 2, bytes, 2, new[] { "A", "#50" });
    }

    [TestMethod]
    public void TestZ80_OUT_Port_A()
    {
        var bytes = new byte[] { 0xD3, 0xFE };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "OUT", 2, bytes, 2, new[] { "#FE", "A" });
    }

    // Test control
    [TestMethod]
    public void TestZ80_DI()
    {
        var bytes = new byte[] { 0xF3 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "DI", 1, bytes);
    }

    [TestMethod]
    public void TestZ80_EI()
    {
        var bytes = new byte[] { 0xFB };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "EI", 1, bytes);
    }

    [TestMethod]
    public void TestZ80_LD_SP_HL()
    {
        var bytes = new byte[] { 0xF9 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "LD", 1, bytes, 2, new[] { "SP", "HL" });
    }

    // Test CB-prefixed bit manipulation
    [TestMethod]
    public void TestZ80_BIT_0_A()
    {
        var bytes = new byte[] { 0xCB, 0x47 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "BIT", 2, bytes, 2, new[] { "0", "A" });
    }

    [TestMethod]
    public void TestZ80_SET_3_B()
    {
        var bytes = new byte[] { 0xCB, 0xD8 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "SET", 2, bytes, 2, new[] { "3", "B" });
    }

    [TestMethod]
    public void TestZ80_RES_5_C()
    {
        var bytes = new byte[] { 0xCB, 0xA9 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "RES", 2, bytes, 2, new[] { "5", "C" });
    }

    [TestMethod]
    public void TestZ80_RLC_D()
    {
        var bytes = new byte[] { 0xCB, 0x02 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "RLC", 2, bytes, 1, new[] { "D" });
    }

    // Test ED-prefixed extended instructions
    [TestMethod]
    public void TestZ80_NEG()
    {
        var bytes = new byte[] { 0xED, 0x44 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "NEG", 2, bytes);
    }

    [TestMethod]
    public void TestZ80_LDI()
    {
        var bytes = new byte[] { 0xED, 0xA0 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "LDI", 2, bytes);
    }

    [TestMethod]
    public void TestZ80_LDIR()
    {
        var bytes = new byte[] { 0xED, 0xB0 };
        var result = _disassembler.DecodeNext(bytes, 0x0000);
        AssertInstruction(result, "LDIR", 2, bytes, isTerminator: true);
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
    
    // Next Address Tests
    [TestMethod]
    public void TestZ80_NextAddresses_RegularInstruction()
    {
        // Regular instruction should have fall-through address only
        var bytes = new byte[] { 0xF3 };  // DI
        var result = _disassembler.DecodeNext(bytes, 0x1000);  // DI
        AssertInstruction(result, "DI", 1, bytes,
            nextAddresses: new List<ulong> { 0x1001 });
    }
    
    [TestMethod]
    public void TestZ80_NextAddresses_ConditionalJump()
    {
        // Conditional jump should have both fall-through and target addresses
        var bytes = new byte[] { 0xC2, 0x00, 0x20 };
        var result = _disassembler.DecodeNext(bytes, 0x1000);  // JP NZ,$2000
        var fallThrough = 0x1000UL + 3;  // Address after instruction
        var target = 0x2000UL; // Jump target
        AssertInstruction(result, "JP", 3, bytes, 2, new[] { "NZ", "$2000" }, isBranch: true, 
            nextAddresses: new List<ulong> { fallThrough, target });
    }
    
    [TestMethod]
    public void TestZ80_NextAddresses_ConditionalCall()
    {
        // Conditional call should have both fall-through and target addresses
        var bytes = new byte[] { 0xC4, 0x00, 0x30 };
        var result = _disassembler.DecodeNext(bytes, 0x2000);  // CALL NZ,$3000
        var fallThrough = 0x2000UL + 3;  // Address after instruction
        var target = 0x3000UL; // Call target
        AssertInstruction(result, "CALL", 3, bytes, 2, new[] { "NZ", "$3000" }, isBranch: true, 
            nextAddresses: new List<ulong> { fallThrough, target });
    }
    
    [TestMethod]
    public void TestZ80_NextAddresses_ConditionalReturn()
    {
        // Conditional return should have both fall-through and no target (unknown)
        var bytes = new byte[] { 0xC0 };
        var result = _disassembler.DecodeNext(bytes, 0x3000);  // RET NZ
        var fallThrough = 0x3000UL + 1;  // Address after instruction
        AssertInstruction(result, "RET", 1, bytes, 1, new[] { "NZ" }, isBranch: true, 
            nextAddresses: new List<ulong> { fallThrough });
    }
    
    [TestMethod]
    public void TestZ80_NextAddresses_ConditionalRelativeJump()
    {
        // Conditional relative jump should have both fall-through and target addresses
        var bytes = new byte[] { 0x20, 0x10 };
        var result = _disassembler.DecodeNext(bytes, 0x4000);  // JR NZ,+16
        var fallThrough = 0x4000UL + 2;  // Address after instruction
        var target = 0x4000UL + 2 + 0x10; // Relative jump target
        AssertInstruction(result, "JR", 2, bytes, 2, isBranch: true, 
            nextAddresses: new List<ulong> { fallThrough, target });
    }
    
    [TestMethod]
    public void TestZ80_NextAddresses_DJNZ()
    {
        // DJNZ should have both fall-through and target addresses
        var bytes = new byte[] { 0x10, 0x08 };
        var result = _disassembler.DecodeNext(bytes, 0x5000);  // DJNZ +8
        var fallThrough = 0x5000UL + 2;  // Address after instruction
        var target = 0x5000UL + 2 + 0x08; // DJNZ target
        AssertInstruction(result, "DJNZ", 2, bytes, 1, isBranch: true, 
            nextAddresses: new List<ulong> { fallThrough, target });
    }
    
    [TestMethod]
    public void TestZ80_NextAddresses_UnconditionalJump()
    {
        // Unconditional jump should have target address only
        var bytes = new byte[] { 0xC3, 0x00, 0x80 };
        var result = _disassembler.DecodeNext(bytes, 0x6000);  // JP $8000
        AssertInstruction(result, "JP", 3, bytes, 1, new[] { "$8000" }, isBranch: true, isTerminator: true,
            nextAddresses: new List<ulong> { 0x8000 });
    }
    
    [TestMethod]
    public void TestZ80_NextAddresses_UnconditionalRelativeJump()
    {
        // Unconditional relative jump should have target address only
        var bytes = new byte[] { 0x18, 0x20 };
        var result = _disassembler.DecodeNext(bytes, 0x7000);  // JR +32
        var target = 0x7000UL + 2 + 0x20; // Relative jump target
        AssertInstruction(result, "JR", 2, bytes, 1, isBranch: true, 
            nextAddresses: new List<ulong> { target, 0x7002 });
    }
    
    [TestMethod]
    public void TestZ80_NextAddresses_UnconditionalCall()
    {
        // Unconditional call should have target address only
        var bytes = new byte[] { 0xCD, 0x00, 0x90 };
        var result = _disassembler.DecodeNext(bytes, 0x8000);  // CALL $9000
        AssertInstruction(result, "CALL", 3, bytes, 1, new[] { "$9000" }, isBranch: true,
            nextAddresses: new List<ulong> { 0x9000, 0x8003 });
    }
    
    [TestMethod]
    public void TestZ80_NextAddresses_Return()
    {
        // Return should have no next addresses
        var bytes = new byte[] { 0xC9 };
        var result = _disassembler.DecodeNext(bytes, 0x9000);  // RET
        AssertInstruction(result, "RET", 1, bytes, isTerminator: true,
            nextAddresses: new List<ulong>());
    }
    
    [TestMethod]
    public void TestZ80_NextAddresses_HALT()
    {
        // HALT should have no next addresses
        var bytes = new byte[] { 0x76 };
        var result = _disassembler.DecodeNext(bytes, 0xA000);  // HALT
        AssertInstruction(result, "HALT", 1, bytes, isTerminator: true,
            nextAddresses: new List<ulong>());
    }
    
    [TestMethod]
    public void TestZ80_NextAddresses_RST()
    {
        // RST should have target address only
        var bytes = new byte[] { 0xC7 };
        var result = _disassembler.DecodeNext(bytes, 0xB000);  // RST $0000
        AssertInstruction(result, "RST", 1, bytes, 1, new[] { "$0000" }, isBranch: true, isTerminator: true,
            nextAddresses: new List<ulong> { 0x0000 });
    }
    
    [TestMethod]
    public void TestZ80_NextAddresses_LDIR()
    {
        // LDIR should have no next addresses (it's a terminator)
        var bytes = new byte[] { 0xED, 0xB0 };
        var result = _disassembler.DecodeNext(bytes, 0xC000);  // LDIR
        AssertInstruction(result, "LDIR", 2, bytes, isTerminator: true,
            nextAddresses: new List<ulong>());
    }

    [TestMethod]
    public void TestZ80_ED_Instructions()
    {
        // Test ED prefix extended instructions
        AssertInstruction("IM 0", new byte[] { 0xED, 0x46 });
        AssertInstruction("LD I, A", new byte[] { 0xED, 0x47 });
        AssertInstruction("LD R, A", new byte[] { 0xED, 0x4F });
        AssertInstruction("IM 1", new byte[] { 0xED, 0x56 });
        AssertInstruction("LD A, I", new byte[] { 0xED, 0x57 });
        AssertInstruction("IM 2", new byte[] { 0xED, 0x5E });
        AssertInstruction("LD A, R", new byte[] { 0xED, 0x5F });
        AssertInstruction("RRD", new byte[] { 0xED, 0x67 });
        AssertInstruction("RLD", new byte[] { 0xED, 0x6F });
        
        // Test 16-bit loads
        AssertInstruction("LD ($1234), BC", new byte[] { 0xED, 0x43, 0x34, 0x12 });
        AssertInstruction("LD BC, ($1234)", new byte[] { 0xED, 0x4B, 0x34, 0x12 });
        AssertInstruction("LD ($1234), DE", new byte[] { 0xED, 0x53, 0x34, 0x12 });
        AssertInstruction("LD DE, ($1234)", new byte[] { 0xED, 0x5B, 0x34, 0x12 });
        AssertInstruction("LD ($1234), HL", new byte[] { 0xED, 0x63, 0x34, 0x12 });
        AssertInstruction("LD HL, ($1234)", new byte[] { 0xED, 0x6B, 0x34, 0x12 });
        AssertInstruction("LD ($1234), SP", new byte[] { 0xED, 0x73, 0x34, 0x12 });
        AssertInstruction("LD SP, ($1234)", new byte[] { 0xED, 0x7B, 0x34, 0x12 });
        
        // Test 16-bit arithmetic
        AssertInstruction("ADC HL, BC", new byte[] { 0xED, 0x4A });
        AssertInstruction("SBC HL, BC", new byte[] { 0xED, 0x42 });
        AssertInstruction("ADC HL, DE", new byte[] { 0xED, 0x5A });
        AssertInstruction("SBC HL, DE", new byte[] { 0xED, 0x52 });
        AssertInstruction("ADC HL, HL", new byte[] { 0xED, 0x6A });
        AssertInstruction("SBC HL, HL", new byte[] { 0xED, 0x62 });
        AssertInstruction("ADC HL, SP", new byte[] { 0xED, 0x7A });
        AssertInstruction("SBC HL, SP", new byte[] { 0xED, 0x72 });
    }

    [TestMethod]
    public void TestZ80_DD_Instructions()
    {
        // Test DD prefix (IX register) instructions
        AssertInstruction("LD IX, #1234", new byte[] { 0xDD, 0x21, 0x34, 0x12 });
        AssertInstruction("ADD IX, BC", new byte[] { 0xDD, 0x09 });
        AssertInstruction("LD ($1234), IX", new byte[] { 0xDD, 0x22, 0x34, 0x12 });
        AssertInstruction("INC IX", new byte[] { 0xDD, 0x23 });
        
        // Note: IX+displacement operations like LD B,(IX+d) require more complex
        // register substitution and are not fully implemented
        
        // Test IX CB instructions (bit operations)
        AssertInstruction("BIT 3, (IX+$05)", new byte[] { 0xDD, 0xCB, 0x05, 0x5E });
        AssertInstruction("SET 7, (IX-$02)", new byte[] { 0xDD, 0xCB, 0xFE, 0xFE });
        AssertInstruction("RES 0, (IX+$00)", new byte[] { 0xDD, 0xCB, 0x00, 0x86 });
    }

    [TestMethod]
    public void TestZ80_FD_Instructions()
    {
        // Test FD prefix (IY register) instructions
        AssertInstruction("LD IY, #1234", new byte[] { 0xFD, 0x21, 0x34, 0x12 });
        AssertInstruction("ADD IY, BC", new byte[] { 0xFD, 0x09 });
        AssertInstruction("LD ($1234), IY", new byte[] { 0xFD, 0x22, 0x34, 0x12 });
        AssertInstruction("INC IY", new byte[] { 0xFD, 0x23 });
        
        // Note: IY+displacement operations like LD B,(IY+d) require more complex
        // register substitution and are not fully implemented
        
        // Test IY CB instructions (bit operations)
        AssertInstruction("BIT 1, (IY+$03)", new byte[] { 0xFD, 0xCB, 0x03, 0x4E });
        AssertInstruction("SET 5, (IY+$07)", new byte[] { 0xFD, 0xCB, 0x07, 0xEE });
        AssertInstruction("RES 2, (IY-$04)", new byte[] { 0xFD, 0xCB, 0xFC, 0x96 });
    }

    [TestMethod]
    public void TestZ80_Phase1_CoreMemoryOperations()
    {
        // Test Phase 1: Core memory load/store operations
        
        // LD (BC),A and LD A,(BC)
        AssertInstruction("LD (BC), A", new byte[] { 0x02 });
        AssertInstruction("LD A, (BC)", new byte[] { 0x0A });
        
        // LD (DE),A and LD A,(DE) 
        AssertInstruction("LD (DE), A", new byte[] { 0x12 });
        AssertInstruction("LD A, (DE)", new byte[] { 0x1A });
        
        // LD HL,(nn) and LD (nn),HL
        AssertInstruction("LD ($1234), HL", new byte[] { 0x22, 0x34, 0x12 });
        AssertInstruction("LD HL, ($1234)", new byte[] { 0x2A, 0x34, 0x12 });
        
        // LD A,(nn) and LD (nn),A
        AssertInstruction("LD ($1234), A", new byte[] { 0x32, 0x34, 0x12 });
        AssertInstruction("LD A, ($1234)", new byte[] { 0x3A, 0x34, 0x12 });
        
        // EX (SP),HL
        AssertInstruction("EX (SP), HL", new byte[] { 0xE3 });
    }

    [TestMethod]
    public void TestZ80_Phase2_BlockIOOperations()
    {
        // Test Phase 2: Complete ED prefix block I/O operations
        
        // Single block I/O operations
        AssertInstruction("INI", new byte[] { 0xED, 0xA2 });
        AssertInstruction("IND", new byte[] { 0xED, 0xAA });
        AssertInstruction("OUTI", new byte[] { 0xED, 0xA3 });
        AssertInstruction("OUTD", new byte[] { 0xED, 0xAB });
        
        // Repeating block I/O operations (terminators)
        AssertInstruction("INIR", new byte[] { 0xED, 0xB2 });
        AssertInstruction("INDR", new byte[] { 0xED, 0xBA });
        AssertInstruction("OTIR", new byte[] { 0xED, 0xB3 });
        AssertInstruction("OTDR", new byte[] { 0xED, 0xBB });
    }

    [TestMethod]
    public void TestZ80_Phase5_StackOperations()
    {
        // Test Phase 5: Stack operations for IX/IY registers
        
        // EX (SP),IX
        AssertInstruction("EX (SP), IX", new byte[] { 0xDD, 0xE3 });
        
        // EX (SP),IY
        AssertInstruction("EX (SP), IY", new byte[] { 0xFD, 0xE3 });
    }

    [TestMethod]
    public void TestZ80_Phase3_DisplacementEngine()
    {
        // Test Phase 3: IX/IY Displacement Engine
        
        // IX+displacement LD operations
        AssertInstruction("LD B, (IX+$05)", new byte[] { 0xDD, 0x46, 0x05 });
        AssertInstruction("LD C, (IX-$03)", new byte[] { 0xDD, 0x4E, 0xFD });
        AssertInstruction("LD D, (IX+$00)", new byte[] { 0xDD, 0x56, 0x00 });
        AssertInstruction("LD E, (IX+$10)", new byte[] { 0xDD, 0x5E, 0x10 });
        AssertInstruction("LD H, (IX-$08)", new byte[] { 0xDD, 0x66, 0xF8 });
        AssertInstruction("LD L, (IX+$7F)", new byte[] { 0xDD, 0x6E, 0x7F });
        AssertInstruction("LD A, (IX-$80)", new byte[] { 0xDD, 0x7E, 0x80 });
        
        // IX+displacement store operations
        AssertInstruction("LD (IX+$02), B", new byte[] { 0xDD, 0x70, 0x02 });
        AssertInstruction("LD (IX-$04), C", new byte[] { 0xDD, 0x71, 0xFC });
        AssertInstruction("LD (IX+$15), D", new byte[] { 0xDD, 0x72, 0x15 });
        AssertInstruction("LD (IX+$0A), A", new byte[] { 0xDD, 0x77, 0x0A });
        
        // IX+displacement ALU operations
        AssertInstruction("ADD A, (IX+$03)", new byte[] { 0xDD, 0x86, 0x03 });
        AssertInstruction("ADC A, (IX-$02)", new byte[] { 0xDD, 0x8E, 0xFE });
        AssertInstruction("SUB A, (IX+$08)", new byte[] { 0xDD, 0x96, 0x08 });
        AssertInstruction("SBC A, (IX-$01)", new byte[] { 0xDD, 0x9E, 0xFF });
        AssertInstruction("AND A, (IX+$0C)", new byte[] { 0xDD, 0xA6, 0x0C });
        AssertInstruction("XOR A, (IX+$07)", new byte[] { 0xDD, 0xAE, 0x07 });
        AssertInstruction("OR A, (IX-$06)", new byte[] { 0xDD, 0xB6, 0xFA });
        AssertInstruction("CP A, (IX+$14)", new byte[] { 0xDD, 0xBE, 0x14 });
        
        // IY+displacement LD operations
        AssertInstruction("LD B, (IY+$08)", new byte[] { 0xFD, 0x46, 0x08 });
        AssertInstruction("LD C, (IY-$05)", new byte[] { 0xFD, 0x4E, 0xFB });
        AssertInstruction("LD A, (IY+$12)", new byte[] { 0xFD, 0x7E, 0x12 });
        
        // IY+displacement store operations  
        AssertInstruction("LD (IY-$03), B", new byte[] { 0xFD, 0x70, 0xFD });
        AssertInstruction("LD (IY+$06), A", new byte[] { 0xFD, 0x77, 0x06 });
        
        // IY+displacement ALU operations
        AssertInstruction("ADD A, (IY+$0F)", new byte[] { 0xFD, 0x86, 0x0F });
        AssertInstruction("SUB A, (IY-$04)", new byte[] { 0xFD, 0x96, 0xFC });
        AssertInstruction("CP A, (IY+$20)", new byte[] { 0xFD, 0xBE, 0x20 });
    }

    [TestMethod]
    public void TestZ80_Phase4_HighLowRegisterOps()
    {
        // Test Phase 4: IX/IY High/Low Register Operations
        
        // IXH/IXL basic operations
        AssertInstruction("INC IXH", new byte[] { 0xDD, 0x24 });
        AssertInstruction("DEC IXH", new byte[] { 0xDD, 0x25 });
        AssertInstruction("LD IXH, #42", new byte[] { 0xDD, 0x26, 0x42 });
        AssertInstruction("INC IXL", new byte[] { 0xDD, 0x2C });
        AssertInstruction("DEC IXL", new byte[] { 0xDD, 0x2D });
        AssertInstruction("LD IXL, #55", new byte[] { 0xDD, 0x2E, 0x55 });
        
        // IXH/IXL register transfers
        AssertInstruction("LD B, IXH", new byte[] { 0xDD, 0x44 });
        AssertInstruction("LD B, IXL", new byte[] { 0xDD, 0x45 });
        AssertInstruction("LD C, IXH", new byte[] { 0xDD, 0x4C });
        AssertInstruction("LD C, IXL", new byte[] { 0xDD, 0x4D });
        AssertInstruction("LD IXH, B", new byte[] { 0xDD, 0x60 });
        AssertInstruction("LD IXH, C", new byte[] { 0xDD, 0x61 });
        AssertInstruction("LD IXH, IXH", new byte[] { 0xDD, 0x64 });
        AssertInstruction("LD IXH, IXL", new byte[] { 0xDD, 0x65 });
        AssertInstruction("LD IXL, IXH", new byte[] { 0xDD, 0x6C });
        AssertInstruction("LD IXL, IXL", new byte[] { 0xDD, 0x6D });
        AssertInstruction("LD A, IXH", new byte[] { 0xDD, 0x7C });
        AssertInstruction("LD A, IXL", new byte[] { 0xDD, 0x7D });
        
        // IXH/IXL ALU operations
        AssertInstruction("ADD A, IXH", new byte[] { 0xDD, 0x84 });
        AssertInstruction("ADD A, IXL", new byte[] { 0xDD, 0x85 });
        AssertInstruction("SUB A, IXH", new byte[] { 0xDD, 0x94 });
        AssertInstruction("SUB A, IXL", new byte[] { 0xDD, 0x95 });
        AssertInstruction("AND A, IXH", new byte[] { 0xDD, 0xA4 });
        AssertInstruction("XOR A, IXL", new byte[] { 0xDD, 0xAD });
        AssertInstruction("CP A, IXH", new byte[] { 0xDD, 0xBC });
        
        // IYH/IYL basic operations
        AssertInstruction("INC IYH", new byte[] { 0xFD, 0x24 });
        AssertInstruction("DEC IYH", new byte[] { 0xFD, 0x25 });
        AssertInstruction("LD IYH, #33", new byte[] { 0xFD, 0x26, 0x33 });
        AssertInstruction("INC IYL", new byte[] { 0xFD, 0x2C });
        AssertInstruction("DEC IYL", new byte[] { 0xFD, 0x2D });
        AssertInstruction("LD IYL, #AA", new byte[] { 0xFD, 0x2E, 0xAA });
        
        // IYH/IYL register transfers
        AssertInstruction("LD B, IYH", new byte[] { 0xFD, 0x44 });
        AssertInstruction("LD B, IYL", new byte[] { 0xFD, 0x45 });
        AssertInstruction("LD IYH, A", new byte[] { 0xFD, 0x67 });
        AssertInstruction("LD IYL, A", new byte[] { 0xFD, 0x6F });
        AssertInstruction("LD A, IYH", new byte[] { 0xFD, 0x7C });
        AssertInstruction("LD A, IYL", new byte[] { 0xFD, 0x7D });
        
        // IYH/IYL ALU operations
        AssertInstruction("ADD A, IYH", new byte[] { 0xFD, 0x84 });
        AssertInstruction("ADC A, IYL", new byte[] { 0xFD, 0x8D });
        AssertInstruction("OR A, IYH", new byte[] { 0xFD, 0xB4 });
        AssertInstruction("CP A, IYL", new byte[] { 0xFD, 0xBD });
    }
}
