
using System;
using System.Linq;
using RetroEditor.Source.Internals.ReverseEngineering.Platform;
using Xunit;

namespace RetroEditor.Tests
{

    public class RangeSplittingTests
    {
        class DummyMapper : IMemoryMapper
        {
            public UInt64 MapRomToCpu(UInt64 romAddress)
            {
                return romAddress;
            }

            public UInt64 MapHardwareAddressToCpu(UInt64 linearAddress)
            {
                return linearAddress;
            }

            public ulong MapCpuToRegion(ulong cpuAddress, out Source.Internals.ReverseEngineering.Platform.MemoryRegion region)
            {
                region = Source.Internals.ReverseEngineering.Platform.MemoryRegion.ROM;
                return cpuAddress;
            }

            public ulong MapCpuToHardwareAddress(ulong address, out Source.Internals.ReverseEngineering.Platform.MemoryRegion region)
            {
                region = Source.Internals.ReverseEngineering.Platform.MemoryRegion.ROM;
                return address;
            }

        }

        [Fact]
        public void TestCodeSplit()
        {
            var mapper = new DummyMapper();
            var romDataParser = new RomDataParser();
            romDataParser.LoadRomData([0x69, 0x42, 0x12, 0x69, 0x42, 0x12, 0x69, 0x42, 0x12, 0x69, 0x42, 0x12]);
            var disassembler = new SNES65816Disassembler();
            var state = (SNES65816State)disassembler.State;
            state.SetEmulationMode(false);
            state.Accumulator8Bit = false;
            state.Index8Bit = false;
            disassembler.State = state;
            romDataParser.AddUnknownRange(RomDataParser.RangeRegion.Cartridge, 0, 11);
            romDataParser.AddCodeRange(disassembler, 0, 2, mapper);
            romDataParser.AddCodeRange(disassembler, 3, 5, mapper);
            romDataParser.AddCodeRange(disassembler, 6, 8, mapper);
            romDataParser.AddCodeRange(disassembler, 9, 11, mapper);
            
            Assert.True(romDataParser.GetRomRanges.Count == 1, "Code range count should be 1.");
        }

        [Fact]
        public void VerifyDataNextAddress()
        {
            var romDataParser = new RomDataParser();
            romDataParser.LoadRomData(
                [0x69, 0x42, 0x12, 0x69, 
                 0x42, 0x12, 0x69, 0x42, 
                 0x12, 0x69, 0x42, 0x12, 
                 0x11, 0x22, 0x33, 0x44]);
            romDataParser.AddUnknownRange(RomDataParser.RangeRegion.Cartridge, 0, (ulong)romDataParser.GetRomData.Length-1);
            romDataParser.AddDataRange(RomDataParser.RangeRegion.Cartridge, 0, 3, 4);
            
            Assert.True(romDataParser.GetRomRanges.Count == 2, "Range count should be 2.");
            var ranges = romDataParser.GetRomRanges.ToList();
            Assert.True(ranges[1].Value.AddressStart == 4, "Next address after data range should be 4.");

            romDataParser.AddDataRange(RomDataParser.RangeRegion.Cartridge, 4, 7, 4);
            Assert.True(romDataParser.GetRomRanges.Count == 2, "Range count should be 2.");
            ranges = romDataParser.GetRomRanges.ToList();
            Assert.True(ranges[1].Value.AddressStart == 8, "Next address after data range should be 8.");

            romDataParser.AddDataRange(RomDataParser.RangeRegion.Cartridge, 8, 11, 4);
            Assert.True(romDataParser.GetRomRanges.Count == 2, "Range count should be 2.");
            ranges = romDataParser.GetRomRanges.ToList();
            Assert.True(ranges[1].Value.AddressStart == 12, "Next address after data range should be 12.");
        }
    }
}