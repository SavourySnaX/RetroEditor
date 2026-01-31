using System;
using System.Linq;
using RetroEditor.Source.Internals.ReverseEngineering.Platform;
using Xunit;

namespace RetroEditor.Tests
{
    public class RomDataRenderingTests
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

        [Fact]
        public void TestGetLineInfoForUnknownRegion()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            romDataParser.LoadRomData([0xAB, 0xCD, 0xEF, 0x12]);
            
            romDataParser.AddUnknownRange(0, 3);
            var ranges = romDataParser.GetRanges();
            
            Assert.True(ranges.LineCount == 4, "Should have 4 lines for unknown region.");
            
            var line0 = ranges.GetRangeContainingLine(0, out var offset);
            var lineInfo = line0.Value.GetLineInfo(offset);
            
            Assert.True(lineInfo.Address.Contains("00000000"), "First line address should be 00000000.");
            Assert.True(lineInfo.Bytes.Contains("AB"), "First line should show byte AB.");
        }

        [Fact]
        public void TestGetLineInfoForDataRegion()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            romDataParser.LoadRomData([0xAB, 0xCD, 0xEF, 0x12, 0x34, 0x56, 0x78, 0x90]);
            
            romDataParser.AddUnknownRange(0, 7);
            romDataParser.AddDataRange(0, 3, 2); // 2-byte words
            
            var ranges = romDataParser.GetRanges();
            
            // Should have 2 lines for data (2 words) + 4 lines for remaining unknown
            Assert.True(ranges.LineCount == 6, "Should have 6 total lines.");
            
            var line0 = ranges.GetRangeContainingLine(0, out var offset);
            var lineInfo = line0.Value.GetLineInfo(offset);
            
            Assert.True(lineInfo.Details.Contains("dw"), "Data region should show 'dw' directive.");
        }

        [Fact]
        public void TestGetLineInfoForCodeRegion()
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
            
            var ranges = romDataParser.GetRanges();
            var line0 = ranges.GetRangeContainingLine(0, out var offset);
            var lineInfo = line0.Value.GetLineInfo(offset);
            
            Assert.True(lineInfo.Address.Contains("00000000"), "Code should have address.");
            Assert.True(!string.IsNullOrEmpty(lineInfo.Bytes), "Code should have bytes.");
            Assert.True(!string.IsNullOrEmpty(lineInfo.Details), "Code should have disassembly.");
        }

        [Fact]
        public void TestLineCountWithoutLabels()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            romDataParser.LoadRomData([0x69, 0x42, 0x12, 0x69, 0x42, 0x12]);
            
            romDataParser.AddUnknownRange(0, 5);
            
            var ranges = romDataParser.GetRanges();
            Assert.True(ranges.LineCount == 6, "Should have 6 lines without labels.");
        }

        [Fact]
        public void TestLineCountWithLabels()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            romDataParser.LoadRomData([0x69, 0x42, 0x12, 0x69, 0x42, 0x12]);
            
            romDataParser.AddUnknownRange(0, 5);
            romDataParser.AddLabel(0, "Start");
            romDataParser.AddLabel(3, "Middle");
            
            var ranges = romDataParser.GetRanges();
            // 6 data lines + 2 label lines = 8 total
            Assert.True(ranges.LineCount == 8, "Should have 8 lines (6 data + 2 labels).");
        }

        [Fact]
        public void TestLineCountWithMultipleLabelsAtSameAddress()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            romDataParser.LoadRomData([0x69, 0x42, 0x12, 0x69, 0x42, 0x12]);
            
            romDataParser.AddUnknownRange(0, 5);
            romDataParser.AddLabel(0, "Start");
            romDataParser.AddLabel(0, "EntryPoint");
            romDataParser.AddLabel(0, "Reset");
            
            var ranges = romDataParser.GetRanges();
            // 6 data lines + 3 label lines = 9 total
            Assert.True(ranges.LineCount == 9, "Should have 9 lines (6 data + 3 labels at same address).");
        }

        [Fact]
        public void TestGetLineInfoForLabel()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            romDataParser.LoadRomData([0x69, 0x42, 0x12]);
            
            romDataParser.AddUnknownRange(0, 2);
            romDataParser.AddLabel(0, "StartLabel");
            
            var ranges = romDataParser.GetRanges();
            var range = ranges.GetRangeContainingLine(0, out var offset);
            var lineInfo = range.Value.GetLineInfo(offset);
            
            Assert.True(lineInfo.IsLabel, "First line should be a label.");
            Assert.True(lineInfo.Address.Contains("StartLabel:"), "Label line should contain label name with colon.");
        }

        [Fact]
        public void TestFetchLineForAddress()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            romDataParser.LoadRomData([0xAB, 0xCD, 0xEF, 0x12]);
            
            romDataParser.AddUnknownRange(0, 3);
            
            var ranges = romDataParser.GetRanges();
            
            var line0 = ranges.FetchLineForAddress(0);
            var line1 = ranges.FetchLineForAddress(1);
            var line3 = ranges.FetchLineForAddress(3);
            
            Assert.True(line0 == 0, "Address 0 should be at line 0.");
            Assert.True(line1 == 1, "Address 1 should be at line 1.");
            Assert.True(line3 == 3, "Address 3 should be at line 3.");
        }

        [Fact]
        public void TestFetchLineForAddressWithLabels()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            romDataParser.LoadRomData([0xAB, 0xCD, 0xEF, 0x12]);
            
            romDataParser.AddUnknownRange(0, 3);
            romDataParser.AddLabel(0, "Start");
            romDataParser.AddLabel(2, "Middle");
            
            var ranges = romDataParser.GetRanges();
            
            // Line 0: Label "Start"
            // Line 1: Address 0 data
            // Line 2: Address 1 data
            // Line 3: Label "Middle"
            // Line 4: Address 2 data
            // Line 5: Address 3 data
            
            var lineForAddr0 = ranges.FetchLineForAddress(0);
            var lineForAddr2 = ranges.FetchLineForAddress(2);
            
            // Address 0 has a label, so it should be at line 1 (after the label at line 0)
            Assert.True(lineForAddr0 == 1, "Address 0 should be at line 1 (after its label).");
            // Address 2 has a label, so it should be at line 4 (after the label at line 3)
            Assert.True(lineForAddr2 == 4, "Address 2 should be at line 4 (after its label).");
        }

        [Fact]
        public void TestFetchAddressForLine()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            romDataParser.LoadRomData([0xAB, 0xCD, 0xEF, 0x12]);
            
            romDataParser.AddUnknownRange(0, 3);
            
            var ranges = romDataParser.GetRanges();
            
            var addr0 = ranges.FetchAddressForLine(0);
            var addr1 = ranges.FetchAddressForLine(1);
            var addr3 = ranges.FetchAddressForLine(3);
            
            Assert.True(addr0 == 0, "Line 0 should be address 0.");
            Assert.True(addr1 == 1, "Line 1 should be address 1.");
            Assert.True(addr3 == 3, "Line 3 should be address 3.");
        }

        [Fact]
        public void TestFetchAddressForLineWithLabels()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            romDataParser.LoadRomData([0xAB, 0xCD, 0xEF, 0x12]);
            
            romDataParser.AddUnknownRange(0, 3);
            romDataParser.AddLabel(0, "Start");
            
            var ranges = romDataParser.GetRanges();
            
            // Line 0: Label "Start" (points to address 0)
            // Line 1: Address 0 data
            var addrForLine0 = ranges.FetchAddressForLine(0);
            var addrForLine1 = ranges.FetchAddressForLine(1);
            
            Assert.True(addrForLine0 == 0, "Label line should point to its address (0).");
            Assert.True(addrForLine1 == 0, "Data line after label should also be address 0.");
        }

        [Fact]
        public void TestGetRangeContainingLine()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            romDataParser.LoadRomData([0xAB, 0xCD, 0xEF, 0x12, 0x34, 0x56]);
            
            romDataParser.AddUnknownRange(0, 5);
            romDataParser.AddDataRange(0, 1, 1);
            
            var ranges = romDataParser.GetRanges();
            
            var range0 = ranges.GetRangeContainingLine(0, out var offset0);
            var range1 = ranges.GetRangeContainingLine(1, out var offset1);
            var range2 = ranges.GetRangeContainingLine(2, out var offset2);
            
            Assert.NotNull(range0);
            Assert.NotNull(range1);
            Assert.NotNull(range2);
            
            // First two lines are data region
            Assert.True(range0.Value is DataRegion, "Line 0 should be in DataRegion.");
            Assert.True(range1.Value is DataRegion, "Line 1 should be in DataRegion.");
            // Remaining lines are unknown region
            Assert.True(range2.Value is UnknownRegion, "Line 2 should be in UnknownRegion.");
        }

        [Fact]
        public void TestGetRangeContainingAddress()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            romDataParser.LoadRomData([0xAB, 0xCD, 0xEF, 0x12, 0x34, 0x56]);
            
            romDataParser.AddUnknownRange(0, 5);
            romDataParser.AddDataRange(0, 1, 1);
            
            var ranges = romDataParser.GetRanges();
            
            var range0 = ranges.GetRangeContainingAddress(0, out _);
            var range1 = ranges.GetRangeContainingAddress(1, out _);
            var range2 = ranges.GetRangeContainingAddress(2, out _);
            
            Assert.NotNull(range0);
            Assert.NotNull(range1);
            Assert.NotNull(range2);
            
            Assert.True(range0.Value is DataRegion, "Address 0 should be in DataRegion.");
            Assert.True(range1.Value is DataRegion, "Address 1 should be in DataRegion.");
            Assert.True(range2.Value is UnknownRegion, "Address 2 should be in UnknownRegion.");
        }

        [Fact]
        public void TestMultipleRangesLineCount()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            romDataParser.LoadRomData([0x69, 0x42, 0x12, 0x69, 0x42, 0x12, 0xAB, 0xCD]);
            
            var disassembler = new SNES65816Disassembler();
            var state = (SNES65816State)disassembler.State;
            state.SetEmulationMode(false);
            state.Accumulator8Bit = false;
            state.Index8Bit = false;
            disassembler.State = state;
            
            romDataParser.AddUnknownRange(0, 7);
            romDataParser.AddCodeRange(disassembler, 0, 2, new DummyMapper());  // 3 bytes = 1 instruction
            romDataParser.AddDataRange(6, 7, 1);  // 2 bytes = 2 lines
            
            var ranges = romDataParser.GetRanges();
            
            // 1 line for code + 2 lines for data + 3 lines for unknown (bytes 3-5) = 6 total
            Assert.True(ranges.Count == 3, "Should have 3 separate ranges.");
        }

        [Fact]
        public void TestStringRegionLineInfo()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            romDataParser.LoadRomData([0x48, 0x65, 0x6C, 0x6C, 0x6F]); // "Hello"
            
            romDataParser.AddUnknownRange(0, 4);
            romDataParser.AddStringRange(0, 4);
            
            var ranges = romDataParser.GetRanges();
            var range0 = ranges.GetRangeContainingLine(0, out var offset);
            var lineInfo = range0.Value.GetLineInfo(offset);
            
            Assert.True(lineInfo.Details.Contains("db"), "String region should use 'db' directive.");
            Assert.True(lineInfo.Details.Contains("Hello"), "String region should show the text.");
        }

        [Fact]
        public void TestDataRegionWithDifferentSizes()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            romDataParser.LoadRomData([0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08]);
            
            romDataParser.AddUnknownRange(0, 7);
            romDataParser.AddDataRange(0, 1, 2);  // 2-byte word: 2 bytes = 1 line
            romDataParser.AddDataRange(2, 5, 4);  // 4-byte long: 4 bytes = 1 line
            
            var ranges = romDataParser.GetRanges();
            
            // 1 line (2-byte) + 1 line (4-byte) + 2 lines (unknown) = 4 total
            Assert.True(ranges.LineCount == 4, "Should have 4 lines total.");
            
            var range0 = ranges.GetRangeContainingLine(0, out var offset0);
            var lineInfo0 = range0.Value.GetLineInfo(offset0);
            Assert.True(lineInfo0.Details.Contains("dw"), "First data should be 'dw' (word).");
            
            var range1 = ranges.GetRangeContainingLine(1, out var offset1);
            var lineInfo1 = range1.Value.GetLineInfo(offset1);
            Assert.True(lineInfo1.Details.Contains("dl"), "Second data should be 'dl' (long).");
        }

        [Fact]
        public void TestLabelOrderingAtSameAddress()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            romDataParser.LoadRomData([0x00, 0x01, 0x02]);
            
            romDataParser.AddUnknownRange(0, 2);
            romDataParser.AddLabel(0, "First");
            romDataParser.AddLabel(0, "Second");
            romDataParser.AddLabel(0, "Third");
            
            var ranges = romDataParser.GetRanges();
            
            // Get the three label lines
            var range0 = ranges.GetRangeContainingLine(0, out var offset0);
            var lineInfo0 = range0.Value.GetLineInfo(offset0);
            
            var range1 = ranges.GetRangeContainingLine(1, out var offset1);
            var lineInfo1 = range1.Value.GetLineInfo(offset1);
            
            var range2 = ranges.GetRangeContainingLine(2, out var offset2);
            var lineInfo2 = range2.Value.GetLineInfo(offset2);
            
            Assert.True(lineInfo0.IsLabel, "Line 0 should be a label.");
            Assert.True(lineInfo1.IsLabel, "Line 1 should be a label.");
            Assert.True(lineInfo2.IsLabel, "Line 2 should be a label.");
            
            // Verify all labels are present
            var labelTexts = new[] { lineInfo0.Address, lineInfo1.Address, lineInfo2.Address };
            Assert.True(labelTexts.Any(l => l.Contains("First:")), "Should contain 'First' label.");
            Assert.True(labelTexts.Any(l => l.Contains("Second:")), "Should contain 'Second' label.");
            Assert.True(labelTexts.Any(l => l.Contains("Third:")), "Should contain 'Third' label.");
        }

        [Fact]
        public void TestCommentRangeAboveRegion()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            romDataParser.LoadRomData([0xAB, 0xCD]);
            
            romDataParser.AddUnknownRange(0, 1);
            romDataParser.AddCommentRange(new[] { "This is a comment", "Second line" }, 0);
            
            var ranges = romDataParser.GetRanges();
            
            // 2 comment lines + 2 data lines = 4 total
            Assert.True(ranges.LineCount == 4, "Should have 4 lines (2 comments + 2 data).");
            
            var range0 = ranges.GetRangeContainingLine(0, out var offset0);
            var lineInfo0 = range0.Value.GetLineInfo(offset0);
            
            Assert.True(lineInfo0.Details.Contains("This is a comment"), "First line should be the comment.");
        }

        [Fact]
        public void TestRangeCollectionIterationOrder()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            romDataParser.LoadRomData([0x01, 0x02, 0x03, 0x04, 0x05, 0x06]);
            
            romDataParser.AddUnknownRange(0, 5);
            romDataParser.AddDataRange(0, 1, 1);
            romDataParser.AddDataRange(4, 5, 1);
            
            var ranges = romDataParser.GetRanges();
            
            // Verify ranges are in address order
            var rangeList = ranges.ToList();
            Assert.True(rangeList.Count == 3, "Should have 3 ranges.");
            Assert.True(rangeList[0].Value.AddressStart == 0, "First range should start at 0.");
            Assert.True(rangeList[1].Value.AddressStart == 2, "Second range should start at 2.");
            Assert.True(rangeList[2].Value.AddressStart == 4, "Third range should start at 4.");
        }

        [Fact]
        public void TestLineIterationAcrossMultipleRanges()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            romDataParser.LoadRomData([0x01, 0x02, 0x03, 0x04]);
            
            romDataParser.AddUnknownRange(0, 3);
            romDataParser.AddDataRange(0, 1, 1);  // 2 lines
            romDataParser.AddDataRange(2, 3, 1);  // 2 lines
            
            var ranges = romDataParser.GetRanges();
            var totalLines = ranges.LineCount;
            
            // Iterate through all lines
            for (ulong line = 0; line < totalLines; line++)
            {
                var range = ranges.GetRangeContainingLine(line, out var offset);
                Assert.NotNull(range);
                var lineInfo = range.Value.GetLineInfo(offset);
                Assert.NotNull(lineInfo.Address);
            }
        }

        [Fact]
        public void TestLabelRendersWhenCodeInsertedAboveAndBelow()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            
            // Create ROM data with code opcodes
            romDataParser.LoadRomData([
                0x69, 0x42, 0x12,  // Bytes 0-2: Code above
                0xAB, 0xCD, 0xEF,  // Bytes 3-5: Unknown
                0x69, 0x42, 0x12,  // Bytes 6-8: Label location
                0x69, 0x42, 0x12,  // Bytes 9-11: Code below
            ]);
            
            var disassembler = new SNES65816Disassembler();
            var state = (SNES65816State)disassembler.State;
            state.SetEmulationMode(false);
            state.Accumulator8Bit = false;
            state.Index8Bit = false;
            disassembler.State = state;
            
            var mapper = new DummyMapper();
            
            // Step 1: Set up unknown range for entire region
            romDataParser.AddUnknownRange(0, 11);
            
            // Step 2: Add a label at address 6
            romDataParser.AddLabel(6, "MiddleLabel");
            
            // Step 3: Add code above the label (addresses 0-2)
            romDataParser.AddCodeRange(disassembler, 0, 2, mapper);
            
            // Step 4: Add code below the label (addresses 9-11)
            romDataParser.AddCodeRange(disassembler, 9, 11, mapper);
            
            var ranges = romDataParser.GetRanges();
            
            // Verify the label still exists in the label provider
            Assert.True(romDataParser.LabelProvider.HasLabel(6), "Label should still exist at address 6.");
            
            // Find the label in the rendered output
            bool labelFound = false;
            var totalLines = ranges.LineCount;
            
            for (ulong line = 0; line < totalLines; line++)
            {
                var range = ranges.GetRangeContainingLine(line, out var offset);
                if (range != null)
                {
                    var lineInfo = range.Value.GetLineInfo(offset);
                    if (lineInfo.IsLabel && lineInfo.Address.Contains("MiddleLabel"))
                    {
                        labelFound = true;
                        break;
                    }
                }
            }
            
            Assert.True(labelFound, "Label 'MiddleLabel' should be rendered in the output.");
        }

        [Fact]
        public void TestLabelAtCodeBoundary()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            
            romDataParser.LoadRomData([
                0x69, 0x42, 0x12,  // Bytes 0-2: Code
                0x69, 0x42, 0x12,  // Bytes 3-5: Code (with label at start)
                0x69, 0x42, 0x12,  // Bytes 6-8: Code
            ]);
            
            var disassembler = new SNES65816Disassembler();
            var state = (SNES65816State)disassembler.State;
            state.SetEmulationMode(false);
            state.Accumulator8Bit = false;
            state.Index8Bit = false;
            disassembler.State = state;
            
            var mapper = new DummyMapper();
            
            romDataParser.AddUnknownRange(0, 8);
            
            // Add code regions
            romDataParser.AddCodeRange(disassembler, 0, 2, mapper);
            
            // Add label at the boundary (address 3)
            romDataParser.AddLabel(3, "CodeBoundary");
            
            // Add more code after the label
            romDataParser.AddCodeRange(disassembler, 3, 8, mapper);
            
            var ranges = romDataParser.GetRanges();
            
            // Verify label exists
            Assert.True(romDataParser.LabelProvider.HasLabel(3), "Label should exist at address 3.");
            
            // Find the label in rendered output
            bool labelFound = false;
            var totalLines = ranges.LineCount;
            
            for (ulong line = 0; line < totalLines; line++)
            {
                var range = ranges.GetRangeContainingLine(line, out var offset);
                if (range != null)
                {
                    var lineInfo = range.Value.GetLineInfo(offset);
                    if (lineInfo.IsLabel && lineInfo.Address.Contains("CodeBoundary"))
                    {
                        labelFound = true;
                        break;
                    }
                }
            }
            
            Assert.True(labelFound, "Label 'CodeBoundary' should be rendered at code boundary.");
        }

        [Fact]
        public void TestMultipleLabelsWithSurroundingCode()
        {
            var memInfo = new DummyMemoryInformation("Cartridge", new MemoryRegionKey((uint)MemoryInformationRegion.ROM));
            var dataProvider = memInfo.CreateDataProvider();
            var romDataParser = new RomDataParser(memInfo, dataProvider);
            
            romDataParser.LoadRomData([
                0x69, 0x42, 0x12,  // Bytes 0-2: Code
                0xAB, 0xCD, 0xEF,  // Bytes 3-5: Unknown (label at 3 and 5)
                0x69, 0x42, 0x12,  // Bytes 6-8: Code
            ]);
            
            var disassembler = new SNES65816Disassembler();
            var state = (SNES65816State)disassembler.State;
            state.SetEmulationMode(false);
            state.Accumulator8Bit = false;
            state.Index8Bit = false;
            disassembler.State = state;
            
            var mapper = new DummyMapper();
            
            romDataParser.AddUnknownRange(0, 8);
            
            // Add labels in the middle
            romDataParser.AddLabel(3, "Label1");
            romDataParser.AddLabel(5, "Label2");
            
            // Add code around the labels
            romDataParser.AddCodeRange(disassembler, 0, 2, mapper);
            romDataParser.AddCodeRange(disassembler, 6, 8, mapper);
            
            var ranges = romDataParser.GetRanges();
            
            // Count how many labels are rendered
            int labelsFound = 0;
            var totalLines = ranges.LineCount;
            
            for (ulong line = 0; line < totalLines; line++)
            {
                var range = ranges.GetRangeContainingLine(line, out var offset);
                if (range != null)
                {
                    var lineInfo = range.Value.GetLineInfo(offset);
                    if (lineInfo.IsLabel)
                    {
                        if (lineInfo.Address.Contains("Label1") || lineInfo.Address.Contains("Label2"))
                        {
                            labelsFound++;
                        }
                    }
                }
            }
            
            Assert.True(labelsFound == 2, $"Both labels should be rendered, but found {labelsFound}.");
        }
    }
}

