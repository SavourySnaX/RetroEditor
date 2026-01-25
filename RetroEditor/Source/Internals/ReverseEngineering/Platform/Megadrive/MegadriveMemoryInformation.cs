using RetroEditor.Source.Internals.ReverseEngineering.Platform;

internal class MegadriveMemoryInformationProvider : IMemoryInformationProvider
{
    public IEnumerable<IMemoryInformation> GetMemoryRegions()
    {
        yield return new MegadriveROMRegion();
    }
}

internal class MegadriveROMRegion : IMemoryInformation
{
    public string MameViewName => "Region ':mdslot:cart:rom'";
}
