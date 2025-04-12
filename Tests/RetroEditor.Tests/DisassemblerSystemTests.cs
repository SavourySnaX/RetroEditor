using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace RetroEditor.Tests
{
    public class DisassemblerSystemTests
    {
        [Fact]
        public void TestOperandCreation()
        {
            var operand = new Operand("$1234", true, false);
            Assert.Equal("$1234", operand.Text);
            Assert.True(operand.IsSource);
            Assert.False(operand.IsDestination);
            Assert.Null(operand.Value);
            operand = new Operand("#42", false, true, 42);
            Assert.Equal("#42", operand.Text);
            Assert.False(operand.IsSource);
            Assert.True(operand.IsDestination);
            Assert.Equal(42ul, operand.Value);
        }

        [Fact]
        public void TestInstructionCreation()
        {
            var operands = new List<Operand> { new Operand("$1234") };
            var bytes = new byte[] { 0xA9, 0x34, 0x12 };
            var instruction = new Instruction(0x8000, "LDA", operands, bytes, new EmptyState());

            Assert.Equal(0x8000ul, instruction.Address);
            Assert.Equal("LDA", instruction.Mnemonic);
            Assert.Single(instruction.Operands);
            Assert.Equal(3, instruction.Bytes.Length);
            Assert.False(instruction.IsBranch);
            Assert.False(instruction.IsBasicBlockTerminator);
            Assert.Empty(instruction.NextAddresses);
        }

        [Fact]
        public void TestDecodeResultCreation()
        {
            var result = DecodeResult.NeedMoreBytes(2);
            Assert.False(result.Success);
            Assert.True(result.NeedsMoreBytes);
            Assert.Equal(2, result.AdditionalBytesNeeded);

            var instruction = new Instruction(0x8000, "NOP", new List<Operand>(), new byte[] { 0xEA }, new EmptyState());
            result = DecodeResult.CreateSuccess(instruction, 1);
            Assert.True(result.Success);
            Assert.False(result.NeedsMoreBytes);
            Assert.Equal(1, result.BytesConsumed);
            Assert.Equal(instruction, result.Instruction);

            result = DecodeResult.CreateError("Invalid opcode");
            Assert.False(result.Success);
            Assert.False(result.NeedsMoreBytes);
            Assert.Equal("Invalid opcode", result.ErrorMessage);
        }

        [Fact]
        public void Test65816StateManagement()
        {
            var disassembler = new SNES65816Disassembler();
            var state = (SNES65816State)disassembler.State;

            // Test initial state
            Assert.True(state.EmulationMode);
            Assert.True(state.Accumulator8Bit);
            Assert.True(state.Index8Bit);

            // Disable emulation
            state.SetEmulationMode(false);
            disassembler.State = state;

            // Test REP instruction
            var bytes = new byte[] { 0xC2, 0x30 }; // REP #$30 (16-bit A, 16-bit X/Y)
            var result = disassembler.DecodeNext(bytes, 0x8000);
            Assert.True(result.Success);
            state = (SNES65816State)disassembler.State;
            Assert.False(state.Accumulator8Bit);
            Assert.False(state.Index8Bit);

            // Test SEP instruction
            bytes = new byte[] { 0xE2, 0x30 }; // SEP #$30 (8-bit A, 8-bit X/Y)
            result = disassembler.DecodeNext(bytes, 0x8002);
            Assert.True(result.Success);
            state = (SNES65816State)disassembler.State;
            Assert.True(state.Accumulator8Bit);
            Assert.True(state.Index8Bit);

            // Test XCE instruction
            bytes = new byte[] { 0xFB }; // XCE (Toggle emulation mode - but in disassembly does not update state)
            result = disassembler.DecodeNext(bytes, 0x8004);
            Assert.True(result.Success);
        }

    }
}