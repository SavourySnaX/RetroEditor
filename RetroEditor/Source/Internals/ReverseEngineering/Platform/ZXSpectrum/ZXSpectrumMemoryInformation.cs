using RetroEditor.Source.Internals.ReverseEngineering.Platform;

namespace RetroEditor.Source.Internals.ReverseEngineering.Platform.ZXSpectrum;

internal class ZXSpectrumMemoryInformationProvider : IMemoryInformationProvider
{
    // Standard memory region keys for ZX Spectrum platform
    public static readonly MemoryRegionKey ROMKey = new MemoryRegionKey((UInt32)MemoryInformationRegion.ROM);
    public static readonly MemoryRegionKey RAMKey = new MemoryRegionKey((UInt32)MemoryInformationRegion.RAM);
    public static readonly MemoryRegionKey SRAMKey = new MemoryRegionKey((UInt32)MemoryInformationRegion.SRAM);
    public static readonly MemoryRegionKey IOKey = new MemoryRegionKey((UInt32)MemoryInformationRegion.IO);
    public static readonly MemoryRegionKey InvalidKey = new MemoryRegionKey((UInt32)MemoryInformationRegion.Invalid);

    public IEnumerable<IMemoryInformation> GetMemoryRegions()
    {
        yield return new ZXSpectrumROMRegion();
        yield return new ZXSpectrumRAMRegion();
        yield return new ZXSpectrumIORegion();
    }
}

internal class ZXSpectrumROMRegion : IMemoryInformation
{
    public string MameViewName => "Zilog Z80 ':maincpu' program space memory";
    public string DisplayName => "ROM";
    public MemoryRegionKey RegionKey => ZXSpectrumMemoryInformationProvider.ROMKey;
    public bool HasPhysicalData => true;
    public MemoryRegionType Type => MemoryRegionType.Mixed;
    public (UInt64 Start, UInt64 End) AddressRange => (0x0000, 0x3FFF);

    public IMemoryRegionDataProvider CreateDataProvider()
    {
        return new DebuggerDataReadOnlyProvider(MameViewName, MameViewName);
    }
}

internal class ZXSpectrumIORegion : IMemoryInformation
{
    public string MameViewName => "Z80 IO Ports";
    public string DisplayName => "IO";
    public MemoryRegionKey RegionKey => ZXSpectrumMemoryInformationProvider.IOKey;
    public bool HasPhysicalData => false;
    public MemoryRegionType Type => MemoryRegionType.Data;
    public (UInt64 Start, UInt64 End) AddressRange => (0x0000, 0xFFFF);

    public IMemoryRegionDataProvider CreateDataProvider()
    {
        return new VirtualDataProvider(DisplayName, 0x10000);
    }
}
internal class ZXSpectrumRAMRegion : IMemoryInformation
{
    public string MameViewName => "Zilog Z80 ':maincpu' program space memory";
    public string DisplayName => "RAM";
    public MemoryRegionKey RegionKey => ZXSpectrumMemoryInformationProvider.RAMKey;
    public bool HasPhysicalData => true;
    public MemoryRegionType Type => MemoryRegionType.Mixed;
    public (UInt64 Start, UInt64 End) AddressRange => (0x4000, 0xFFFF);

    public IMemoryRegionDataProvider CreateDataProvider()
    {
        return new DebuggerDataLiveProvider(MameViewName, MameViewName, 0x4000, 0xFFFF);
    }
}
