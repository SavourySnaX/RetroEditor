using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroEditor.Plugins;

namespace RetroEditor.Tests
{
    public class DisassemblerTestBase
    {
        private readonly SNES65816Disassembler _disassembler;

        protected DisassemblerTestBase()
        {
            _disassembler = new SNES65816Disassembler();
        }

        internal DecodeResult DecodeNext(byte[] bytes, ulong address = 0x8000)
        {
            return _disassembler.DecodeNext(bytes, address);
        }

        internal void SetState(bool emulation, bool a16bit, bool x16bit)
        {
            var state = (SNES65816State)_disassembler.State;
            state.Accumulator8Bit = !a16bit;
            state.Index8Bit = !x16bit;
            state.SetEmulationMode(emulation);
        }

        internal void TestInAllStates(Action<DecodeResult> testAction, byte[] bytes, ulong address = 0x8000)
        {
            // Test in emulation mode
            SetState(emulation: true, a16bit: false, x16bit: false);
            testAction(DecodeNext(bytes, address));

            // Test in native mode with 8-bit A and X
            SetState(emulation: false, a16bit: false, x16bit: false);
            testAction(DecodeNext(bytes, address));

            // Test in native mode with 16-bit A and 8-bit X
            SetState(emulation: false, a16bit: true, x16bit: false);
            testAction(DecodeNext(bytes, address));

            // Test in native mode with 8-bit A and 16-bit X
            SetState(emulation: false, a16bit: false, x16bit: true);
            testAction(DecodeNext(bytes, address));

            // Test in native mode with 16-bit A and X
            SetState(emulation: false, a16bit: true, x16bit: true);
            testAction(DecodeNext(bytes, address));
        }

        internal void AssertInstruction(
            DecodeResult result,
            string mnemonic,
            int bytesConsumed,
            int operandCount = 0,
            string[] operandText = null,
            bool isBranch = false,
            bool isTerminator = false,
            HashSet<ulong> nextAddresses = null)
        {
            Assert.IsTrue(result.Success);

            var instruction = result.Instruction;
            Assert.AreEqual(mnemonic, instruction.Mnemonic);
            Assert.AreEqual(bytesConsumed, result.BytesConsumed);
            
            var operands = instruction.Operands;
            Assert.AreEqual(operandCount, operands.Count);
            
            if (operandText != null)
            {
                int o = 0;
                foreach (var operand in operands)
                {
                    Assert.IsNotNull(operand);
                    Assert.AreEqual(operandText[o], operand.Text);
                    o++;
                }
            }

            Assert.AreEqual(isBranch, instruction.IsBranch);
            Assert.AreEqual(isTerminator, instruction.IsBasicBlockTerminator);

            if (nextAddresses != null)
            {
                var addresses = instruction.NextAddresses;
                Assert.AreEqual(nextAddresses.Count, addresses.Count);
                foreach (var address in nextAddresses)
                {
                    Assert.IsTrue(addresses.Contains(address));
                }
            }
        }
    }

    [TestClass]
    public class DisassemblerTests : DisassemblerTestBase
    {
        [TestMethod]
        public void Test65816_SEI()
        {
            TestInAllStates(result => 
                AssertInstruction(result, "SEI", 1),
                new byte[] { 0x78 }
            );
        }

        [TestMethod]
        public void Test65816_CLC()
        {
            TestInAllStates(result => 
                AssertInstruction(result, "CLC", 1),
                new byte[] { 0x18 }
            );
        }

        [TestMethod]
        public void Test65816_CLI()
        {
            TestInAllStates(result => 
                AssertInstruction(result, "CLI", 1),
                new byte[] { 0x58 }
            );
        }

        [TestMethod]
        public void Test65816_CLD()
        {
            TestInAllStates(result => 
                AssertInstruction(result, "CLD", 1),
                new byte[] { 0xD8 }
            );
        }

        [TestMethod]
        public void Test65816_CLV()
        {
            TestInAllStates(result => 
                AssertInstruction(result, "CLV", 1),
                new byte[] { 0xB8 }
            );
        }

        [TestMethod]
        public void Test65816_ADC_Immediate()
        {
            // Test in emulation mode (8-bit A)
            SetState(emulation: true, a16bit: false, x16bit: false);
            var result = DecodeNext(new byte[] { 0x69, 0x42 });
            AssertInstruction(
                result,
                mnemonic: "ADC",
                bytesConsumed: 2,
                operandCount: 1,
                operandText: new[] { "#$42" }
            );

            // Test in native mode with 8-bit A
            SetState(emulation: false, a16bit: false, x16bit: false);
            result = DecodeNext(new byte[] { 0x69, 0x42 });
            AssertInstruction(
                result,
                mnemonic: "ADC",
                bytesConsumed: 2,
                operandCount: 1,
                operandText: new[] { "#$42" }
            );

            // Test in native mode with 16-bit A
            SetState(emulation: false, a16bit: true, x16bit: false);
            result = DecodeNext(new byte[] { 0x69, 0x42, 0x12 });
            AssertInstruction(
                result,
                mnemonic: "ADC",
                bytesConsumed: 3,
                operandCount: 1,
                operandText: new[] { "#$1242" }
            );

            // Test in native mode with 8-bit A and 16-bit X
            SetState(emulation: false, a16bit: false, x16bit: true);
            result = DecodeNext(new byte[] { 0x69, 0x42 });
            AssertInstruction(
                result,
                mnemonic: "ADC",
                bytesConsumed: 2,
                operandCount: 1,
                operandText: new[] { "#$42" }
            );

            // Test in native mode with 16-bit A and X
            SetState(emulation: false, a16bit: true, x16bit: true);
            result = DecodeNext(new byte[] { 0x69, 0x42, 0x12 });
            AssertInstruction(
                result,
                mnemonic: "ADC",
                bytesConsumed: 3,
                operandCount: 1,
                operandText: new[] { "#$1242" }
            );
        }
        
        [TestMethod]
        public void Test65816_ADC_Absolute()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ADC",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234" }
                ),
                new byte[] { 0x6D, 0x34, 0x12 }
            );
        }
        
        [TestMethod]
        public void Test65816_ADC_AbsoluteLong()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ADC",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: new[] { "$123456" }
                ),
                new byte[] { 0x6F, 0x56, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_ADC_DirectPage()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ADC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42" }
                ),
                new byte[] { 0x65, 0x42 }
            );
        }
        
        [TestMethod]
        public void Test65816_ADC_DirectPageIndirect()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ADC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "($42)" }
                ),
                new byte[] { 0x72, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_ADC_DirectPageIndirectLong()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ADC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "[$42]" }
                ),
                new byte[] { 0x72, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_ADC_AbsoluteIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ADC",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234,X" }
                ),
                new byte[] { 0x7D, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_ADC_AbsoluteLongIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ADC",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: new[] { "$123456,X" }
                ),
                new byte[] { 0x7F, 0x56, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_ADC_AbsoluteIndexedy()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ADC",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234,Y" }
                ),
                new byte[] { 0x79, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_ADC_DirectPageIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ADC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42,X" }
                ),
                new byte[] { 0x75, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_ADC_DirectPageIndirectX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ADC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "($1234,X)" }
                ),
                new byte[] { 0x61, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_ADC_DirectPageIndirectIndexedY()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ADC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "($42),Y" }
                ),
                new byte[] { 0x71, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_ADC_DirectPageIndirectLongIndexedY()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ADC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "[$42],Y" }
                ),
                new byte[] { 0x77, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_ADC_StackRelative()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ADC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42,S" }
                ),
                new byte[] { 0x63, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_ADC_StackRelativeIndexedY()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ADC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "($42,S),Y" }
                ),
                new byte[] { 0x73, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_JML()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "JMP",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: new[] { "$123456" },
                    isBranch: true,
                    isTerminator: true,
                    nextAddresses: new HashSet<ulong> { 0x8004, 0x123456 }
                ),
                new byte[] { 0x5C, 0x56, 0x34, 0x12 }
            );
        }
    }
} 