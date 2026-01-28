
using System;
using System.Collections.Generic;
using System.Linq;
using RetroEditor.Source.Internals.ReverseEngineering.Platform;
using RetroEditor.Source.Internals.ReverseEngineering;
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

            public ulong MapCpuToRegion(ulong cpuAddress, out MemoryRegionKey region)
            {
                region = new MemoryRegionKey((uint)MemoryInformationRegion.ROM);
                return cpuAddress;
            }

            public ulong MapCpuToHardwareAddress(ulong address, out MemoryRegionKey region)
            {
                region = new MemoryRegionKey((uint)MemoryInformationRegion.ROM);
                return address;
            }
        }

        class DummyMemoryInformation : IMemoryInformation
        {
            public string MameViewName { get; }
            public string DisplayName { get; }
            public MemoryRegionKey RegionKey { get; }
            public bool HasPhysicalData => true;
            public (UInt64 Start, UInt64 End) AddressRange { get; }

            public DummyMemoryInformation(string name, MemoryRegionKey regionKey, string displayName = null)
            {
                MameViewName = name;
                DisplayName = displayName ?? name;
                AddressRange = (0, UInt64.MaxValue);
                RegionKey = regionKey;
            }

            public IMemoryRegionDataProvider CreateDataProvider()
            {
                return new BufferDataProvider(MameViewName);
            }
        }

        class DummyMemoryInformationProvider : IMemoryInformationProvider
        {
            public IEnumerable<IMemoryInformation> GetMemoryRegions()
            {
                yield return new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
                yield return new DummyMemoryInformation("RAM", new MemoryRegionKey((uint)MemoryInformationRegion.RAM));
            }
        }

        [Fact]
        public void TestCodeSplit()
        {
            var mapper = new DummyMapper();
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, (IMemoryRegionDataProvider)dataProvider);
            romDataParser.LoadRomData([0x69, 0x42, 0x12, 0x69, 0x42, 0x12, 0x69, 0x42, 0x12, 0x69, 0x42, 0x12]);
            var disassembler = new SNES65816Disassembler();
            var state = (SNES65816State)disassembler.State;
            state.SetEmulationMode(false);
            state.Accumulator8Bit = false;
            state.Index8Bit = false;
            disassembler.State = state;
            romDataParser.AddUnknownRange(0, 11);
            romDataParser.AddCodeRange(disassembler, 0, 2, mapper);
            romDataParser.AddCodeRange(disassembler, 3, 5, mapper);
            romDataParser.AddCodeRange(disassembler, 6, 8, mapper);
            romDataParser.AddCodeRange(disassembler, 9, 11, mapper);
            
            var ranges = romDataParser.GetRanges();
            Assert.True(ranges.Count == 1, "Code range count should be 1.");
        }

        [Fact]
        public void VerifyDataNextAddress()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            romDataParser.LoadRomData(
                [0x69, 0x42, 0x12, 0x69, 
                 0x42, 0x12, 0x69, 0x42, 
                 0x12, 0x69, 0x42, 0x12, 
                 0x11, 0x22, 0x33, 0x44]);
            romDataParser.AddUnknownRange(0, (ulong)romDataParser.GetRomData.Length-1);
            romDataParser.AddDataRange(0, 3, 4);
            
            var ranges = romDataParser.GetRanges();
            Assert.True(ranges.Count == 2, "Range count should be 2.");
            var rangesList = ranges.ToList();
            Assert.True(rangesList[1].Value.AddressStart == 4, "Next address after data range should be 4.");

            romDataParser.AddDataRange(4, 7, 4);
            Assert.True(ranges.Count == 2, "Range count should be 2.");
            rangesList = ranges.ToList();
            Assert.True(rangesList[1].Value.AddressStart == 8, "Next address after data range should be 8.");

            romDataParser.AddDataRange(8, 11, 4);
            Assert.True(ranges.Count == 2, "Range count should be 2.");
            rangesList = ranges.ToList();
            Assert.True(rangesList[1].Value.AddressStart == 12, "Next address after data range should be 12.");
        }
    }
}