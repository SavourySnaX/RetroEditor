namespace RetroEditor.Source.Internals.ReverseEngineering.Platform.ZXSpectrum;

/// <summary>
/// ZX Spectrum Z80 memory mapper (basic 48K/128K model without bank details)
/// </summary>
internal class ZXSpectrumMemoryMapper : IMemoryMapper
{
    // TODO - what about ports
    public UInt64 MapCpuToRegion(UInt64 cpuAddress, out MemoryRegionKey region)
    {
        // ZX Specutrm 48K memory map
        // 0x0000 - 0x3FFF: ROM (16KB)
        // 0x4000 - 0xFFFF: RAM (48KB)
        if (cpuAddress < 0x4000)
        {
            region = new MemoryRegionKey((UInt32)MemoryInformationRegion.ROM);
            return cpuAddress;
        }
        if (cpuAddress < 0x10000)
        {
            region = new MemoryRegionKey((UInt32)MemoryInformationRegion.RAM);
            return cpuAddress;
        }
        region = new MemoryRegionKey((UInt32)MemoryInformationRegion.Invalid);
        return 0;
    }

    public UInt64 MapRomToCpu(UInt64 romAddress)
    {
        // the z80 on the 48k spectrum all memory is mapped linearly
        return romAddress;
    }

    public UInt64 MapHardwareAddressToCpu(UInt64 linearAddress)
    {
        // ZX Spectrum does not use memory-mapped IO; return linear as-is
        return linearAddress;
    }

    public UInt64 MapCpuToHardwareAddress(UInt64 address, out MemoryRegionKey region)
    {
        // No memory-mapped IO; map to ROM/RAM
        return MapCpuToRegion(address, out region);
    }
}
