using RetroEditor.Source.Internals.ReverseEngineering.Platform;

namespace RetroEditor.Source.Internals.ReverseEngineering.Platform.ZXSpectrum;

internal class ZXSpectrumMemoryInformationProvider : IMemoryInformationProvider
{
    public IEnumerable<IMemoryInformation> GetMemoryRegions()
    {
        yield return new ZXSpectrumRAMRegion();
    }
}

internal class ZXSpectrumRAMRegion : IMemoryInformation
{
    public string MameViewName => "Zilog Z80 ':maincpu' program space memory";
    public string DisplayName => "RAM";
    public bool HasPhysicalData => true;
    public MemoryRegionType Type => MemoryRegionType.Mixed;
    public (UInt64 Start, UInt64 End) AddressRange => (0x0000, 0xFFFF);

    public IMemoryRegionDataProvider CreateDataProvider()
    {
        return new DebuggerDataProvider(MameViewName, MameViewName);
    }
}
