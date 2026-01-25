using RetroEditor.Source.Internals.ReverseEngineering.Platform;

internal class SNESMemoryInformationProvider : IMemoryInformationProvider
{
    public IEnumerable<IMemoryInformation> GetMemoryRegions()
    {
        yield return new SNESROMRegion();
        yield return new SNESWRAMRegion();
    }
}

internal class SNESROMRegion : IMemoryInformation
{
    public string MameViewName => "Region ':snsslot:cart:rom'";
    public string DisplayName => "Cartridge ROM";
    public bool HasPhysicalData => true;
    public MemoryRegionType Type => MemoryRegionType.Mixed;
    public (UInt64 Start, UInt64 End) AddressRange => (0x000000, 0x7FFFFF);

    public IMemoryRegionDataProvider CreateDataProvider()
    {
        return new DebuggerDataProvider(MameViewName, MameViewName);
    }
}

internal class SNESWRAMRegion : IMemoryInformation
{
    public string MameViewName => "memory/:maincpu/0/:wram";
    public string DisplayName => "Work RAM";
    public bool HasPhysicalData => true;
    public MemoryRegionType Type => MemoryRegionType.Mixed;
    public (UInt64 Start, UInt64 End) AddressRange => (0x7E0000, 0x7FFFFF);

    public IMemoryRegionDataProvider CreateDataProvider()
    {
        return new DebuggerDataProvider(MameViewName, MameViewName);
    }
}

