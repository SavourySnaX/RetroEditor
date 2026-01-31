namespace RetroEditor.Source.Internals.ReverseEngineering.Platform.SNES;

/// <summary>
/// SNES hardware register provider
/// </summary>
internal class SNESHardwareRegisterProvider : IHardwareSymbolsProvider
{
    public void InitializeSymbols(IRomDataParser romData, MemoryRegionKey regionKey)
    {
        // Only add hardware register symbols to the IO region
        if (regionKey != SNESMemoryInformationProvider.IOKey)
            return;
            
        // PPU Registers
        romData.AddSymbol(0x2100, 2, "INIDISP");
        romData.AddSymbol(0x2101, 2, "OBSEL");
        romData.AddSymbol(0x2102, 2, "OAMADDL");
        romData.AddSymbol(0x2103, 2, "OAMADDH");
        romData.AddSymbol(0x2104, 2, "OAMDATA");
        romData.AddSymbol(0x2105, 2, "BGMODE");
        romData.AddSymbol(0x2106, 2, "MOSAIC");
        romData.AddSymbol(0x2107, 2, "BG1SC");
        romData.AddSymbol(0x2108, 2, "BG2SC");
        romData.AddSymbol(0x2109, 2, "BG3SC");
        romData.AddSymbol(0x210A, 2, "BG4SC");
        romData.AddSymbol(0x210B, 2, "BG12NBA");
        romData.AddSymbol(0x210C, 2, "BG34NBA");
        romData.AddSymbol(0x210D, 2, "BG1HOFS");
        romData.AddSymbol(0x210E, 2, "BG1VOFS");
        romData.AddSymbol(0x210F, 2, "BG2HOFS");
        romData.AddSymbol(0x2110, 2, "BG2VOFS");
        romData.AddSymbol(0x2111, 2, "BG3HOFS");
        romData.AddSymbol(0x2112, 2, "BG3VOFS");
        romData.AddSymbol(0x2113, 2, "BG4HOFS");
        romData.AddSymbol(0x2114, 2, "BG4VOFS");
        romData.AddSymbol(0x2115, 2, "VMAIN");
        romData.AddSymbol(0x2116, 2, "VMADDL");
        romData.AddSymbol(0x2117, 2, "VMADDH");
        romData.AddSymbol(0x2118, 2, "VMDATAL");
        romData.AddSymbol(0x2119, 2, "VMDATAH");
        romData.AddSymbol(0x211A, 2, "M7SEL");
        romData.AddSymbol(0x211B, 2, "M7A");
        romData.AddSymbol(0x211C, 2, "M7B");
        romData.AddSymbol(0x211D, 2, "M7C");
        romData.AddSymbol(0x211E, 2, "M7D");
        romData.AddSymbol(0x211F, 2, "M7X");
        romData.AddSymbol(0x2120, 2, "M7Y");
        romData.AddSymbol(0x2121, 2, "CGADD");
        romData.AddSymbol(0x2122, 2, "CGDATA");
        romData.AddSymbol(0x2123, 2, "W12SEL");
        romData.AddSymbol(0x2124, 2, "W34SEL");
        romData.AddSymbol(0x2125, 2, "WOBJSEL");
        romData.AddSymbol(0x2126, 2, "WH0");
        romData.AddSymbol(0x2127, 2, "WH1");
        romData.AddSymbol(0x2128, 2, "WH2");
        romData.AddSymbol(0x2129, 2, "WH3");
        romData.AddSymbol(0x212A, 2, "WBGLOG");
        romData.AddSymbol(0x212B, 2, "WOBJLOG");
        romData.AddSymbol(0x212C, 2, "TM");
        romData.AddSymbol(0x212D, 2, "TD");
        romData.AddSymbol(0x212E, 2, "TMW");
        romData.AddSymbol(0x212F, 2, "TSW");
        romData.AddSymbol(0x2130, 2, "CGWSEL");
        romData.AddSymbol(0x2131, 2, "CGADSUB");
        romData.AddSymbol(0x2132, 2, "COLDATA");
        romData.AddSymbol(0x2133, 2, "SETINI");
        romData.AddSymbol(0x2134, 2, "MPYL");
        romData.AddSymbol(0x2135, 2, "MPYM");
        romData.AddSymbol(0x2136, 2, "MPYH");
        romData.AddSymbol(0x2137, 2, "SLHV");
        romData.AddSymbol(0x2138, 2, "OAMDATAREAD");
        romData.AddSymbol(0x2139, 2, "VMDATAL");
        romData.AddSymbol(0x213A, 2, "VMDATAH");
        romData.AddSymbol(0x213B, 2, "CGDATAREAD");
        romData.AddSymbol(0x213C, 2, "OPHCT");
        romData.AddSymbol(0x213D, 2, "OPVCT");
        romData.AddSymbol(0x213E, 2, "STAT77");
        romData.AddSymbol(0x213F, 2, "STAT78");
        romData.AddSymbol(0x2140, 2, "APUI00");
        romData.AddSymbol(0x2141, 2, "APUI01");
        romData.AddSymbol(0x2142, 2, "APUI02");
        romData.AddSymbol(0x2143, 2, "APUI03");

        // WRAM Registers
        romData.AddSymbol(0x2180, 2, "WMDATA");
        romData.AddSymbol(0x2181, 2, "WMADDL");
        romData.AddSymbol(0x2182, 2, "WMADDM");
        romData.AddSymbol(0x2183, 2, "WMADDH");

        // Controller Registers
        romData.AddSymbol(0x4016, 2, "JOYA");
        romData.AddSymbol(0x4017, 2, "JOYB");

        // CPU Registers
        romData.AddSymbol(0x4200, 2, "NMITIMEN");
        romData.AddSymbol(0x4201, 2, "WRIO");
        romData.AddSymbol(0x4202, 2, "WRMPYA");
        romData.AddSymbol(0x4203, 2, "WRMPYB");
        romData.AddSymbol(0x4204, 2, "WRDIVLH");
        romData.AddSymbol(0x4205, 2, "WRDIVB");
        romData.AddSymbol(0x4207, 2, "HTIMELH");
        romData.AddSymbol(0x4209, 2, "VTIMELH");
        romData.AddSymbol(0x420B, 2, "MDMAEN");
        romData.AddSymbol(0x420C, 2, "HDMAEN");
        romData.AddSymbol(0x420D, 2, "MEMSEL");

        romData.AddSymbol(0x4210, 2, "RDNMI");
        romData.AddSymbol(0x4211, 2, "TIMEUP");
        romData.AddSymbol(0x4212, 2, "RDIO");
        romData.AddSymbol(0x4213, 2, "RDDIVL");
        romData.AddSymbol(0x4214, 2, "RDDIVH");
        romData.AddSymbol(0x4215, 2, "RDMPYL");
        romData.AddSymbol(0x4216, 2, "RDMPYH");

        romData.AddSymbol(0x4218, 2, "JOY1L");
        romData.AddSymbol(0x4219, 2, "JOY1H");
        romData.AddSymbol(0x421A, 2, "JOY2L");
        romData.AddSymbol(0x421B, 2, "JOY2H");
        romData.AddSymbol(0x421C, 2, "JOY3L");
        romData.AddSymbol(0x421D, 2, "JOY3H");
        romData.AddSymbol(0x421E, 2, "JOY4L");
        romData.AddSymbol(0x421F, 2, "JOY4H");

        // DMA Registers
        for (int a = 0; a < 8; a++)
        {
            romData.AddSymbol(0x4300 + (UInt64)(0x10 * a), 2, $"DMAP{a}");
            romData.AddSymbol(0x4301 + (UInt64)(0x10 * a), 2, $"BBAD{a}");
            romData.AddSymbol(0x4302 + (UInt64)(0x10 * a), 2, $"A1T{a}L");
            romData.AddSymbol(0x4303 + (UInt64)(0x10 * a), 2, $"A1T{a}H");
            romData.AddSymbol(0x4304 + (UInt64)(0x10 * a), 2, $"A1B{a}");
            romData.AddSymbol(0x4305 + (UInt64)(0x10 * a), 2, $"DAS{a}L");
            romData.AddSymbol(0x4306 + (UInt64)(0x10 * a), 2, $"DAS{a}H");
            romData.AddSymbol(0x430A + (UInt64)(0x10 * a), 2, $"NTRL{a}");
        }
    }
}