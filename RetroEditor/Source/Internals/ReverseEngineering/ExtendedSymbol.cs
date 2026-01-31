using RetroEditor.Source.Internals.ReverseEngineering.Platform;

/// <summary>
/// Extended symbol information with metadata for reverse engineering.
/// Wraps basic symbol data with memory region context for navigation and organization.
/// </summary>
internal record ExtendedSymbol(ulong Address, int Size, string Name, MemoryRegionKey RegionKey)
{
    /// <summary>
    /// Gets the display name for this symbol's memory region.
    /// </summary>
    public string RegionName => RegionKey.Key switch
    {
        0 => "ROM",
        1 => "RAM",
        2 => "SRAM",
        3 => "IO",
        _ => "Unknown"
    };

    public override string ToString() => $"{Name} @ 0x{Address:X8} ({RegionName})";
}

/// <summary>
/// Extended label information with memory region context for navigation and organization.
/// </summary>
internal record ExtendedLabel(ulong Address, string Name, MemoryRegionKey RegionKey)
{
    /// <summary>
    /// Gets the display name for this label's memory region.
    /// </summary>
    public string RegionName => RegionKey.Key switch
    {
        0 => "ROM",
        1 => "RAM",
        2 => "SRAM",
        3 => "IO",
        _ => "Unknown"
    };

    public override string ToString() => $"{Name} @ 0x{Address:X8} ({RegionName})";
}
