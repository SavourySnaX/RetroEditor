using RetroEditor.Source.Internals.ReverseEngineering.Platform;

internal class MegadriveMemoryInformationProvider : IMemoryInformationProvider
{
    // Standard memory region keys for Megadrive platform
    public static readonly MemoryRegionKey ROMKey = new MemoryRegionKey((UInt32)MemoryInformationRegion.ROM);
    public static readonly MemoryRegionKey RAMKey = new MemoryRegionKey((UInt32)MemoryInformationRegion.RAM);
    public static readonly MemoryRegionKey SRAMKey = new MemoryRegionKey((UInt32)MemoryInformationRegion.SRAM);
    public static readonly MemoryRegionKey IOKey = new MemoryRegionKey((UInt32)MemoryInformationRegion.IO);
    public static readonly MemoryRegionKey InvalidKey = new MemoryRegionKey((UInt32)MemoryInformationRegion.Invalid);

    public IEnumerable<IMemoryInformation> GetMemoryRegions()
    {
        yield return new MegadriveROMRegion();
        yield return new MegadriveRAMRegion();
        yield return new MegadriveIORegion();
    }
}

internal class MegadriveROMRegion : IMemoryInformation
{
    public string MameViewName => "Region ':mdslot:cart:rom'";
    public string DisplayName => "Cartridge ROM";
    public MemoryRegionKey RegionKey => MegadriveMemoryInformationProvider.ROMKey;
    public bool HasPhysicalData => true;
    public MemoryRegionType Type => MemoryRegionType.Code;
    public (UInt64 Start, UInt64 End) AddressRange => (0x000000, 0x7FFFFF);

    public IMemoryRegionDataProvider CreateDataProvider()
    {
        return new DebuggerDataReadOnlyProvider(MameViewName, MameViewName);
    }
}

internal class MegadriveRAMRegion : IMemoryInformation
{
    public string MameViewName => "memory/:maincpu/0/:megadrive_ram";
    public string DisplayName => "RAM";
    public MemoryRegionKey RegionKey => MegadriveMemoryInformationProvider.RAMKey;
    public bool HasPhysicalData => true;
    public MemoryRegionType Type => MemoryRegionType.Mixed;
    public (UInt64 Start, UInt64 End) AddressRange => (0xFF0000, 0xFFFFFF);

    public IMemoryRegionDataProvider CreateDataProvider()
    {
        return new DebuggerDataLiveProvider(MameViewName, MameViewName, 0xFF0000, 0xFFFFFF);
    }
}

internal class MegadriveIORegion : IMemoryInformation
{
    public string MameViewName => "Megadrive IO";
    public string DisplayName => "IO";
    public MemoryRegionKey RegionKey => MegadriveMemoryInformationProvider.IOKey;
    public bool HasPhysicalData => false;
    public MemoryRegionType Type => MemoryRegionType.Data;
    public (UInt64 Start, UInt64 End) AddressRange => (0xA00000, 0xDFFFFF);

    public IMemoryRegionDataProvider CreateDataProvider()
    {
        return new VirtualDataProvider(DisplayName, 0x400000);
    }
}

