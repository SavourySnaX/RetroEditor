using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroEditor.Source.Internals.ReverseEngineering.Platform;

namespace RetroEditor.Tests
{
    public class _65816_Checker
    {
        private readonly SNES65816Disassembler _disassembler;
        private readonly IMemoryMapper _memoryMapper;

        protected _65816_Checker()
        {
            _disassembler = new SNES65816Disassembler();
            _memoryMapper = new PassthroughMemoryMapper();
        }

        private sealed class PassthroughMemoryMapper : IMemoryMapper
        {
            public ulong MapCpuToRegion(ulong cpuAddress, out MemoryRegionKey region)
            {
                region = new MemoryRegionKey((uint)MemoryInformationRegion.ROM);
                return cpuAddress;
            }

            public ulong MapRomToCpu(ulong romAddress) => romAddress;

            public ulong MapHardwareAddressToCpu(ulong linearAddress) => linearAddress;

            public ulong MapCpuToHardwareAddress(ulong address, out MemoryRegionKey region)
            {
                region = new MemoryRegionKey((uint)MemoryInformationRegion.ROM);
                return address;
            }
        }

        internal DecodeResult DecodeNext(byte[] bytes, ulong address = 0x8000)
        {
            return _disassembler.DecodeNext(bytes, address);
        }

        internal List<(ulong address, uint size)> FetchMemoryAccesses(Instruction instruction, ICpuRegisterState registers)
        {
            return _disassembler
                .FetchMappedAccesses(instruction, registers, _memoryMapper)
                .Where(access => access.RegionKey.Key != (uint)MemoryInformationRegion.IO
                                 && access.RegionKey.Key != (uint)MemoryInformationRegion.Invalid)
                .Select(access => (access.Address, access.Size))
                .ToList();
        }

        internal List<(ulong address, uint size, MemoryAccessDirection direction)> FetchMemoryAccessesWithDirection(Instruction instruction, ICpuRegisterState registers)
        {
            return _disassembler
                .FetchMappedAccesses(instruction, registers, _memoryMapper)
                .Where(access => access.RegionKey.Key != (uint)MemoryInformationRegion.IO
                                 && access.RegionKey.Key != (uint)MemoryInformationRegion.Invalid)
                .Select(access => (access.Address, access.Size, access.Direction))
                .ToList();
        }

        internal void SetState(bool emulation, bool a16bit, bool x16bit)
        {
            var state = (SNES65816State)_disassembler.State;
            state.SetEmulationMode(emulation);
            state.Accumulator8Bit = !a16bit;
            state.Index8Bit = !x16bit;
            _disassembler.State=state;
        }

        internal void TestInAllStates(Action<byte[], DecodeResult> testAction, byte[] bytes, ulong address = 0x8000)
        {
            // Test in emulation mode
            SetState(emulation: true, a16bit: false, x16bit: false);
            testAction(bytes, DecodeNext(bytes, address));

            // Test in native mode with 8-bit A and X
            SetState(emulation: false, a16bit: false, x16bit: false);
            testAction(bytes, DecodeNext(bytes, address));

            // Test in native mode with 16-bit A and 8-bit X
            SetState(emulation: false, a16bit: true, x16bit: false);
            testAction(bytes, DecodeNext(bytes, address));

            // Test in native mode with 8-bit A and 16-bit X
            SetState(emulation: false, a16bit: false, x16bit: true);
            testAction(bytes, DecodeNext(bytes, address));

            // Test in native mode with 16-bit A and X
            SetState(emulation: false, a16bit: true, x16bit: true);
            testAction(bytes, DecodeNext(bytes, address));
        }

        internal void AssertInstruction(
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
                int o = 0;
                foreach (var operand in operands)
                {
                    Assert.IsNotNull(operand);
                    Assert.AreEqual(operandText[o], operand.Text());
                    o++;
                }
            }

            Assert.AreEqual(isBranch, instruction.IsBranch);
            Assert.AreEqual(isTerminator, instruction.IsBasicBlockTerminator);

            if (nextAddresses==null && !(instruction.IsBranch||instruction.IsBasicBlockTerminator))
            {
                nextAddresses = [(ulong)(0x8000 + result.BytesConsumed)];
            }

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
    public class Disassembler_65816_Tests : _65816_Checker
    {
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
                    expectedBytes: testCase.Bytes,
                    mnemonic: "ADC",
                    bytesConsumed: testCase.ExpectedLength,
                    operandCount: 1,
                    operandText: [testCase.ExpectedOperand]
                );
            }
        }
        
        [TestMethod]
        public void Test65816_ADC_Absolute()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ADC",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234"]
                ),
                [0x6D, 0x34, 0x12]
            );
        }
        
        [TestMethod]
        public void Test65816_ADC_AbsoluteLong()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ADC",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: ["$123456"]
                ),
                [0x6F, 0x56, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_ADC_DirectPage()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ADC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42"]
                ),
                [0x65, 0x42]
            );
        }
        
        [TestMethod]
        public void Test65816_ADC_DirectPageIndirect()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ADC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42)"]
                ),
                [0x72, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_ADC_DirectPageIndirectLong()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ADC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["[$42]"]
                ),
                [0x67, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_ADC_AbsoluteIndexedX()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ADC",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234,X"]
                ),
                [0x7D, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_ADC_AbsoluteLongIndexedX()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ADC",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: ["$123456,X"]
                ),
                [0x7F, 0x56, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_ADC_AbsoluteIndexedY()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ADC",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234,Y"]
                ),
                [0x79, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_ADC_DirectPageIndexedX()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ADC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42,X"]
                ),
                [0x75, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_ADC_DirectPageIndirectX()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ADC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42,X)"]
                ),
                [0x61, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_ADC_DirectPageIndirectIndexedY()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ADC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42),Y"]
                ),
                [0x71, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_ADC_DirectPageIndirectLongIndexedY()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ADC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["[$42],Y"]
                ),
                [0x77, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_ADC_StackRelative()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ADC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42,S"]
                ),
                [0x63, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_ADC_StackRelativeIndexedY()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ADC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42,S),Y"]
                ),
                [0x73, 0x42]
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
                    expectedBytes: testCase.Bytes,
                    mnemonic: "AND",
                    bytesConsumed: testCase.ExpectedLength,
                    operandCount: 1,
                    operandText: [testCase.ExpectedOperand]
                );
            }
        }

        [TestMethod]
        public void Test65816_AND_Absolute()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "AND",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234"]
                ),
                [0x2D, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_AND_AbsoluteLong()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "AND",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: ["$123456"]
                ),
                [0x2F, 0x56, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_AND_DirectPage()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "AND",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42"]
                ),
                [0x25, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_AND_DirectPageIndirect()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "AND",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42)"]
                ),
                [0x32, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_AND_DirectPageIndirectLong()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "AND",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["[$42]"]
                ),
                [0x27, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_AND_AbsoluteIndexedX()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "AND",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234,X"]
                ),
                [0x3D, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_AND_AbsoluteLongIndexedX()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: [0x3F, 0x56, 0x34, 0x12],
                    mnemonic: "AND",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: ["$123456,X"]
                ),
                [0x3F, 0x56, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_AND_AbsoluteIndexedY()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "AND",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234,Y"]
                ),
                [0x39, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_AND_DirectPageIndexedX()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "AND",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42,X"]
                ),
                [0x35, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_AND_DirectPageIndirectX()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "AND",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42,X)"]
                ),
                [0x21, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_AND_DirectPageIndirectIndexedY()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "AND",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42),Y"]
                ),
                [0x31, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_AND_DirectPageIndirectLongIndexedY()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "AND",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["[$42],Y"]
                ),
                [0x37, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_AND_StackRelative()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "AND",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42,S"]
                ),
                [0x23, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_AND_StackRelativeIndexedY()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "AND",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42,S),Y"]
                ),
                [0x33, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_ASL_Accumulator()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ASL",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0x0A]
            );
        }

        [TestMethod]
        public void Test65816_ASL_Absolute()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ASL",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234"]
                ),
                [0x0E, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_ASL_DirectPage()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ASL",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42"]
                ),
                [0x06, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_ASL_AbsoluteIndexedX()
        {
            TestInAllStates((bytes, result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ASL",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234,X"]
                ),
                [0x1E, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_ASL_DirectPageIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ASL",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42,X"]
                ),
                [0x16, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_BCC_Forward()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "BCC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$8002"],
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x8002 }
                ),
                [0x90, 0x00]
            );
        }

        [TestMethod]
        public void Test65816_BCC_Backward()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "BCC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$7FFE"],
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x7FFE }
                ),
                [0x90, 0xFC]
            );
        }

        [TestMethod]
        public void Test65816_BCS_Forward()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "BCS",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$8002"],
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x8002 }
                ),
                [0xB0, 0x00]
            );
        }

        [TestMethod]
        public void Test65816_BCS_Backward()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "BCS",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$7FFE"],
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x7FFE }
                ),
                [0xB0, 0xFC]
            );
        }

        [TestMethod]
        public void Test65816_BEQ_Forward()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "BEQ",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$8002"],
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x8002 }
                ),
                [0xF0, 0x00]
            );
        }

        [TestMethod]
        public void Test65816_BEQ_Backward()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "BEQ",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$7FFE"],
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x7FFE }
                ),
                [0xF0, 0xFC]
            );
        }

        [TestMethod]
        public void Test65816_BMI_Forward()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "BMI",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$8002"],
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x8002 }
                ),
                [0x30, 0x00]
            );
        }

        [TestMethod]
        public void Test65816_BMI_Backward()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "BMI",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$7FFE"],
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x7FFE }
                ),
                [0x30, 0xFC]
            );
        }

        [TestMethod]
        public void Test65816_BNE_Forward()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "BNE",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$8002"],
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x8002 }
                ),
                [0xD0, 0x00]
            );
        }

        [TestMethod]
        public void Test65816_BNE_Backward()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "BNE",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$7FFE"],
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x7FFE }
                ),
                [0xD0, 0xFC]
            );
        }

        [TestMethod]
        public void Test65816_BPL_Forward()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "BPL",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$8002"],
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x8002 }
                ),
                [0x10, 0x00]
            );
        }

        [TestMethod]
        public void Test65816_BPL_Backward()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "BPL",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$7FFE"],
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x7FFE }
                ),
                [0x10, 0xFC]
            );
        }

        [TestMethod]
        public void Test65816_BRA_Forward()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "BRA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$8002"],
                    isBranch: true,
                    isTerminator: true,
                    nextAddresses: new List<ulong> { 0x8002 }
                ),
                [0x80, 0x00]
            );
        }

        [TestMethod]
        public void Test65816_BRA_Backward()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "BRA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$7FFE"],
                    isBranch: true,
                    isTerminator: true,
                    nextAddresses: new List<ulong> { 0x7FFE }
                ),
                [0x80, 0xFC]
            );
        }

        [TestMethod]
        public void Test65816_BVC_Forward()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "BVC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$8002"],
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x8002 }
                ),
                [0x50, 0x00]
            );
        }

        [TestMethod]
        public void Test65816_BVC_Backward()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "BVC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$7FFE"],
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x7FFE }
                ),
                [0x50, 0xFC]
            );
        }

        [TestMethod]
        public void Test65816_BVS_Forward()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "BVS",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$8002"],
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x8002 }
                ),
                [0x70, 0x00]
            );
        }

        [TestMethod]
        public void Test65816_BVS_Backward()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "BVS",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$7FFE"],
                    isBranch: true,
                    nextAddresses: new List<ulong> { 0x8002, 0x7FFE }
                ),
                [0x70, 0xFC]
            );
        }

        [TestMethod]
        public void Test65816_BRL_Forward()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "BRL",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$8003"],
                    isBranch: true,
                    isTerminator: true,
                    nextAddresses: new List<ulong> { 0x8003 }
                ),
                [0x82, 0x00, 0x00]
            );
        }

        [TestMethod]
        public void Test65816_BRL_Backward()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "BRL",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$7FFF"],
                    isBranch: true,
                    isTerminator: true,
                    nextAddresses: new List<ulong> { 0x7FFF }
                ),
                [0x82, 0xFC, 0xFF]
            );
        }

        [TestMethod]
        public void Test65816_BRL_LongDistance()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "BRL",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$9003"],
                    isBranch: true,
                    isTerminator: true,
                    nextAddresses: new List<ulong> { 0x9003 }
                ),
                [0x82, 0x00, 0x10]
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
                    expectedBytes: testCase.Bytes,
                    mnemonic: "BIT",
                    bytesConsumed: testCase.ExpectedLength,
                    operandCount: 1,
                    operandText: [testCase.ExpectedOperand]
                );
            }
        }

        [TestMethod]
        public void Test65816_BIT_Absolute()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "BIT",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234"]
                ),
                [0x2C, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_BIT_DirectPage()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "BIT",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42"]
                ),
                [0x24, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_BIT_AbsoluteIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "BIT",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234,X"]
                ),
                [0x3C, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_BIT_DirectPageIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "BIT",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42,X"]
                ),
                [0x34, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_BRK()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "BRK",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["#$42"],
                    isTerminator: true
                ),
                [0x00, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_COP()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "COP",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["#$42"],
                    isTerminator: true
                ),
                [0x02, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_CLC()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(result, "CLC", 1, [0x18]),
                [0x18]
            );
        }

        [TestMethod]
        public void Test65816_CLI()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(result, "CLI", 1, [0x58]),
                [0x58]
            );
        }

        [TestMethod]
        public void Test65816_CLD()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(result, "CLD", 1, [0xD8]),
                [0xD8]
            );
        }

        [TestMethod]
        public void Test65816_CLV()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(result, "CLV", 1, [0xB8]),
                [0xB8]
            );
        }


        [TestMethod]
        public void Test65816_CMP_DirectPage()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "CMP",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42"]
                ),
                [0xC5, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_CMP_DirectPageIndirect()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "CMP",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42)"]
                ),
                [0xD2, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_CMP_DirectPageIndirectLong()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "CMP",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["[$42]"]
                ),
                [0xC7, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_CMP_AbsoluteIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "CMP",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234,X"]
                ),
                [0xDD, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_CMP_AbsoluteLongIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "CMP",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: ["$123456,X"]
                ),
                [0xDF, 0x56, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_CMP_AbsoluteIndexedY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "CMP",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234,Y"]
                ),
                [0xD9, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_CMP_DirectPageIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "CMP",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42,X"]
                ),
                [0xD5, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_CMP_DirectPageIndirectX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "CMP",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42,X)"]
                ),
                [0xC1, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_CMP_DirectPageIndirectIndexedY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "CMP",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42),Y"]
                ),
                [0xD1, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_CMP_DirectPageIndirectLongIndexedY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "CMP",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["[$42],Y"]
                ),
                [0xD7, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_CMP_StackRelative()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "CMP",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42,S"]
                ),
                [0xC3, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_CMP_StackRelativeIndexedY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "CMP",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42,S),Y"]
                ),
                [0xD3, 0x42]
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
                new { Emulation = false, A16Bit = false, X16Bit = true, Bytes = new byte[] { 0xE0, 0x42, 0x12 }, ExpectedLength = 3, ExpectedOperand = "#$1242" },
                
                // native mode, 8-bit X, 16-bit A
                new { Emulation = false, A16Bit = true, X16Bit = false, Bytes = new byte[] { 0xE0, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
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
                    expectedBytes: testCase.Bytes,
                    operandCount: 1,
                    operandText: [testCase.ExpectedOperand]
                );
            }
        }

        [TestMethod]
        public void Test65816_CPX_Absolute()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "CPX",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234"]
                ),
                [0xEC, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_CPX_DirectPage()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "CPX",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42"]
                ),
                [0xE4, 0x42]
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
                new { Emulation = false, A16Bit = false, X16Bit = true, Bytes = new byte[] { 0xC0, 0x42, 0x12 }, ExpectedLength = 3, ExpectedOperand = "#$1242" },
                
                // native mode, 8-bit Y, 16-bit A
                new { Emulation = false, A16Bit = true, X16Bit = false, Bytes = new byte[] { 0xC0, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
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
                    expectedBytes: testCase.Bytes,
                    operandCount: 1,
                    operandText: [testCase.ExpectedOperand]
                );
            }
        }

        [TestMethod]
        public void Test65816_CPY_Absolute()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "CPY",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234"]
                ),
                [0xCC, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_CPY_DirectPage()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "CPY",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42"]
                ),
                [0xC4, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_DEC_Accumulator()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "DEC",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0x3A]
            );
        }
        
        // Next Address Tests
        [TestMethod]
        public void Test65816_NextAddresses_RegularInstruction()
        {
            // Regular instruction should have fall-through address only
            var result = DecodeNext(new byte[] { 0x18 }, 0x1000);  // CLC
            AssertInstruction(result, "CLC", 1, [0x18], nextAddresses: new List<ulong> { 0x1001 });
        }
        
        [TestMethod]
        public void Test65816_NextAddresses_ConditionalBranch()
        {
            // Conditional branch should have both fall-through and target addresses
            var result = DecodeNext(new byte[] { 0x90, 0x10 }, 0x2000);  // BCC +16
            var fallThrough = 0x2000UL + 2;  // Address after instruction
            var target = 0x2000UL + 2 + 0x10; // Branch target
            AssertInstruction(result, "BCC", 2, [0x90, 0x10], 1, isBranch: true, 
                nextAddresses: new List<ulong> { fallThrough, target });
        }
        
        [TestMethod]
        public void Test65816_NextAddresses_ConditionalBranch_Negative()
        {
            // Conditional branch with negative offset
            var result = DecodeNext(new byte[] { 0xB0, 0xF0 }, 0x2000);  // BCS -16
            var fallThrough = 0x2000UL + 2;  // Address after instruction
            var target = (ulong)((long)(0x2000UL + 2) + unchecked((sbyte)0xF0)); // Branch target (negative)
            AssertInstruction(result, "BCS", 2, [0xB0, 0xF0], 1, isBranch: true, 
                nextAddresses: new List<ulong> { fallThrough, target });
        }
        
        [TestMethod]
        public void Test65816_NextAddresses_UnconditionalBranch()
        {
            // Unconditional branch should have target address only
            var result = DecodeNext(new byte[] { 0x80, 0x20 }, 0x3000);  // BRA +32
            var target = 0x3000UL + 2 + 0x20; // Branch target
            AssertInstruction(result, "BRA", 2, [0x80, 0x20], 1, isBranch: true, isTerminator: true,
                nextAddresses: new List<ulong> { target });
        }
        
        [TestMethod]
        public void Test65816_NextAddresses_LongBranch()
        {
            // Long branch should have target address only
            var result = DecodeNext(new byte[] { 0x82, 0x00, 0x10 }, 0x4000);  // BRL +4096
            var target = 0x4000UL + 3 + 0x1000; // Branch target
            AssertInstruction(result, "BRL", 3, [0x82, 0x00, 0x10], 1, isBranch: true, isTerminator: true,
                nextAddresses: new List<ulong> { target });
        }
        
        [TestMethod]
        public void Test65816_NextAddresses_Jump()
        {
            // Jump should have target address only
            var result = DecodeNext(new byte[] { 0x4C, 0x00, 0x80 }, 0x5000);  // JMP $8000
            AssertInstruction(result, "JMP", 3, [0x4C, 0x00, 0x80], 1, isBranch: true, isTerminator: true,
                nextAddresses: new List<ulong> { 0x8000 });
        }
        
        [TestMethod]
        public void Test65816_NextAddresses_Call()
        {
            // Call should have target address only
            var result = DecodeNext(new byte[] { 0x20, 0x00, 0x90 }, 0x6000);  // JSR $9000
            AssertInstruction(result, "JSR", 3, [0x20, 0x00, 0x90], 1, isBranch: true, isTerminator: true,
                nextAddresses: new List<ulong> { 0x9000 });
        }
        
        [TestMethod]
        public void Test65816_NextAddresses_Return()
        {
            // Return should have no next addresses
            var result = DecodeNext(new byte[] { 0x60 }, 0x7000);  // RTS
            AssertInstruction(result, "RTS", 1, [0x60], isTerminator: true,
                nextAddresses: new List<ulong>());
        }
        
        [TestMethod]
        public void Test65816_NextAddresses_Break()
        {
            // Break should have no next addresses
            var result = DecodeNext(new byte[] { 0x00, 0x00 }, 0x8000);  // BRK
            AssertInstruction(result, "BRK", 2, [0x00, 0x00], 1, isTerminator: true,
                nextAddresses: new List<ulong>());
        }

        [TestMethod]
        public void Test65816_DEC_Absolute()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "DEC",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234"]
                ),
                [0xCE, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_DEC_DirectPage()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "DEC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42"]
                ),
                [0xC6, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_DEC_AbsoluteIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "DEC",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234,X"]
                ),
                [0xDE, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_DEC_DirectPageIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "DEC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42,X"]
                ),
                [0xD6, 0x42]
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
                    expectedBytes: testCase.Bytes,
                    operandCount: 1,
                    operandText: [testCase.ExpectedOperand]
                );
            }
        }

        [TestMethod]
        public void Test65816_EOR_Absolute()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "EOR",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234"]
                ),
                [0x4D, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_EOR_AbsoluteLong()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "EOR",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: ["$123456"]
                ),
                [0x4F, 0x56, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_EOR_DirectPage()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "EOR",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42"]
                ),
                [0x45, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_EOR_DirectPageIndirect()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "EOR",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42)"]
                ),
                [0x52, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_EOR_DirectPageIndirectLong()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "EOR",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["[$42]"]
                ),
                [0x47, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_EOR_AbsoluteIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "EOR",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234,X"]
                ),
                [0x5D, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_EOR_AbsoluteLongIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "EOR",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: ["$123456,X"]
                ),
                [0x5F, 0x56, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_EOR_AbsoluteIndexedY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "EOR",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234,Y"]
                ),
                [0x59, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_EOR_DirectPageIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "EOR",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42,X"]
                ),
                [0x55, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_EOR_DirectPageIndirectX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "EOR",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42,X)"]
                ),
                [0x41, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_EOR_DirectPageIndirectIndexedY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "EOR",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42),Y"]
                ),
                [0x51, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_EOR_DirectPageIndirectLongIndexedY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "EOR",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["[$42],Y"]
                ),
                [0x57, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_EOR_StackRelative()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "EOR",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42,S"]
                ),
                [0x43, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_EOR_StackRelativeIndexedY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "EOR",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42,S),Y"]
                ),
                [0x53, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_INC_Accumulator()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "INC",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0x1A]
            );
        }

        [TestMethod]
        public void Test65816_INC_Absolute()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "INC",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234"]
                ),
                [0xEE, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_INC_DirectPage()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "INC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42"]
                ),
                [0xE6, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_INC_AbsoluteIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "INC",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234,X"]
                ),
                [0xFE, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_INC_DirectPageIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "INC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42,X"]
                ),
                [0xF6, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_INX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "INX",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0xE8]
            );
        }

        [TestMethod]
        public void Test65816_INY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "INY",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0xC8]
            );
        }

        [TestMethod]
        public void Test65816_JMP_Absolute()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "JMP",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234"],
                    isBranch: true,
                    isTerminator: true,
                    nextAddresses: new List<ulong> { 0x1234 }
                ),
                [0x4C, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_JMP_AbsoluteIndirect()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "JMP",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["($1234)"],
                    isBranch: true,
                    isTerminator: true
                ),
                [0x6C, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_JMP_AbsoluteIndirectIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "JMP",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["($1234,X)"],
                    isBranch: true,
                    isTerminator: true
                ),
                [0x7C, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_JMP_AbsoluteLong()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "JMP",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: ["$123456"],
                    isBranch: true,
                    isTerminator: true,
                    nextAddresses: new List<ulong> { 0x123456 }
                ),
                [0x5C, 0x56, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_JMP_AbsoluteLongIndirect()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "JMP",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["[$1234]"],
                    isBranch: true,
                    isTerminator: true
                ),
                [0xDC, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_JSR_Absolute()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "JSR",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234"],
                    isBranch: true,
                    isTerminator: true,
                    nextAddresses: new List<ulong> { 0x1234 }
                ),
                [0x20, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_JSR_AbsoluteIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "JSR",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["($1234,X)"],
                    isBranch: true,
                    isTerminator: true
                ),
                [0xFC, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_JSL_AbsoluteLong()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "JSL",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: ["$123456"],
                    isBranch: true,
                    isTerminator: true,
                    nextAddresses: new List<ulong> { 0x123456 }
                ),
                [0x22, 0x56, 0x34, 0x12]
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
                    expectedBytes: testCase.Bytes,
                    mnemonic: "LDA",
                    bytesConsumed: testCase.ExpectedLength,
                    operandCount: 1,
                    operandText: [testCase.ExpectedOperand]
                );
            }
        }

        [TestMethod]
        public void Test65816_LDA_Absolute()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "LDA",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234"]
                ),
                [0xAD, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_LDA_AbsoluteLong()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "LDA",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: ["$123456"]
                ),
                [0xAF, 0x56, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_LDA_DirectPage()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "LDA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42"]
                ),
                [0xA5, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_LDA_DirectPageIndirect()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "LDA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42)"]
                ),
                [0xB2, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_LDA_DirectPageIndirectLong()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "LDA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["[$42]"]
                ),
                [0xA7, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_LDA_AbsoluteIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "LDA",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234,X"]
                ),
                [0xBD, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_LDA_AbsoluteLongIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "LDA",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: ["$123456,X"]
                ),
                [0xBF, 0x56, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_LDA_AbsoluteIndexedY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "LDA",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234,Y"]
                ),
                [0xB9, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_LDA_DirectPageIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "LDA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42,X"]
                ),
                [0xB5, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_LDA_DirectPageIndirectX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "LDA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42,X)"]
                ),
                [0xA1, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_LDA_DirectPageIndirectIndexedY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "LDA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42),Y"]
                ),
                [0xB1, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_LDA_DirectPageIndirectLongIndexedY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "LDA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["[$42],Y"]
                ),
                [0xB7, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_LDA_StackRelative()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "LDA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42,S"]
                ),
                [0xA3, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_LDA_StackRelativeIndexedY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "LDA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42,S),Y"]
                ),
                [0xB3, 0x42]
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
                new { Emulation = false, A16Bit = false, X16Bit = true, Bytes = new byte[] { 0xA2, 0x42, 0x12 }, ExpectedLength = 3, ExpectedOperand = "#$1242" },
                
                // native mode, 8-bit X, 16-bit A
                new { Emulation = false, A16Bit = true, X16Bit = false, Bytes = new byte[] { 0xA2, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 16-bit A and X
                new { Emulation = false, A16Bit = true, X16Bit = true, Bytes = new byte[] { 0xA2, 0x42, 0x12 }, ExpectedLength = 3, ExpectedOperand = "#$1242" }
            };

            foreach (var testCase in testCases)
            {
                SetState(testCase.Emulation, testCase.A16Bit, testCase.X16Bit);
                var result = DecodeNext(testCase.Bytes);
                AssertInstruction(
                    result,
                    expectedBytes: testCase.Bytes,
                    mnemonic: "LDX",
                    bytesConsumed: testCase.ExpectedLength,
                    operandCount: 1,
                    operandText: [testCase.ExpectedOperand]
                );
            }
        }

        [TestMethod]
        public void Test65816_LDX_Absolute()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "LDX",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234"]
                ),
                [0xAE, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_LDX_DirectPage()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "LDX",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42"]
                ),
                [0xA6, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_LDX_AbsoluteIndexedY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "LDX",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234,Y"]
                ),
                [0xBE, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_LDX_DirectPageIndexedY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "LDX",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42,Y"]
                ),
                [0xB6, 0x42]
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
                new { Emulation = false, A16Bit = false, X16Bit = true, Bytes = new byte[] { 0xA0, 0x42, 0x12 }, ExpectedLength = 3, ExpectedOperand = "#$1242" },
                
                // native mode, 8-bit Y, 16-bit A
                new { Emulation = false, A16Bit = true, X16Bit = false, Bytes = new byte[] { 0xA0, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 16-bit A and Y
                new { Emulation = false, A16Bit = true, X16Bit = true, Bytes = new byte[] { 0xA0, 0x42, 0x12 }, ExpectedLength = 3, ExpectedOperand = "#$1242" }
            };

            foreach (var testCase in testCases)
            {
                SetState(testCase.Emulation, testCase.A16Bit, testCase.X16Bit);
                var result = DecodeNext(testCase.Bytes);
                AssertInstruction(
                    result,
                    expectedBytes: testCase.Bytes,
                    mnemonic: "LDY",
                    bytesConsumed: testCase.ExpectedLength,
                    operandCount: 1,
                    operandText: [testCase.ExpectedOperand]
                );
            }
        }

        [TestMethod]
        public void Test65816_LDY_Absolute()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "LDY",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234"]
                ),
                [0xAC, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_LDY_DirectPage()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "LDY",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42"]
                ),
                [0xA4, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_LDY_AbsoluteIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "LDY",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234,X"]
                ),
                [0xBC, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_LDY_DirectPageIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "LDY",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42,X"]
                ),
                [0xB4, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_LSR_Accumulator()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "LSR",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0x4A]
            );
        }

        [TestMethod]
        public void Test65816_LSR_Absolute()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "LSR",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234"]
                ),
                [0x4E, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_LSR_DirectPage()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "LSR",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42"]
                ),
                [0x46, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_LSR_AbsoluteIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "LSR",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234,X"]
                ),
                [0x5E, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_LSR_DirectPageIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "LSR",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42,X"]
                ),
                [0x56, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_MVN()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "MVN",
                    bytesConsumed: 3,
                    operandCount: 2,
                    operandText: ["$12", "$34"]
                ),
                [0x54, 0x12, 0x34]
            );
        }

        [TestMethod]
        public void Test65816_MVP()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "MVP",
                    bytesConsumed: 3,
                    operandCount: 2,
                    operandText: ["$12", "$34"]
                ),
                [0x44, 0x12, 0x34]
            );
        }

        [TestMethod]
        public void Test65816_NOP()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "NOP",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0xEA]
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
                    expectedBytes: testCase.Bytes,
                    mnemonic: "ORA",
                    bytesConsumed: testCase.ExpectedLength,
                    operandCount: 1,
                    operandText: [testCase.ExpectedOperand]
                );
            }
        }

        [TestMethod]
        public void Test65816_ORA_Absolute()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ORA",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234"]
                ),
                [0x0D, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_ORA_AbsoluteLong()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ORA",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: ["$123456"]
                ),
                [0x0F, 0x56, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_ORA_DirectPage()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ORA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42"]
                ),
                [0x05, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_ORA_DirectPageIndirect()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ORA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42)"]
                ),
                [0x12, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_ORA_DirectPageIndirectLong()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ORA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["[$42]"]
                ),
                [0x07, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_ORA_AbsoluteIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ORA",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234,X"]
                ),
                [0x1D, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_ORA_AbsoluteLongIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ORA",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: ["$123456,X"]
                ),
                [0x1F, 0x56, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_ORA_AbsoluteIndexedY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ORA",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234,Y"]
                ),
                [0x19, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_ORA_DirectPageIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ORA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42,X"]
                ),
                [0x15, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_ORA_DirectPageIndirectX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ORA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42,X)"]
                ),
                [0x01, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_ORA_DirectPageIndirectIndexedY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ORA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42),Y"]
                ),
                [0x11, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_ORA_DirectPageIndirectLongIndexedY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ORA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["[$42],Y"]
                ),
                [0x17, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_ORA_StackRelative()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ORA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42,S"]
                ),
                [0x03, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_ORA_StackRelativeIndexedY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ORA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42,S),Y"]
                ),
                [0x13, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_PEA()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "PEA",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234"]
                ),
                [0xF4, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_PEI()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "PEI",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42)"]
                ),
                [0xD4, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_PER()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "PER",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$9203"]
                ),
                [0x62, 0x00, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_PHA()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "PHA",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0x48]
            );
        }

        [TestMethod]
        public void Test65816_PHB()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "PHB",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0x8B]
            );
        }

        [TestMethod]
        public void Test65816_PHD()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "PHD",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0x0B]
            );
        }

        [TestMethod]
        public void Test65816_PHK()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "PHK",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0x4B]
            );
        }

        [TestMethod]
        public void Test65816_PHP()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "PHP",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0x08]
            );
        }

        [TestMethod]
        public void Test65816_PHX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "PHX",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0xDA]
            );
        }

        [TestMethod]
        public void Test65816_PHY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "PHY",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0x5A]
            );
        }

        [TestMethod]
        public void Test65816_PLA()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "PLA",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0x68]
            );
        }

        [TestMethod]
        public void Test65816_PLB()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "PLB",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0xAB]
            );
        }

        [TestMethod]
        public void Test65816_PLD()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "PLD",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0x2B]
            );
        }

        [TestMethod]
        public void Test65816_PLP()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "PLP",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0x28]
            );
        }

        [TestMethod]
        public void Test65816_PLX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "PLX",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0xFA]
            );
        }

        [TestMethod]
        public void Test65816_PLY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "PLY",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0x7A]
            );
        }

        [TestMethod]
        public void Test65816_REP()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "REP",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["#$42"]
                ),
                [0xC2, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_ROL_Accumulator()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ROL",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0x2A]
            );
        }

        [TestMethod]
        public void Test65816_ROL_Absolute()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ROL",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234"]
                ),
                [0x2E, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_ROL_DirectPage()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ROL",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42"]
                ),
                [0x26, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_ROL_AbsoluteIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ROL",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234,X"]
                ),
                [0x3E, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_ROL_DirectPageIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ROL",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42,X"]
                ),
                [0x36, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_ROR_Accumulator()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ROR",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0x6A]
            );
        }

        [TestMethod]
        public void Test65816_ROR_Absolute()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ROR",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234"]
                ),
                [0x6E, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_ROR_DirectPage()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ROR",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42"]
                ),
                [0x66, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_ROR_AbsoluteIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ROR",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234,X"]
                ),
                [0x7E, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_ROR_DirectPageIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "ROR",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42,X"]
                ),
                [0x76, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_RTI()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "RTI",
                    bytesConsumed: 1,
                    operandCount: 0,
                    isTerminator: true
                ),
                [0x40]
            );
        }

        [TestMethod]
        public void Test65816_RTS()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "RTS",
                    bytesConsumed: 1,
                    operandCount: 0,
                    isTerminator: true
                ),
                [0x60]
            );
        }

        [TestMethod]
        public void Test65816_RTL()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "RTL",
                    bytesConsumed: 1,
                    operandCount: 0,
                    isTerminator: true
                ),
                [0x6B]
            );
        }

        [TestMethod]
        public void Test65816_SBC_Immediate()
        {
            var testCases = new[]
            {
                // emulation mode (8-bit A)
                new { Emulation = true, A16Bit = false, X16Bit = false, Bytes = new byte[] { 0xE9, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 8-bit A
                new { Emulation = false, A16Bit = false, X16Bit = false, Bytes = new byte[] { 0xE9, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 16-bit A
                new { Emulation = false, A16Bit = true, X16Bit = false, Bytes = new byte[] { 0xE9, 0x42, 0x12 }, ExpectedLength = 3, ExpectedOperand = "#$1242" },
                
                // native mode, 8-bit A, 16-bit X
                new { Emulation = false, A16Bit = false, X16Bit = true, Bytes = new byte[] { 0xE9, 0x42 }, ExpectedLength = 2, ExpectedOperand = "#$42" },
                
                // native mode, 16-bit A and X
                new { Emulation = false, A16Bit = true, X16Bit = true, Bytes = new byte[] { 0xE9, 0x42, 0x12 }, ExpectedLength = 3, ExpectedOperand = "#$1242" }
            };

            foreach (var testCase in testCases)
            {
                SetState(testCase.Emulation, testCase.A16Bit, testCase.X16Bit);
                var result = DecodeNext(testCase.Bytes);
                AssertInstruction(
                    result,
                    expectedBytes: testCase.Bytes,
                    mnemonic: "SBC",
                    bytesConsumed: testCase.ExpectedLength,
                    operandCount: 1,
                    operandText: [testCase.ExpectedOperand]
                );
            }
        }

        [TestMethod]
        public void Test65816_SBC_Absolute()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "SBC",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234"]
                ),
                [0xED, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_SBC_AbsoluteLong()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "SBC",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: ["$123456"]
                ),
                [0xEF, 0x56, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_SBC_DirectPage()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "SBC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42"]
                ),
                [0xE5, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_SBC_DirectPageIndirect()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "SBC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42)"]
                ),
                [0xF2, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_SBC_DirectPageIndirectLong()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "SBC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["[$42]"]
                ),
                [0xE7, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_SBC_AbsoluteIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "SBC",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234,X"]
                ),
                [0xFD, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_SBC_AbsoluteLongIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "SBC",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: ["$123456,X"]
                ),
                [0xFF, 0x56, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_SBC_AbsoluteIndexedY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "SBC",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234,Y"]
                ),
                [0xF9, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_SBC_DirectPageIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "SBC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42,X"]
                ),
                [0xF5, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_SBC_DirectPageIndirectX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "SBC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42,X)"]
                ),
                [0xE1, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_SBC_DirectPageIndirectIndexedY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "SBC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42),Y"]
                ),
                [0xF1, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_SBC_DirectPageIndirectLongIndexedY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "SBC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["[$42],Y"]
                ),
                [0xF7, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_SBC_StackRelative()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "SBC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42,S"]
                ),
                [0xE3, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_SBC_StackRelativeIndexedY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "SBC",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42,S),Y"]
                ),
                [0xF3, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_SEC()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "SEC",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0x38]
            );
        }

        [TestMethod]
        public void Test65816_SED()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "SED",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0xF8]
            );
        }

        [TestMethod]
        public void Test65816_SEI()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "SEI",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0x78]
            );
        }

        [TestMethod]
        public void Test65816_SEP()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "SEP",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["#$42"]
                ),
                [0xE2, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_STA_Absolute()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "STA",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234"]
                ),
                [0x8D, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_STA_AbsoluteLong()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "STA",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: ["$123456"]
                ),
                [0x8F, 0x56, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_STA_DirectPage()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "STA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42"]
                ),
                [0x85, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_STA_DirectPageIndirect()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "STA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42)"]
                ),
                [0x92, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_STA_DirectPageIndirectLong()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "STA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["[$42]"]
                ),
                [0x87, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_STA_AbsoluteIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "STA",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234,X"]
                ),
                [0x9D, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_STA_AbsoluteLongIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "STA",
                    bytesConsumed: 4,
                    operandCount: 1,
                    operandText: ["$123456,X"]
                ),
                [0x9F, 0x56, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_STA_AbsoluteIndexedY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "STA",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234,Y"]
                ),
                [0x99, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_STA_DirectPageIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "STA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42,X"]
                ),
                [0x95, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_STA_DirectPageIndirectX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "STA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42,X)"]
                ),
                [0x81, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_STA_DirectPageIndirectIndexedY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "STA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42),Y"]
                ),
                [0x91, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_STA_DirectPageIndirectLongIndexedY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "STA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["[$42],Y"]
                ),
                [0x97, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_STA_StackRelative()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "STA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42,S"]
                ),
                [0x83, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_STA_StackRelativeIndexedY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "STA",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["($42,S),Y"]
                ),
                [0x93, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_STP()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "STP",
                    bytesConsumed: 1,
                    operandCount: 0,
                    isTerminator: true
                ),
                [0xDB]
            );
        }

        [TestMethod]
        public void Test65816_STX_Absolute()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "STX",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234"]
                ),
                [0x8E, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_STX_DirectPage()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "STX",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42"]
                ),
                [0x86, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_STX_DirectPageIndexedY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "STX",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42,Y"]
                ),
                [0x96, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_STY_Absolute()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "STY",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234"]
                ),
                [0x8C, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_STY_DirectPage()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "STY",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42"]
                ),
                [0x84, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_STY_DirectPageIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "STY",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42,X"]
                ),
                [0x94, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_STZ_Absolute()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "STZ",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234"]
                ),
                [0x9C, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_STZ_DirectPage()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "STZ",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42"]
                ),
                [0x64, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_STZ_AbsoluteIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "STZ",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234,X"]
                ),
                [0x9E, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_STZ_DirectPageIndexedX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "STZ",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42,X"]
                ),
                [0x74, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_TAX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "TAX",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0xAA]
            );
        }

        [TestMethod]
        public void Test65816_TAY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "TAY",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0xA8]
            );
        }

        [TestMethod]
        public void Test65816_TCD()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "TCD",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0x5B]
            );
        }

        [TestMethod]
        public void Test65816_TCS()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "TCS",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0x1B]
            );
        }

        [TestMethod]
        public void Test65816_TDC()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "TDC",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0x7B]
            );
        }

        [TestMethod]
        public void Test65816_TSC()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "TSC",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0x3B]
            );
        }

        [TestMethod]
        public void Test65816_TSX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "TSX",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0xBA]
            );
        }

        [TestMethod]
        public void Test65816_TXA()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "TXA",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0x8A]
            );
        }

        [TestMethod]
        public void Test65816_TXS()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "TXS",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0x9A]
            );
        }

        [TestMethod]
        public void Test65816_TXY()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "TXY",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0x9B]
            );
        }

        [TestMethod]
        public void Test65816_TYA()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "TYA",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0x98]
            );
        }

        [TestMethod]
        public void Test65816_TYX()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "TYX",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0xBB]
            );
        }

        [TestMethod]
        public void Test65816_TRB_Absolute()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "TRB",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234"]
                ),
                [0x1C, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_TRB_DirectPage()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "TRB",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42"]
                ),
                [0x14, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_TSB_Absolute()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "TSB",
                    bytesConsumed: 3,
                    operandCount: 1,
                    operandText: ["$1234"]
                ),
                [0x0C, 0x34, 0x12]
            );
        }

        [TestMethod]
        public void Test65816_TSB_DirectPage()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "TSB",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["$42"]
                ),
                [0x04, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_WAI()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "WAI",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0xCB]
            );
        }

        [TestMethod]
        public void Test65816_WDM()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "WDM",
                    bytesConsumed: 2,
                    operandCount: 1,
                    operandText: ["#$42"]
                ),
                [0x42, 0x42]
            );
        }

        [TestMethod]
        public void Test65816_XBA()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "XBA",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0xEB]
            );
        }

        [TestMethod]
        public void Test65816_XCE()
        {
            TestInAllStates((bytes,result) => 
                AssertInstruction(
                    result,
                    expectedBytes: bytes,
                    mnemonic: "XCE",
                    bytesConsumed: 1,
                    operandCount: 0
                ),
                [0xFB]
            );
        }

        // Memory Access Tests
        [TestMethod]
        public void Test65816_MemoryAccesses_Absolute_A8()
        {
            SetState(emulation: false, a16bit: false, x16bit: false);

            var bytes = new byte[] { 0xAD, 0x34, 0x12 }; // LDA $1234
            var result = DecodeNext(bytes, 0x8000);
            Assert.IsTrue(result.Success);

            var registers = new SNES65816RegisterState
            {
                DBR = 0x7E,
                D = 0x0000,
                X = 0x0000,
                Y = 0x0000,
                S = 0x0000
            };

            var accesses = FetchMemoryAccessesWithDirection(result.Instruction, registers);
            Assert.AreEqual(1, accesses.Count);
            Assert.AreEqual(0x7E1234ul, accesses[0].address);
            Assert.AreEqual(1u, accesses[0].size);
            Assert.AreEqual(MemoryAccessDirection.Read, accesses[0].direction);
        }

        [TestMethod]
        public void Test65816_MemoryAccesses_DirectPage_WithD_A16()
        {
            SetState(emulation: false, a16bit: true, x16bit: false);

            var bytes = new byte[] { 0xA5, 0x20 }; // LDA $20
            var result = DecodeNext(bytes, 0x8000);
            Assert.IsTrue(result.Success);

            var registers = new SNES65816RegisterState
            {
                DBR = 0x00,
                D = 0x0100,
                X = 0x0000,
                Y = 0x0000,
                S = 0x0000
            };

            var accesses = FetchMemoryAccessesWithDirection(result.Instruction, registers);
            Assert.AreEqual(1, accesses.Count);
            Assert.AreEqual(0x0120ul, accesses[0].address);
            Assert.AreEqual(2u, accesses[0].size);
            Assert.AreEqual(MemoryAccessDirection.Read, accesses[0].direction);
        }

        [TestMethod]
        public void Test65816_MemoryAccesses_Immediate_None()
        {
            SetState(emulation: false, a16bit: false, x16bit: false);

            var bytes = new byte[] { 0xA9, 0x42 }; // LDA #$42
            var result = DecodeNext(bytes, 0x8000);
            Assert.IsTrue(result.Success);

            var registers = new SNES65816RegisterState
            {
                DBR = 0x7E,
                D = 0x0000,
                X = 0x0000,
                Y = 0x0000,
                S = 0x0000
            };

            var accesses = FetchMemoryAccessesWithDirection(result.Instruction, registers);
            Assert.AreEqual(0, accesses.Count);
        }

        [TestMethod]
        public void Test65816_MemoryAccesses_Branch_None()
        {
            SetState(emulation: false, a16bit: false, x16bit: false);

            var bytes = new byte[] { 0xD0, 0x10 }; // BNE +$10
            var result = DecodeNext(bytes, 0x8000);
            Assert.IsTrue(result.Success);

            var registers = new SNES65816RegisterState
            {
                DBR = 0x7E,
                D = 0x0000,
                X = 0x0000,
                Y = 0x0000,
                S = 0x0000
            };

            var accesses = FetchMemoryAccessesWithDirection(result.Instruction, registers);
            Assert.AreEqual(0, accesses.Count);
        }
    }
} 