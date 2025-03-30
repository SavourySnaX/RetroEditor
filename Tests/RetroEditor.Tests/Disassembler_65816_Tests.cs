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
            List<ulong> nextAddresses = null)
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
        public void Test65816_ADC_Immediate()
        {
            var testCases = new[]
            {
                // emulation mode (8-bit A)
                new { Emulation = true, A16Bit = false, X16Bit = false, Bytes = new byte[] { 0x69, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 8-bit A
                new { Emulation = false, A16Bit = false, X16Bit = false, Bytes = new byte[] { 0x69, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 16-bit A
                new { Emulation = false, A16Bit = true, X16Bit = false, Bytes = new byte[] { 0x69, 0x42, 0x12 }, ExpectedLength = 3, ExpectedOperand = "#$1242" },
                
                // native mode, 8-bit A, 16-bit X
                new { Emulation = false, A16Bit = false, X16Bit = true, Bytes = new byte[] { 0x69, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 16-bit A and X
                new { Emulation = false, A16Bit = true, X16Bit = true, Bytes = new byte[] { 0x69, 0x42, 0x12 }, ExpectedLength = 3, ExpectedOperand = "#$1242" }
            };

            foreach (var testCase in testCases)
            {
                SetState(testCase.Emulation, testCase.A16Bit, testCase.X16Bit);
                var result = DecodeNext(testCase.Bytes);
                AssertInstruction(
                    result,
                    mnemonic: "ADC",
                    bytesConsumed: testCase.ExpectedLength,
                    operandCount: 1,
                    operandText: new[] { testCase.ExpectedOperand }
                );
            }
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
                new byte[] { 0x67, 0x42 }
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
        public void Test65816_ADC_AbsoluteIndexedY()
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
                    operandText: new[] { "($42,X)" }
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
        public void Test65816_AND_Immediate()
        {
            var testCases = new[]
            {
                // emulation mode (8-bit A)
                new { Emulation = true, A16Bit = false, X16Bit = false, Bytes = new byte[] { 0x29, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 8-bit A
                new { Emulation = false, A16Bit = false, X16Bit = false, Bytes = new byte[] { 0x29, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 16-bit A
                new { Emulation = false, A16Bit = true, X16Bit = false, Bytes = new byte[] { 0x29, 0x42, 0x12 }, ExpectedLength = 3, ExpectedOperand = "#$1242" },
                
                // native mode, 8-bit A, 16-bit X
                new { Emulation = false, A16Bit = false, X16Bit = true, Bytes = new byte[] { 0x29, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 16-bit A and X
                new { Emulation = false, A16Bit = true, X16Bit = true, Bytes = new byte[] { 0x29, 0x42, 0x12 }, ExpectedLength = 3, ExpectedOperand = "#$1242" }
            };

            foreach (var testCase in testCases)
            {
                SetState(testCase.Emulation, testCase.A16Bit, testCase.X16Bit);
                var result = DecodeNext(testCase.Bytes);
                AssertInstruction(
                    result,
                    mnemonic: "AND",
                    bytesConsumed: testCase.ExpectedLength,
                    operandCount: 1,
                    operandText: new[] { testCase.ExpectedOperand }
                );
            }
        }

        [TestMethod]
        public void Test65816_AND_Absolute()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "AND",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234" }
                ),
                new byte[] { 0x2D, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_AND_AbsoluteLong()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "AND",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: new[] { "$123456" }
                ),
                new byte[] { 0x2F, 0x56, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_AND_DirectPage()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "AND",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42" }
                ),
                new byte[] { 0x25, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_AND_DirectPageIndirect()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "AND",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "($42)" }
                ),
                new byte[] { 0x32, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_AND_DirectPageIndirectLong()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "AND",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "[$42]" }
                ),
                new byte[] { 0x27, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_AND_AbsoluteIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "AND",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234,X" }
                ),
                new byte[] { 0x3D, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_AND_AbsoluteLongIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "AND",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: new[] { "$123456,X" }
                ),
                new byte[] { 0x3F, 0x56, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_AND_AbsoluteIndexedY()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "AND",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234,Y" }
                ),
                new byte[] { 0x39, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_AND_DirectPageIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "AND",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42,X" }
                ),
                new byte[] { 0x35, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_AND_DirectPageIndirectX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "AND",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "($42,X)" }
                ),
                new byte[] { 0x21, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_AND_DirectPageIndirectIndexedY()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "AND",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "($42),Y" }
                ),
                new byte[] { 0x31, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_AND_DirectPageIndirectLongIndexedY()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "AND",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "[$42],Y" }
                ),
                new byte[] { 0x37, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_AND_StackRelative()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "AND",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42,S" }
                ),
                new byte[] { 0x23, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_AND_StackRelativeIndexedY()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "AND",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "($42,S),Y" }
                ),
                new byte[] { 0x33, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_ASL_Accumulator()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ASL",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                new byte[] { 0x0A }
            );
        }

        [TestMethod]
        public void Test65816_ASL_Absolute()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ASL",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234" }
                ),
                new byte[] { 0x0E, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_ASL_DirectPage()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ASL",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42" }
                ),
                new byte[] { 0x06, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_ASL_AbsoluteIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ASL",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234,X" }
                ),
                new byte[] { 0x1E, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_ASL_DirectPageIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ASL",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42,X" }
                ),
                new byte[] { 0x16, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_BCC_Forward()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "BCC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$8002" },
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x8002 }
                ),
                new byte[] { 0x90, 0x00 }
            );
        }

        [TestMethod]
        public void Test65816_BCC_Backward()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "BCC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$7FFE" },
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x7FFE }
                ),
                new byte[] { 0x90, 0xFC }
            );
        }

        [TestMethod]
        public void Test65816_BCS_Forward()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "BCS",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$8002" },
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x8002 }
                ),
                new byte[] { 0xB0, 0x00 }
            );
        }

        [TestMethod]
        public void Test65816_BCS_Backward()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "BCS",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$7FFE" },
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x7FFE }
                ),
                new byte[] { 0xB0, 0xFC }
            );
        }

        [TestMethod]
        public void Test65816_BEQ_Forward()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "BEQ",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$8002" },
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x8002 }
                ),
                new byte[] { 0xF0, 0x00 }
            );
        }

        [TestMethod]
        public void Test65816_BEQ_Backward()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "BEQ",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$7FFE" },
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x7FFE }
                ),
                new byte[] { 0xF0, 0xFC }
            );
        }

        [TestMethod]
        public void Test65816_BMI_Forward()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "BMI",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$8002" },
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x8002 }
                ),
                new byte[] { 0x30, 0x00 }
            );
        }

        [TestMethod]
        public void Test65816_BMI_Backward()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "BMI",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$7FFE" },
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x7FFE }
                ),
                new byte[] { 0x30, 0xFC }
            );
        }

        [TestMethod]
        public void Test65816_BNE_Forward()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "BNE",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$8002" },
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x8002 }
                ),
                new byte[] { 0xD0, 0x00 }
            );
        }

        [TestMethod]
        public void Test65816_BNE_Backward()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "BNE",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$7FFE" },
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x7FFE }
                ),
                new byte[] { 0xD0, 0xFC }
            );
        }

        [TestMethod]
        public void Test65816_BPL_Forward()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "BPL",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$8002" },
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x8002 }
                ),
                new byte[] { 0x10, 0x00 }
            );
        }

        [TestMethod]
        public void Test65816_BPL_Backward()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "BPL",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$7FFE" },
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x7FFE }
                ),
                new byte[] { 0x10, 0xFC }
            );
        }

        [TestMethod]
        public void Test65816_BRA_Forward()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "BRA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$8002" },
                    isBranch: true,
                    isTerminator: true,
                    nextAddresses: new List<ulong> { 0x8002 }
                ),
                new byte[] { 0x80, 0x00 }
            );
        }

        [TestMethod]
        public void Test65816_BRA_Backward()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "BRA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$7FFE" },
                    isBranch: true,
                    isTerminator: true,
                    nextAddresses: new List<ulong> { 0x7FFE }
                ),
                new byte[] { 0x80, 0xFC }
            );
        }

        [TestMethod]
        public void Test65816_BVC_Forward()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "BVC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$8002" },
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x8002 }
                ),
                new byte[] { 0x50, 0x00 }
            );
        }

        [TestMethod]
        public void Test65816_BVC_Backward()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "BVC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$7FFE" },
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x7FFE }
                ),
                new byte[] { 0x50, 0xFC }
            );
        }

        [TestMethod]
        public void Test65816_BVS_Forward()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "BVS",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$8002" },
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x8002 }
                ),
                new byte[] { 0x70, 0x00 }
            );
        }

        [TestMethod]
        public void Test65816_BVS_Backward()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "BVS",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$7FFE" },
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x7FFE }
                ),
                new byte[] { 0x70, 0xFC }
            );
        }

        [TestMethod]
        public void Test65816_BRL_Forward()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "BRL",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$8003" },
                    isBranch: true,
                    isTerminator: true,
                    nextAddresses: new List<ulong> { 0x8003 }
                ),
                new byte[] { 0x82, 0x00, 0x00 }
            );
        }

        [TestMethod]
        public void Test65816_BRL_Backward()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "BRL",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$7FFF" },
                    isBranch: true,
                    isTerminator: true,
                    nextAddresses: new List<ulong> { 0x7FFF }
                ),
                new byte[] { 0x82, 0xFC, 0xFF }
            );
        }

        [TestMethod]
        public void Test65816_BRL_LongDistance()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "BRL",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$9003" },
                    isBranch: true,
                    isTerminator: true,
                    nextAddresses: new List<ulong> { 0x9003 }
                ),
                new byte[] { 0x82, 0x00, 0x10 }
            );
        }

        [TestMethod]
        public void Test65816_BIT_Immediate()
        {
            var testCases = new[]
            {
                // emulation mode (8-bit A)
                new { Emulation = true, A16Bit = false, X16Bit = false, Bytes = new byte[] { 0x89, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 8-bit A
                new { Emulation = false, A16Bit = false, X16Bit = false, Bytes = new byte[] { 0x89, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 16-bit A
                new { Emulation = false, A16Bit = true, X16Bit = false, Bytes = new byte[] { 0x89, 0x42, 0x12 }, ExpectedLength = 3, ExpectedOperand = "#$1242" },
                
                // native mode, 8-bit A, 16-bit X
                new { Emulation = false, A16Bit = false, X16Bit = true, Bytes = new byte[] { 0x89, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 16-bit A and X
                new { Emulation = false, A16Bit = true, X16Bit = true, Bytes = new byte[] { 0x89, 0x42, 0x12 }, ExpectedLength = 3, ExpectedOperand = "#$1242" }
            };

            foreach (var testCase in testCases)
            {
                SetState(testCase.Emulation, testCase.A16Bit, testCase.X16Bit);
                var result = DecodeNext(testCase.Bytes);
                AssertInstruction(
                    result,
                    mnemonic: "BIT",
                    bytesConsumed: testCase.ExpectedLength,
                    operandCount: 1,
                    operandText: new[] { testCase.ExpectedOperand }
                );
            }
        }

        [TestMethod]
        public void Test65816_BIT_Absolute()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "BIT",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234" }
                ),
                new byte[] { 0x2C, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_BIT_DirectPage()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "BIT",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42" }
                ),
                new byte[] { 0x24, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_BIT_AbsoluteIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "BIT",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234,X" }
                ),
                new byte[] { 0x3C, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_BIT_DirectPageIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "BIT",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42,X" }
                ),
                new byte[] { 0x34, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_BRK()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "BRK",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "#$42" },
                    isTerminator: true,
                    nextAddresses: new List<ulong> { }
                ),
                new byte[] { 0x00, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_COP()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "COP",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "#$42" },
                    isTerminator: true,
                    nextAddresses: new List<ulong> { }
                ),
                new byte[] { 0x02, 0x42 }
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
        public void Test65816_CMP_DirectPage()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "CMP",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42" }
                ),
                new byte[] { 0xC5, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_CMP_DirectPageIndirect()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "CMP",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "($42)" }
                ),
                new byte[] { 0xD2, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_CMP_DirectPageIndirectLong()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "CMP",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "[$42]" }
                ),
                new byte[] { 0xC7, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_CMP_AbsoluteIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "CMP",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234,X" }
                ),
                new byte[] { 0xDD, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_CMP_AbsoluteLongIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "CMP",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: new[] { "$123456,X" }
                ),
                new byte[] { 0xDF, 0x56, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_CMP_AbsoluteIndexedY()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "CMP",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234,Y" }
                ),
                new byte[] { 0xD9, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_CMP_DirectPageIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "CMP",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42,X" }
                ),
                new byte[] { 0xD5, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_CMP_DirectPageIndirectX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "CMP",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "($42,X)" }
                ),
                new byte[] { 0xC1, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_CMP_DirectPageIndirectIndexedY()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "CMP",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "($42),Y" }
                ),
                new byte[] { 0xD1, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_CMP_DirectPageIndirectLongIndexedY()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "CMP",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "[$42],Y" }
                ),
                new byte[] { 0xD7, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_CMP_StackRelative()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "CMP",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42,S" }
                ),
                new byte[] { 0xC3, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_CMP_StackRelativeIndexedY()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "CMP",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "($42,S),Y" }
                ),
                new byte[] { 0xD3, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_CPX_Immediate()
        {
            var testCases = new[]
            {
                // emulation mode (8-bit X)
                new { Emulation = true, A16Bit = false, X16Bit = false, Bytes = new byte[] { 0xE0, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 8-bit X
                new { Emulation = false, A16Bit = false, X16Bit = false, Bytes = new byte[] { 0xE0, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 16-bit X
                new { Emulation = false, A16Bit = false, X16Bit = true, Bytes = new byte[] { 0xE0, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 8-bit X, 16-bit A
                new { Emulation = false, A16Bit = true, X16Bit = false, Bytes = new byte[] { 0xE0, 0x42, 0x12 }, ExpectedLength = 3, ExpectedOperand = "#$1242" },
                
                // native mode, 16-bit A and X
                new { Emulation = false, A16Bit = true, X16Bit = true, Bytes = new byte[] { 0xE0, 0x42, 0x12 }, ExpectedLength = 3, ExpectedOperand = "#$1242" }
            };

            foreach (var testCase in testCases)
            {
                SetState(testCase.Emulation, testCase.A16Bit, testCase.X16Bit);
                var result = DecodeNext(testCase.Bytes);
                AssertInstruction(
                    result,
                    mnemonic: "CPX",
                    bytesConsumed: testCase.ExpectedLength,
                    operandCount: 1,
                    operandText: new[] { testCase.ExpectedOperand }
                );
            }
        }

        [TestMethod]
        public void Test65816_CPX_Absolute()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "CPX",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234" }
                ),
                new byte[] { 0xEC, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_CPX_DirectPage()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "CPX",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42" }
                ),
                new byte[] { 0xE4, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_CPY_Immediate()
        {
            var testCases = new[]
            {
                // emulation mode (8-bit Y)
                new { Emulation = true, A16Bit = false, X16Bit = false, Bytes = new byte[] { 0xC0, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 8-bit Y
                new { Emulation = false, A16Bit = false, X16Bit = false, Bytes = new byte[] { 0xC0, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 16-bit Y
                new { Emulation = false, A16Bit = false, X16Bit = true, Bytes = new byte[] { 0xC0, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 8-bit Y, 16-bit A
                new { Emulation = false, A16Bit = true, X16Bit = false, Bytes = new byte[] { 0xC0, 0x42, 0x12 }, ExpectedLength = 3, ExpectedOperand = "#$1242" },
                
                // native mode, 16-bit A and Y
                new { Emulation = false, A16Bit = true, X16Bit = true, Bytes = new byte[] { 0xC0, 0x42, 0x12 }, ExpectedLength = 3, ExpectedOperand = "#$1242" }
            };

            foreach (var testCase in testCases)
            {
                SetState(testCase.Emulation, testCase.A16Bit, testCase.X16Bit);
                var result = DecodeNext(testCase.Bytes);
                AssertInstruction(
                    result,
                    mnemonic: "CPY",
                    bytesConsumed: testCase.ExpectedLength,
                    operandCount: 1,
                    operandText: new[] { testCase.ExpectedOperand }
                );
            }
        }

        [TestMethod]
        public void Test65816_CPY_Absolute()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "CPY",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234" }
                ),
                new byte[] { 0xCC, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_CPY_DirectPage()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "CPY",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42" }
                ),
                new byte[] { 0xC4, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_DEC_Accumulator()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "DEC",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                new byte[] { 0x3A }
            );
        }

        [TestMethod]
        public void Test65816_DEC_Absolute()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "DEC",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234" }
                ),
                new byte[] { 0xCE, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_DEC_DirectPage()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "DEC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42" }
                ),
                new byte[] { 0xC6, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_DEC_AbsoluteIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "DEC",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234,X" }
                ),
                new byte[] { 0xDE, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_DEC_DirectPageIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "DEC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42,X" }
                ),
                new byte[] { 0xD6, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_EOR_Immediate()
        {
            var testCases = new[]
            {
                // emulation mode (8-bit A)
                new { Emulation = true, A16Bit = false, X16Bit = false, Bytes = new byte[] { 0x49, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 8-bit A
                new { Emulation = false, A16Bit = false, X16Bit = false, Bytes = new byte[] { 0x49, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 16-bit A
                new { Emulation = false, A16Bit = true, X16Bit = false, Bytes = new byte[] { 0x49, 0x42, 0x12 }, ExpectedLength = 3, ExpectedOperand = "#$1242" },
                
                // native mode, 8-bit A, 16-bit X
                new { Emulation = false, A16Bit = false, X16Bit = true, Bytes = new byte[] { 0x49, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 16-bit A and X
                new { Emulation = false, A16Bit = true, X16Bit = true, Bytes = new byte[] { 0x49, 0x42, 0x12 }, ExpectedLength = 3, ExpectedOperand = "#$1242" }
            };

            foreach (var testCase in testCases)
            {
                SetState(testCase.Emulation, testCase.A16Bit, testCase.X16Bit);
                var result = DecodeNext(testCase.Bytes);
                AssertInstruction(
                    result,
                    mnemonic: "EOR",
                    bytesConsumed: testCase.ExpectedLength,
                    operandCount: 1,
                    operandText: new[] { testCase.ExpectedOperand }
                );
            }
        }

        [TestMethod]
        public void Test65816_EOR_Absolute()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "EOR",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234" }
                ),
                new byte[] { 0x4D, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_EOR_AbsoluteLong()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "EOR",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: new[] { "$123456" }
                ),
                new byte[] { 0x4F, 0x56, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_EOR_DirectPage()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "EOR",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42" }
                ),
                new byte[] { 0x45, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_EOR_DirectPageIndirect()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "EOR",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "($42)" }
                ),
                new byte[] { 0x52, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_EOR_DirectPageIndirectLong()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "EOR",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "[$42]" }
                ),
                new byte[] { 0x47, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_EOR_AbsoluteIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "EOR",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234,X" }
                ),
                new byte[] { 0x5D, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_EOR_AbsoluteLongIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "EOR",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: new[] { "$123456,X" }
                ),
                new byte[] { 0x5F, 0x56, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_EOR_AbsoluteIndexedY()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "EOR",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234,Y" }
                ),
                new byte[] { 0x59, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_EOR_DirectPageIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "EOR",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42,X" }
                ),
                new byte[] { 0x55, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_EOR_DirectPageIndirectX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "EOR",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "($42,X)" }
                ),
                new byte[] { 0x41, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_EOR_DirectPageIndirectIndexedY()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "EOR",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "($42),Y" }
                ),
                new byte[] { 0x51, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_EOR_DirectPageIndirectLongIndexedY()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "EOR",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "[$42],Y" }
                ),
                new byte[] { 0x57, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_EOR_StackRelative()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "EOR",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42,S" }
                ),
                new byte[] { 0x43, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_EOR_StackRelativeIndexedY()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "EOR",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "($42,S),Y" }
                ),
                new byte[] { 0x53, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_INC_Accumulator()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "INC",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                new byte[] { 0x1A }
            );
        }

        [TestMethod]
        public void Test65816_INC_Absolute()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "INC",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234" }
                ),
                new byte[] { 0xEE, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_INC_DirectPage()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "INC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42" }
                ),
                new byte[] { 0xE6, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_INC_AbsoluteIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "INC",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234,X" }
                ),
                new byte[] { 0xFE, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_INC_DirectPageIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "INC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42,X" }
                ),
                new byte[] { 0xF6, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_INX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "INX",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                new byte[] { 0xE8 }
            );
        }

        [TestMethod]
        public void Test65816_INY()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "INY",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                new byte[] { 0xC8 }
            );
        }

        [TestMethod]
        public void Test65816_JMP_Absolute()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "JMP",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234" },
                    isBranch: true,
                    isTerminator: true,
                    nextAddresses: new List<ulong> { 0x1234 }
                ),
                new byte[] { 0x4C, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_JMP_AbsoluteIndirect()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "JMP",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "($1234)" },
                    isBranch: true,
                    isTerminator: true,
                    nextAddresses: new List<ulong> { 0x1234 }
                ),
                new byte[] { 0x6C, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_JMP_AbsoluteIndirectIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "JMP",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "($1234,X)" },
                    isBranch: true,
                    isTerminator: true,
                    nextAddresses: new List<ulong> { 0x1234 }
                ),
                new byte[] { 0x7C, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_JMP_AbsoluteLong()
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
                    nextAddresses: new List<ulong> { 0x123456 }
                ),
                new byte[] { 0x5C, 0x56, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_JMP_AbsoluteLongIndirect()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "JMP",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: new[] { "[$123456]" },
                    isBranch: true,
                    isTerminator: true,
                    nextAddresses: new List<ulong> { 0x123456 }
                ),
                new byte[] { 0xDC, 0x56, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_JSR_Absolute()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "JSR",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234" },
                    isBranch: true,
                    isTerminator: true,
                    nextAddresses: new List<ulong> { 0x1234 }
                ),
                new byte[] { 0x20, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_JSR_AbsoluteIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "JSR",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "($1234,X)" },
                    isBranch: true,
                    isTerminator: true,
                    nextAddresses: new List<ulong> { 0x1234 }
                ),
                new byte[] { 0xFC, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_JSL_AbsoluteLong()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "JSL",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: new[] { "$123456" },
                    isBranch: true,
                    isTerminator: true,
                    nextAddresses: new List<ulong> { 0x123456 }
                ),
                new byte[] { 0x22, 0x56, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_LDA_Immediate()
        {
            var testCases = new[]
            {
                // emulation mode (8-bit A)
                new { Emulation = true, A16Bit = false, X16Bit = false, Bytes = new byte[] { 0xA9, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 8-bit A
                new { Emulation = false, A16Bit = false, X16Bit = false, Bytes = new byte[] { 0xA9, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 16-bit A
                new { Emulation = false, A16Bit = true, X16Bit = false, Bytes = new byte[] { 0xA9, 0x42, 0x12 }, ExpectedLength = 3, ExpectedOperand = "#$1242" },
                
                // native mode, 8-bit A, 16-bit X
                new { Emulation = false, A16Bit = false, X16Bit = true, Bytes = new byte[] { 0xA9, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 16-bit A and X
                new { Emulation = false, A16Bit = true, X16Bit = true, Bytes = new byte[] { 0xA9, 0x42, 0x12 }, ExpectedLength = 3, ExpectedOperand = "#$1242" }
            };

            foreach (var testCase in testCases)
            {
                SetState(testCase.Emulation, testCase.A16Bit, testCase.X16Bit);
                var result = DecodeNext(testCase.Bytes);
                AssertInstruction(
                    result,
                    mnemonic: "LDA",
                    bytesConsumed: testCase.ExpectedLength,
                    operandCount: 1,
                    operandText: new[] { testCase.ExpectedOperand }
                );
            }
        }

        [TestMethod]
        public void Test65816_LDA_Absolute()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "LDA",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234" }
                ),
                new byte[] { 0xAD, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_LDA_AbsoluteLong()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "LDA",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: new[] { "$123456" }
                ),
                new byte[] { 0xAF, 0x56, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_LDA_DirectPage()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "LDA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42" }
                ),
                new byte[] { 0xA5, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_LDA_DirectPageIndirect()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "LDA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "($42)" }
                ),
                new byte[] { 0xB2, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_LDA_DirectPageIndirectLong()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "LDA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "[$42]" }
                ),
                new byte[] { 0xA7, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_LDA_AbsoluteIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "LDA",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234,X" }
                ),
                new byte[] { 0xBD, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_LDA_AbsoluteLongIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "LDA",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: new[] { "$123456,X" }
                ),
                new byte[] { 0xBF, 0x56, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_LDA_AbsoluteIndexedY()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "LDA",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234,Y" }
                ),
                new byte[] { 0xB9, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_LDA_DirectPageIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "LDA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42,X" }
                ),
                new byte[] { 0xB5, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_LDA_DirectPageIndirectX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "LDA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "($42,X)" }
                ),
                new byte[] { 0xA1, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_LDA_DirectPageIndirectIndexedY()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "LDA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "($42),Y" }
                ),
                new byte[] { 0xB1, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_LDA_DirectPageIndirectLongIndexedY()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "LDA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "[$42],Y" }
                ),
                new byte[] { 0xB7, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_LDA_StackRelative()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "LDA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42,S" }
                ),
                new byte[] { 0xA3, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_LDA_StackRelativeIndexedY()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "LDA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "($42,S),Y" }
                ),
                new byte[] { 0xB3, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_LDX_Immediate()
        {
            var testCases = new[]
            {
                // emulation mode (8-bit X)
                new { Emulation = true, A16Bit = false, X16Bit = false, Bytes = new byte[] { 0xA2, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 8-bit X
                new { Emulation = false, A16Bit = false, X16Bit = false, Bytes = new byte[] { 0xA2, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 16-bit X
                new { Emulation = false, A16Bit = false, X16Bit = true, Bytes = new byte[] { 0xA2, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 8-bit X, 16-bit A
                new { Emulation = false, A16Bit = true, X16Bit = false, Bytes = new byte[] { 0xA2, 0x42, 0x12 }, ExpectedLength = 3, ExpectedOperand = "#$1242" },
                
                // native mode, 16-bit A and X
                new { Emulation = false, A16Bit = true, X16Bit = true, Bytes = new byte[] { 0xA2, 0x42, 0x12 }, ExpectedLength = 3, ExpectedOperand = "#$1242" }
            };

            foreach (var testCase in testCases)
            {
                SetState(testCase.Emulation, testCase.A16Bit, testCase.X16Bit);
                var result = DecodeNext(testCase.Bytes);
                AssertInstruction(
                    result,
                    mnemonic: "LDX",
                    bytesConsumed: testCase.ExpectedLength,
                    operandCount: 1,
                    operandText: new[] { testCase.ExpectedOperand }
                );
            }
        }

        [TestMethod]
        public void Test65816_LDX_Absolute()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "LDX",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234" }
                ),
                new byte[] { 0xAE, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_LDX_DirectPage()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "LDX",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42" }
                ),
                new byte[] { 0xA6, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_LDX_AbsoluteIndexedY()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "LDX",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234,Y" }
                ),
                new byte[] { 0xBE, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_LDX_DirectPageIndexedY()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "LDX",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42,Y" }
                ),
                new byte[] { 0xB6, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_LDY_Immediate()
        {
            var testCases = new[]
            {
                // emulation mode (8-bit Y)
                new { Emulation = true, A16Bit = false, X16Bit = false, Bytes = new byte[] { 0xA0, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 8-bit Y
                new { Emulation = false, A16Bit = false, X16Bit = false, Bytes = new byte[] { 0xA0, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 16-bit Y
                new { Emulation = false, A16Bit = false, X16Bit = true, Bytes = new byte[] { 0xA0, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 8-bit Y, 16-bit A
                new { Emulation = false, A16Bit = true, X16Bit = false, Bytes = new byte[] { 0xA0, 0x42, 0x12 }, ExpectedLength = 3, ExpectedOperand = "#$1242" },
                
                // native mode, 16-bit A and Y
                new { Emulation = false, A16Bit = true, X16Bit = true, Bytes = new byte[] { 0xA0, 0x42, 0x12 }, ExpectedLength = 3, ExpectedOperand = "#$1242" }
            };

            foreach (var testCase in testCases)
            {
                SetState(testCase.Emulation, testCase.A16Bit, testCase.X16Bit);
                var result = DecodeNext(testCase.Bytes);
                AssertInstruction(
                    result,
                    mnemonic: "LDY",
                    bytesConsumed: testCase.ExpectedLength,
                    operandCount: 1,
                    operandText: new[] { testCase.ExpectedOperand }
                );
            }
        }

        [TestMethod]
        public void Test65816_LDY_Absolute()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "LDY",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234" }
                ),
                new byte[] { 0xAC, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_LDY_DirectPage()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "LDY",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42" }
                ),
                new byte[] { 0xA4, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_LDY_AbsoluteIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "LDY",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234,X" }
                ),
                new byte[] { 0xBC, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_LDY_DirectPageIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "LDY",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42,X" }
                ),
                new byte[] { 0xB4, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_LSR_Accumulator()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "LSR",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                new byte[] { 0x4A }
            );
        }

        [TestMethod]
        public void Test65816_LSR_Absolute()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "LSR",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234" }
                ),
                new byte[] { 0x4E, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_LSR_DirectPage()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "LSR",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42" }
                ),
                new byte[] { 0x46, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_LSR_AbsoluteIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "LSR",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234,X" }
                ),
                new byte[] { 0x5E, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_LSR_DirectPageIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "LSR",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42,X" }
                ),
                new byte[] { 0x56, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_MVN()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "MVN",
                    bytesConsumed: 3,
                    operandCount: 2,
                    operandText: new[] { "$12", "$34" },
                    isTerminator: true,
                    nextAddresses: new List<ulong> { }
                ),
                new byte[] { 0x54, 0x12, 0x34 }
            );
        }

        [TestMethod]
        public void Test65816_MVP()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "MVP",
                    bytesConsumed: 3,
                    operandCount: 2,
                    operandText: new[] { "$12", "$34" },
                    isTerminator: true,
                    nextAddresses: new List<ulong> { }
                ),
                new byte[] { 0x44, 0x12, 0x34 }
            );
        }

        [TestMethod]
        public void Test65816_NOP()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "NOP",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                new byte[] { 0xEA }
            );
        }

        [TestMethod]
        public void Test65816_ORA_Immediate()
        {
            var testCases = new[]
            {
                // emulation mode (8-bit A)
                new { Emulation = true, A16Bit = false, X16Bit = false, Bytes = new byte[] { 0x09, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 8-bit A
                new { Emulation = false, A16Bit = false, X16Bit = false, Bytes = new byte[] { 0x09, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 16-bit A
                new { Emulation = false, A16Bit = true, X16Bit = false, Bytes = new byte[] { 0x09, 0x42, 0x12 }, ExpectedLength = 3, ExpectedOperand = "#$1242" },
                
                // native mode, 8-bit A, 16-bit X
                new { Emulation = false, A16Bit = false, X16Bit = true, Bytes = new byte[] { 0x09, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 16-bit A and X
                new { Emulation = false, A16Bit = true, X16Bit = true, Bytes = new byte[] { 0x09, 0x42, 0x12 }, ExpectedLength = 3, ExpectedOperand = "#$1242" }
            };

            foreach (var testCase in testCases)
            {
                SetState(testCase.Emulation, testCase.A16Bit, testCase.X16Bit);
                var result = DecodeNext(testCase.Bytes);
                AssertInstruction(
                    result,
                    mnemonic: "ORA",
                    bytesConsumed: testCase.ExpectedLength,
                    operandCount: 1,
                    operandText: new[] { testCase.ExpectedOperand }
                );
            }
        }

        [TestMethod]
        public void Test65816_ORA_Absolute()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ORA",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234" }
                ),
                new byte[] { 0x0D, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_ORA_AbsoluteLong()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ORA",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: new[] { "$123456" }
                ),
                new byte[] { 0x0F, 0x56, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_ORA_DirectPage()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ORA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42" }
                ),
                new byte[] { 0x05, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_ORA_DirectPageIndirect()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ORA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "($42)" }
                ),
                new byte[] { 0x12, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_ORA_DirectPageIndirectLong()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ORA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "[$42]" }
                ),
                new byte[] { 0x07, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_ORA_AbsoluteIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ORA",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234,X" }
                ),
                new byte[] { 0x1D, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_ORA_AbsoluteLongIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ORA",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: new[] { "$123456,X" }
                ),
                new byte[] { 0x1F, 0x56, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_ORA_AbsoluteIndexedY()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ORA",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: new[] { "$1234,Y" }
                ),
                new byte[] { 0x19, 0x34, 0x12 }
            );
        }

        [TestMethod]
        public void Test65816_ORA_DirectPageIndexedX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ORA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42,X" }
                ),
                new byte[] { 0x15, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_ORA_DirectPageIndirectX()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ORA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "($42,X)" }
                ),
                new byte[] { 0x01, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_ORA_DirectPageIndirectIndexedY()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ORA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "($42),Y" }
                ),
                new byte[] { 0x11, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_ORA_DirectPageIndirectLongIndexedY()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ORA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "[$42],Y" }
                ),
                new byte[] { 0x17, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_ORA_StackRelative()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ORA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "$42,S" }
                ),
                new byte[] { 0x03, 0x42 }
            );
        }

        [TestMethod]
        public void Test65816_ORA_StackRelativeIndexedY()
        {
            TestInAllStates(result => 
                AssertInstruction(
                    result,
                    mnemonic: "ORA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: new[] { "($42,S),Y" }
                ),
                new byte[] { 0x13, 0x42 }
            );
        }
    }
} 