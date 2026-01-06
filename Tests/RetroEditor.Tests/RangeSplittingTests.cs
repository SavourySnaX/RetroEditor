
using System;
using System.Linq;
using Xunit;

namespace RetroEditor.Tests
{

    public class RangeSplittingTests
    {
        [Fact]
        public void TestCodeSplit()
        {
            var romDataParser = new RomDataParser();
            romDataParser.LoadRomData([0x69, 0x42, 0x12, 0x69, 0x42, 0x12, 0x69, 0x42, 0x12, 0x69, 0x42, 0x12]);
            var disassembler = new SNES65816Disassembler();
            var state = (SNES65816State)disassembler.State;
            state.SetEmulationMode(false);
            state.Accumulator8Bit = false;
            state.Index8Bit = false;
            disassembler.State = state;
            romDataParser.AddUnknownRange(RomDataParser.RangeRegion.Cartridge, 0, 11);
            romDataParser.AddCodeRange(disassembler, 0, 2);
            romDataParser.AddCodeRange(disassembler, 3, 5);
            romDataParser.AddCodeRange(disassembler, 6, 8);
            romDataParser.AddCodeRange(disassembler, 9, 11);
            
            Assert.True(romDataParser.GetRomRanges.Count == 1, "Code range count should be 1.");
        }
    }
}