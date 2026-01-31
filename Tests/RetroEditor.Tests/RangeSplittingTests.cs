
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

        [Fact]
        public void TestAddSingleLabel()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            romDataParser.LoadRomData([0x69, 0x42, 0x12, 0x69, 0x42, 0x12]);
            
            romDataParser.AddLabel(0, "StartOfCode");
            
            Assert.True(romDataParser.LabelProvider.HasLabel(0), "Label should exist at address 0.");
            var labels = romDataParser.LabelProvider.GetLabels(0);
            Assert.True(labels.Count == 1, "Should have 1 label at address 0.");
            Assert.True(labels[0] == "StartOfCode", "Label should be 'StartOfCode'.");
        }

        [Fact]
        public void TestAddMultipleLabelsToSameAddress()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            romDataParser.LoadRomData([0x69, 0x42, 0x12, 0x69, 0x42, 0x12]);
            
            romDataParser.AddLabel(0, "StartOfCode");
            romDataParser.AddLabel(0, "EntryPoint");
            romDataParser.AddLabel(0, "Reset");
            
            var labels = romDataParser.LabelProvider.GetLabels(0);
            Assert.True(labels.Count == 3, "Should have 3 labels at address 0.");
            Assert.True(labels.Contains("StartOfCode"), "Should contain 'StartOfCode'.");
            Assert.True(labels.Contains("EntryPoint"), "Should contain 'EntryPoint'.");
            Assert.True(labels.Contains("Reset"), "Should contain 'Reset'.");
        }

        [Fact]
        public void TestDuplicateLabelAtSameAddress()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            romDataParser.LoadRomData([0x69, 0x42, 0x12, 0x69, 0x42, 0x12]);
            
            romDataParser.AddLabel(0, "StartOfCode");
            romDataParser.AddLabel(0, "StartOfCode");
            
            var labels = romDataParser.LabelProvider.GetLabels(0);
            Assert.True(labels.Count == 1, "Should only have 1 label (duplicates should be ignored).");
            Assert.True(labels[0] == "StartOfCode", "Label should be 'StartOfCode'.");
        }

        [Fact]
        public void TestAddSymbol()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            
            romDataParser.AddSymbol(0x1000, 1, "GlobalCounter");
            
            Assert.True(romDataParser.SymbolProvider.HasSymbol(0x1000, 1), "Symbol should exist.");
            var symbol = romDataParser.SymbolProvider.GetSymbol(0x1000, 1);
            Assert.True(symbol == "GlobalCounter", "Symbol should be 'GlobalCounter'.");
        }

        [Fact]
        public void TestAddSymbolWithDifferentSizes()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            
            romDataParser.AddSymbol(0x1000, 1, "Byte");
            romDataParser.AddSymbol(0x1000, 2, "Word");
            romDataParser.AddSymbol(0x1000, 4, "Long");
            
            Assert.True(romDataParser.SymbolProvider.HasSymbol(0x1000, 1), "Byte symbol should exist.");
            Assert.True(romDataParser.SymbolProvider.HasSymbol(0x1000, 2), "Word symbol should exist.");
            Assert.True(romDataParser.SymbolProvider.HasSymbol(0x1000, 4), "Long symbol should exist.");
            
            Assert.True(romDataParser.SymbolProvider.GetSymbol(0x1000, 1) == "Byte", "Should be 'Byte'.");
            Assert.True(romDataParser.SymbolProvider.GetSymbol(0x1000, 2) == "Word", "Should be 'Word'.");
            Assert.True(romDataParser.SymbolProvider.GetSymbol(0x1000, 4) == "Long", "Should be 'Long'.");
        }

        [Fact]
        public void TestOverwriteSymbol()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            
            romDataParser.AddSymbol(0x1000, 2, "OldName");
            romDataParser.AddSymbol(0x1000, 2, "NewName");
            
            var symbol = romDataParser.SymbolProvider.GetSymbol(0x1000, 2);
            Assert.True(symbol == "NewName", "Symbol should be overwritten to 'NewName'.");
        }

        [Fact]
        public void TestAddSymbolWithLabel()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            romDataParser.LoadRomData([0x69, 0x42, 0x12, 0x69, 0x42, 0x12]);
            
            romDataParser.AddSymbolWithLabel(0x100, 2, "PlayerHealth");
            
            // Check that symbol was added
            Assert.True(romDataParser.SymbolProvider.HasSymbol(0x100, 2), "Symbol should exist.");
            Assert.True(romDataParser.SymbolProvider.GetSymbol(0x100, 2) == "PlayerHealth", "Symbol should be 'PlayerHealth'.");
            
            // Check that label was also added
            Assert.True(romDataParser.LabelProvider.HasLabel(0x100), "Label should exist.");
            var labels = romDataParser.LabelProvider.GetLabels(0x100);
            Assert.True(labels.Contains("PlayerHealth"), "Label should contain 'PlayerHealth'.");
        }

        [Fact]
        public void TestGetLabelsInRange()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            
            romDataParser.AddLabel(0x10, "Label1");
            romDataParser.AddLabel(0x20, "Label2");
            romDataParser.AddLabel(0x30, "Label3");
            romDataParser.AddLabel(0x40, "Label4");
            
            var labelsInRange = romDataParser.LabelProvider.GetLabelsInRange(0x15, 0x35);
            Assert.True(labelsInRange.Count == 2, "Should find 2 labels in range [0x15, 0x35].");
            Assert.True(labelsInRange[0].Address == 0x20, "First label should be at 0x20.");
            Assert.True(labelsInRange[1].Address == 0x30, "Second label should be at 0x30.");
        }

        [Fact]
        public void TestGetAllLabels()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            
            var regionKey = new MemoryRegionKey((uint)MemoryInformationRegion.ROM);
            
            romDataParser.AddLabel(0x10, "LabelA");
            romDataParser.AddLabel(0x20, "LabelB");
            romDataParser.AddLabel(0x20, "LabelC");
            
            var allLabels = romDataParser.LabelProvider.GetAllLabels(regionKey);
            Assert.True(allLabels.Count == 3, "Should have 3 total labels.");
        }

        [Fact]
        public void TestGetAllSymbols()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            
            var regionKey = new MemoryRegionKey((uint)MemoryInformationRegion.ROM);
            
            romDataParser.AddSymbol(0x100, 1, "SymbolA");
            romDataParser.AddSymbol(0x200, 2, "SymbolB");
            romDataParser.AddSymbol(0x300, 4, "SymbolC");
            
            var allSymbols = romDataParser.SymbolProvider.GetAllSymbols(regionKey);
            Assert.True(allSymbols.Count == 3, "Should have 3 total symbols.");
        }

        [Fact]
        public void TestLabelVersionIncrement()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            
            var initialVersion = romDataParser.LabelProvider.Version;
            
            romDataParser.AddLabel(0x10, "Label1");
            var versionAfterFirstAdd = romDataParser.LabelProvider.Version;
            Assert.True(versionAfterFirstAdd > initialVersion, "Version should increment after adding label.");
            
            romDataParser.AddLabel(0x20, "Label2");
            var versionAfterSecondAdd = romDataParser.LabelProvider.Version;
            Assert.True(versionAfterSecondAdd > versionAfterFirstAdd, "Version should increment again.");
        }

        [Fact]
        public void TestNoLabelAtAddress()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            
            Assert.False(romDataParser.LabelProvider.HasLabel(0x100), "Should not have label at 0x100.");
            var labels = romDataParser.LabelProvider.GetLabels(0x100);
            Assert.True(labels.Count == 0, "Should return empty list for address with no labels.");
        }

        [Fact]
        public void TestLabelMidCode()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            romDataParser.LoadRomData([0x69, 0x42, 0x12, 0x69, 0x42, 0x12]);
            var disassembler = new SNES65816Disassembler();
            var state = (SNES65816State)disassembler.State;
            state.SetEmulationMode(false);
            state.Accumulator8Bit = false;
            state.Index8Bit = false;
            disassembler.State = state;
            romDataParser.AddUnknownRange(0, 5);
            romDataParser.AddCodeRange(disassembler, 0, 5, new DummyMapper());
            
            romDataParser.AddLabel(2, "MidCodeLabel");
            
            Assert.True(romDataParser.LabelProvider.HasLabel(2), "Label should exist at address 2.");
            var labels = romDataParser.LabelProvider.GetLabels(2);
            Assert.True(labels.Count == 1, "Should have 1 label at address 2.");
            Assert.True(labels[0] == "MidCodeLabel", "Label should be 'MidCodeLabel'.");
        }

    }
}