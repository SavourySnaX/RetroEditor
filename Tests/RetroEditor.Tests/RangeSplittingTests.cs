
using System;
using System.Collections.Generic;
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

        class DummyMemoryInformation : IMemoryInformation
        {
            public string MameViewName { get; }
            public string DisplayName { get; }

            public DummyMemoryInformation(string name, string displayName = null)
            {
                MameViewName = name;
                DisplayName = displayName ?? name;
            }
        }

        class DummyMemoryInformationProvider : IMemoryInformationProvider
        {
            public IEnumerable<IMemoryInformation> GetMemoryRegions()
            {
                yield return new DummyMemoryInformation("Cartridge");
                yield return new DummyMemoryInformation("RAM");
            }
        }

        [Fact]
        public void TestCodeSplit()
        {
            var mapper = new DummyMapper();
            var romDataParser = new RomDataParser(new DummyMemoryInformationProvider());
            romDataParser.LoadRomData([0x69, 0x42, 0x12, 0x69, 0x42, 0x12, 0x69, 0x42, 0x12, 0x69, 0x42, 0x12]);
            var disassembler = new SNES65816Disassembler();
            var state = (SNES65816State)disassembler.State;
            state.SetEmulationMode(false);
            state.Accumulator8Bit = false;
            state.Index8Bit = false;
            disassembler.State = state;
            romDataParser.AddUnknownRange("Cartridge", 0, 11);
            romDataParser.AddCodeRange(disassembler, 0, 2, mapper);
            romDataParser.AddCodeRange(disassembler, 3, 5, mapper);
            romDataParser.AddCodeRange(disassembler, 6, 8, mapper);
            romDataParser.AddCodeRange(disassembler, 9, 11, mapper);
            
            var ranges = romDataParser.GetRangeCollection("Cartridge");
            Assert.True(ranges.Count == 1, "Code range count should be 1.");
        }

        [Fact]
        public void VerifyDataNextAddress()
        {
            var romDataParser = new RomDataParser(new DummyMemoryInformationProvider());
            romDataParser.LoadRomData(
                [0x69, 0x42, 0x12, 0x69, 
                 0x42, 0x12, 0x69, 0x42, 
                 0x12, 0x69, 0x42, 0x12, 
                 0x11, 0x22, 0x33, 0x44]);
            romDataParser.AddUnknownRange("Cartridge", 0, (ulong)romDataParser.GetRomData.Length-1);
            romDataParser.AddDataRange("Cartridge", 0, 3, 4);
            
            var ranges = romDataParser.GetRangeCollection("Cartridge");
            Assert.True(ranges.Count == 2, "Range count should be 2.");
            var rangesList = ranges.ToList();
            Assert.True(rangesList[1].Value.AddressStart == 4, "Next address after data range should be 4.");

            romDataParser.AddDataRange("Cartridge", 4, 7, 4);
            Assert.True(ranges.Count == 2, "Range count should be 2.");
            rangesList = ranges.ToList();
            Assert.True(rangesList[1].Value.AddressStart == 8, "Next address after data range should be 8.");

            romDataParser.AddDataRange("Cartridge", 8, 11, 4);
            Assert.True(ranges.Count == 2, "Range count should be 2.");
            rangesList = ranges.ToList();
            Assert.True(rangesList[1].Value.AddressStart == 12, "Next address after data range should be 12.");
        }
    }
}