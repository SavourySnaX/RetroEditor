using RetroEditor.Source.Internals.ReverseEngineering.Platform;

internal class SNESMemoryInformationProvider : IMemoryInformationProvider
{
    public IEnumerable<IMemoryInformation> GetMemoryRegions()
    {
//        yield return new SNESWRAMRegion();
//        yield return new SNESVRAMRegion();
        yield return new SNESROMRegion();
    }
}

internal class SNESROMRegion : IMemoryInformation
{
    public string MameViewName => "Region ':snsslot:cart:rom'";
}
