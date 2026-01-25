using RetroEditor.Source.Internals.ReverseEngineering.Platform;

internal class MegadriveMemoryInformationProvider : IMemoryInformationProvider
{
    public IEnumerable<IMemoryInformation> GetMemoryRegions()
    {
        yield return new MegadriveROMRegion();
        yield return new MegadriveRAMRegion();
    }
}

internal class MegadriveROMRegion : IMemoryInformation
{
    public string MameViewName => "Region ':mdslot:cart:rom'";
    public string DisplayName => "Cartridge ROM";
    public bool HasPhysicalData => true;
    public MemoryRegionType Type => MemoryRegionType.Code;
    public (UInt64 Start, UInt64 End) AddressRange => (0x000000, 0x7FFFFF);

    public IMemoryRegionDataProvider CreateDataProvider()
    {
        return new DebuggerDataProvider(MameViewName, MameViewName);
    }
}

internal class MegadriveRAMRegion : IMemoryInformation
{
    public string MameViewName => "memory/:maincpu/0/:ram";
    public string DisplayName => "RAM";
    public bool HasPhysicalData => false;
    public MemoryRegionType Type => MemoryRegionType.Mixed;
    public (UInt64 Start, UInt64 End) AddressRange => (0xFF0000, 0xFFFFFF);

    public IMemoryRegionDataProvider CreateDataProvider()
    {
        return new VirtualDataProvider(MameViewName, 0x10000);
    }
}

