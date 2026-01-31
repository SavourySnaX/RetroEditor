namespace RetroEditor.Source.Internals.ReverseEngineering.Platform.ZXSpectrum;

/// <summary>
/// ZX Spectrum hardware register provider
/// Note: ZX Spectrum primarily uses I/O ports rather than memory-mapped registers.
/// This provider can be expanded to add common port symbols for reference.
/// </summary>
internal class ZXSpectrumHardwareRegisterProvider : IHardwareSymbolsProvider
{
    public void InitializeSymbols(IRomDataParser romData, MemoryRegionKey regionKey)
    {
        if (regionKey == ZXSpectrumMemoryInformationProvider.ROMKey)
        {
            romData.AddSymbol(0x00FE, 1, "ULA Port");
        }
        if (regionKey == ZXSpectrumMemoryInformationProvider.RAMKey)
        {
        }
        if (regionKey == ZXSpectrumMemoryInformationProvider.ROMKey)
        {
            romData.AddLabel(0x0000,"ROM_START");
            romData.AddLabel(0x0008,"ROM_ERROR");
            romData.AddLabel(0x0010,"ROM_PRINT_A");
            romData.AddLabel(0x0018,"ROM_GET_CHAR");
            romData.AddLabel(0x0020,"ROM_NEXT_CHAR");
            romData.AddLabel(0x0028,"ROM_FP_CALC");
        }
    }
}
