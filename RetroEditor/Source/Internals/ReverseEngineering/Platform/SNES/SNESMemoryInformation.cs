using RetroEditor.Source.Internals.ReverseEngineering.Platform;

internal class SNESMemoryInformationProvider : IMemoryInformationProvider
{
    // Standard memory region keys for SNES platform
    public static readonly MemoryRegionKey ROMKey = new MemoryRegionKey((UInt32)MemoryInformationRegion.ROM);
    public static readonly MemoryRegionKey RAMKey = new MemoryRegionKey((UInt32)MemoryInformationRegion.RAM);
    public static readonly MemoryRegionKey SRAMKey = new MemoryRegionKey((UInt32)MemoryInformationRegion.SRAM);
    public static readonly MemoryRegionKey IOKey = new MemoryRegionKey((UInt32)MemoryInformationRegion.IO);
    public static readonly MemoryRegionKey InvalidKey = new MemoryRegionKey((UInt32)MemoryInformationRegion.Invalid);

    public IEnumerable<IMemoryInformation> GetMemoryRegions()
    {
        yield return new SNESROMRegion();
        yield return new SNESWRAMRegion();
        yield return new SNESIORegion();
    }
}

internal class SNESROMRegion : IMemoryInformation
{
    public string MameViewName => "Region ':snsslot:cart:rom'";
    public string DisplayName => "Cartridge ROM";
    public MemoryRegionKey RegionKey => SNESMemoryInformationProvider.ROMKey;
    public bool HasPhysicalData => true;
    public MemoryRegionType Type => MemoryRegionType.Mixed;
    public (UInt64 Start, UInt64 End) AddressRange => (0x000000, 0x7FFFFF);

    public IMemoryRegionDataProvider CreateDataProvider()
    {
        return new DebuggerDataReadOnlyProvider(MameViewName, MameViewName);
    }
}

internal class SNESIORegion : IMemoryInformation
{
    public string MameViewName => "SNES IO";
    public string DisplayName => "IO";
    public MemoryRegionKey RegionKey => SNESMemoryInformationProvider.IOKey;
    public bool HasPhysicalData => false;
    public MemoryRegionType Type => MemoryRegionType.Data;
    public (UInt64 Start, UInt64 End) AddressRange => (0x2000, 0x5FFF);

    public IMemoryRegionDataProvider CreateDataProvider()
    {
        return new VirtualDataProvider(DisplayName, 0x4000);
    }
}

internal class SNESWRAMRegion : IMemoryInformation
{
    public string MameViewName => "memory/:maincpu/0/:wram";
    public string DisplayName => "Work RAM";
    public MemoryRegionKey RegionKey => SNESMemoryInformationProvider.RAMKey;
    public bool HasPhysicalData => true;
    public MemoryRegionType Type => MemoryRegionType.Mixed;
    public (UInt64 Start, UInt64 End) AddressRange => (0x7E0000, 0x7FFFFF);

    public IMemoryRegionDataProvider CreateDataProvider()
    {
        return new DebuggerDataLiveProvider(MameViewName, MameViewName, 0x7E0000, 0x7FFFFF);
    }
}

