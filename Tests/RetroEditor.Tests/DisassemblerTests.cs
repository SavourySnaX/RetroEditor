using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroEditor.Plugins;

namespace RetroEditor.Tests
{
    [TestClass]
    public class DisassemblerTests
    {
        private class TestMemoryAccess : IMemoryAccess
        {
            private readonly byte[] _memory;

            public TestMemoryAccess(byte[] memory)
            {
                _memory = memory;
            }

            public ReadOnlySpan<byte> ReadBytes(ReadKind kind, uint address, uint length)
            {
                return new ReadOnlySpan<byte>(_memory, (int)address, (int)length);
            }

            public void WriteBytes(WriteKind kind, uint address, ReadOnlySpan<byte> bytes)
            {
                bytes.CopyTo(new Span<byte>(_memory, (int)address, bytes.Length));
            }

            public int RomSize => _memory.Length;
            public MemoryEndian Endian => MemoryEndian.Little;
        }

        [TestMethod]
        public void TestOperandCreation()
        {
            var operand = new Operand("$1234", true, false);
            Assert.AreEqual("$1234", operand.Text);
            Assert.IsTrue(operand.IsSource);
            Assert.IsFalse(operand.IsDestination);
            Assert.IsNull(operand.Value);

            operand = new Operand("#42", false, true, 42);
            Assert.AreEqual("#42", operand.Text);
            Assert.IsFalse(operand.IsSource);
            Assert.IsTrue(operand.IsDestination);
            Assert.AreEqual(42ul, operand.Value);
        }

        [TestMethod]
        public void TestInstructionCreation()
        {
            var operands = new List<Operand> { new Operand("$1234") };
            var bytes = new byte[] { 0xA9, 0x34, 0x12 };
            var instruction = new Instruction(0x8000, "LDA", operands, bytes);

            Assert.AreEqual(0x8000ul, instruction.Address);
            Assert.AreEqual("LDA", instruction.Mnemonic);
            Assert.AreEqual(1, instruction.Operands.Count);
            Assert.AreEqual(3, instruction.Bytes.Length);
            Assert.IsFalse(instruction.IsBranch);
            Assert.IsFalse(instruction.IsBasicBlockTerminator);
            Assert.AreEqual(0, instruction.NextAddresses.Count);
        }

        [TestMethod]
        public void TestDecodeResultCreation()
        {
            var result = DecodeResult.NeedMoreBytes(2);
            Assert.IsFalse(result.Success);
            Assert.IsTrue(result.NeedsMoreBytes);
            Assert.AreEqual(2, result.AdditionalBytesNeeded);
            Assert.IsNull(result.Instruction);

            var instruction = new Instruction(0x8000, "NOP", new List<Operand>(), new byte[] { 0xEA });
            result = DecodeResult.CreateSuccess(instruction, 1);
            Assert.IsTrue(result.Success);
            Assert.IsFalse(result.NeedsMoreBytes);
            Assert.AreEqual(1, result.BytesConsumed);
            Assert.AreEqual(instruction, result.Instruction);

            result = DecodeResult.CreateError("Invalid opcode");
            Assert.IsFalse(result.Success);
            Assert.IsFalse(result.NeedsMoreBytes);
            Assert.AreEqual("Invalid opcode", result.ErrorMessage);
            Assert.IsNull(result.Instruction);
        }

        [TestMethod]
        public void Test65816StateManagement()
        {
            var memoryAccess = new TestMemoryAccess(new byte[1024]);
            var disassembler = new SNES65816Disassembler(memoryAccess);
            var state = (SNES65816State)disassembler.State;

            // Test initial state
            Assert.IsTrue(state.EmulationMode);
            Assert.IsTrue(state.Accumulator8Bit);
            Assert.IsTrue(state.Index8Bit);

            // Disable emulation
            state.SetEmulationMode(false);

            // Test REP instruction
            var bytes = new byte[] { 0xC2, 0x30 }; // REP #$30 (16-bit A, 16-bit X/Y)
            var result = disassembler.DecodeNext(bytes, 0x8000);
            Assert.IsTrue(result.Success);
            state = (SNES65816State)disassembler.State;
            Assert.IsFalse(state.Accumulator8Bit);
            Assert.IsFalse(state.Index8Bit);

            // Test SEP instruction
            bytes = new byte[] { 0xE2, 0x30 }; // SEP #$30 (8-bit A, 8-bit X/Y)
            result = disassembler.DecodeNext(bytes, 0x8002);
            Assert.IsTrue(result.Success);
            state = (SNES65816State)disassembler.State;
            Assert.IsTrue(state.Accumulator8Bit);
            Assert.IsTrue(state.Index8Bit);

            // Test XCE instruction
            bytes = new byte[] { 0xFB }; // XCE (Toggle emulation mode - but in disassembly does not update state)
            result = disassembler.DecodeNext(bytes, 0x8004);
            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public void Test65816BasicInstructions()
        {
            var memoryAccess = new TestMemoryAccess(new byte[1024]);
            var disassembler = new SNES65816Disassembler(memoryAccess);

            // Test NOP
            var bytes = new byte[] { 0xEA };
            var result = disassembler.DecodeNext(bytes, 0x8000);
            Assert.IsTrue(result.Success);
            Assert.AreEqual("NOP", result.Instruction.Mnemonic);
            Assert.AreEqual(1, result.BytesConsumed);

            // Test LDA immediate
            bytes = new byte[] { 0xA9, 0x42 };
            result = disassembler.DecodeNext(bytes, 0x8001);
            Assert.IsTrue(result.Success);
            Assert.AreEqual("LDA", result.Instruction.Mnemonic);
            Assert.AreEqual(2, result.BytesConsumed);
            Assert.AreEqual(1, result.Instruction.Operands.Count);
            Assert.AreEqual("#$42", result.Instruction.Operands[0].Text);

            // Test JSR
            bytes = new byte[] { 0x20, 0x34, 0x12 };
            result = disassembler.DecodeNext(bytes, 0x8003);
            Assert.IsTrue(result.Success);
            Assert.AreEqual("JSR", result.Instruction.Mnemonic);
            Assert.AreEqual(3, result.BytesConsumed);
            Assert.AreEqual(1, result.Instruction.Operands.Count);
            Assert.AreEqual("$1234", result.Instruction.Operands[0].Text);
            Assert.IsTrue(result.Instruction.IsBranch);
            Assert.IsTrue(result.Instruction.IsBasicBlockTerminator);
            Assert.AreEqual(2, result.Instruction.NextAddresses.Count);
        }

        [TestMethod]
        public void Test65816BranchInstructions()
        {
            var memoryAccess = new TestMemoryAccess(new byte[1024]);
            var disassembler = new SNES65816Disassembler(memoryAccess);

            // Test BRA
            var bytes = new byte[] { 0x80, 0xFE }; // BRA -2
            var result = disassembler.DecodeNext(bytes, 0x8000);
            Assert.IsTrue(result.Success);
            Assert.AreEqual("BRA", result.Instruction.Mnemonic);
            Assert.AreEqual(2, result.BytesConsumed);
            Assert.AreEqual(1, result.Instruction.Operands.Count);
            Assert.AreEqual("$8000", result.Instruction.Operands[0].Text);
            Assert.IsTrue(result.Instruction.IsBranch);
            Assert.IsTrue(result.Instruction.IsBasicBlockTerminator);

            // Test BNE
            bytes = new byte[] { 0xD0, 0xFE }; // BNE -2
            result = disassembler.DecodeNext(bytes, 0x8002);
            Assert.IsTrue(result.Success);
            Assert.AreEqual("BNE", result.Instruction.Mnemonic);
            Assert.AreEqual(2, result.BytesConsumed);
            Assert.AreEqual(1, result.Instruction.Operands.Count);
            Assert.AreEqual("$8002", result.Instruction.Operands[0].Text);
            Assert.IsTrue(result.Instruction.IsBranch);
            Assert.IsTrue(result.Instruction.IsBasicBlockTerminator);
        }

        [TestMethod]
        public void Test65816NeedMoreBytes()
        {
            var memoryAccess = new TestMemoryAccess(new byte[1024]);
            var disassembler = new SNES65816Disassembler(memoryAccess);

            // Test partial JSR
            var bytes = new byte[] { 0x20, 0x34 };
            var result = disassembler.DecodeNext(bytes, 0x8000);
            Assert.IsFalse(result.Success);
            Assert.IsTrue(result.NeedsMoreBytes);
            Assert.AreEqual(1, result.AdditionalBytesNeeded);

            // Test partial JSL
            bytes = new byte[] { 0x22, 0x34, 0x12 };
            result = disassembler.DecodeNext(bytes, 0x8002);
            Assert.IsFalse(result.Success);
            Assert.IsTrue(result.NeedsMoreBytes);
            Assert.AreEqual(1, result.AdditionalBytesNeeded);
        }
    }
} 