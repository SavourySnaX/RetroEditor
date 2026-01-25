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
}

internal class SNESWRAMRegion : IMemoryInformation
{
    public string MameViewName => "memory/:maincpu/0/:wram";
    public string DisplayName => "Work RAM";
    public bool HasPhysicalData => false;
    public MemoryRegionType Type => MemoryRegionType.Mixed;
}
