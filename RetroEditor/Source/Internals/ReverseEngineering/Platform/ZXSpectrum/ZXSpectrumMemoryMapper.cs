namespace RetroEditor.Source.Internals.ReverseEngineering.Platform.ZXSpectrum;

/// <summary>
/// ZX Spectrum Z80 memory mapper (basic 48K/128K model without bank details)
/// </summary>
internal class ZXSpectrumMemoryMapper : IMemoryMapper
{
    public UInt64 MapCpuToRegion(UInt64 cpuAddress, out MemoryRegion region)
    {
        // 0x0000 - 0x3FFF: ROM (16KB)
        // 0x4000 - 0xFFFF: RAM (48KB)
        if (cpuAddress < 0x4000)
        {
            region = MemoryRegion.ROM;
            return cpuAddress;
        }
        if (cpuAddress < 0x10000)
        {
            region = MemoryRegion.RAM;
            return cpuAddress - 0x4000;
        }
        region = MemoryRegion.Invalid;
        return 0;
    }

    public UInt64 MapRomToCpu(UInt64 romAddress)
    {
        // ROM maps directly at 0x0000
        if (romAddress < 0x4000)
            return romAddress;
        return 0; // Out of range for base models
    }

    public UInt64 MapHardwareAddressToCpu(UInt64 linearAddress)
    {
        // ZX Spectrum does not use memory-mapped IO; return linear as-is
        return linearAddress;
    }

    public UInt64 MapCpuToHardwareAddress(UInt64 address, out MemoryRegion region)
    {
        // No memory-mapped IO; map to ROM/RAM
        return MapCpuToRegion(address, out region);
    }
}
