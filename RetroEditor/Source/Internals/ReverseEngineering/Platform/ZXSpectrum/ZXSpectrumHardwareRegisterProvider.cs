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
        // Only add hardware register symbols to the IO region
        if (regionKey != ZXSpectrumMemoryInformationProvider.IOKey)
            return;
            
        // Common ZX Spectrum ports (for reference; not memory mapped)
        // romData.AddSymbol(0x00FE, 1, "ULA Port");
        // romData.AddSymbol(0x7FFD, 1, "Memory Paging (128K)");
        // romData.AddSymbol(0xBFFD, 1, "AY Control");
        // romData.AddSymbol(0xFFFD, 1, "AY Data");
        // Left intentionally minimal; memory-mapped symbols not typical on Spectrum
    }
}
