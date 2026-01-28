namespace RetroEditor.Source.Internals.ReverseEngineering.Platform.Megadrive;

/// <summary>
/// Megadrive/Genesis 68000 memory mapper
/// </summary>
internal class MegadriveMemoryMapper : IMemoryMapper
{
    public ulong MapCpuToHardwareAddress(ulong address, out MemoryRegionKey region)
    {
        return MapCpuToRegion(address, out region);
    }

    public UInt64 MapCpuToRegion(UInt64 cpuAddress, out MemoryRegionKey region)
    {
        region = new MemoryRegionKey((UInt32)MemoryInformationRegion.ROM);
        
        // Megadrive memory map:
        // 0x000000 - 0x3FFFFF: ROM (4MB)
        // 0x400000 - 0x7FFFFF: Reserved (sometimes used for larger ROMs or SRAM)
        // 0x800000 - 0x9FFFFF: Reserved
        // 0xA00000 - 0xA0FFFF: Z80 address space
        // 0xA10000 - 0xA10FFF: I/O and control registers
        // 0xA11000 - 0xA11FFF: Z80 control
        // 0xA12000 - 0xBFFFFF: Reserved
        // 0xC00000 - 0xDFFFFF: VDP (Video Display Processor)
        // 0xE00000 - 0xFFFFFF: RAM (mirrored)
        
        // Check for RAM
        if (cpuAddress >= 0xFF0000 || (cpuAddress >= 0xE00000 && cpuAddress <= 0xFFFFFF))
        {
            region = new MemoryRegionKey((UInt32)MemoryInformationRegion.RAM);
            return cpuAddress & 0xFFFF; // 64KB RAM, mirrored
        }
        
        // Check for I/O
        if (cpuAddress >= 0xA00000 && cpuAddress < 0xC00000)
        {
            region = new MemoryRegionKey((UInt32)MemoryInformationRegion.IO);
            return cpuAddress;
        }
        
        // Check for VDP
        if (cpuAddress >= 0xC00000 && cpuAddress < 0xE00000)
        {
            region = new MemoryRegionKey((UInt32)MemoryInformationRegion.IO);
            return cpuAddress;
        }
        
        // Check for SRAM (typically mapped at 0x200000-0x3FFFFF on some carts)
        // This varies by cartridge, but a common mapping is:
        if (cpuAddress >= 0x200000 && cpuAddress < 0x400000)
        {
            // Could be ROM or SRAM depending on cart configuration
            // For now, treat as ROM
            region = new MemoryRegionKey((UInt32)MemoryInformationRegion.ROM);
            return cpuAddress;
        }
        
        // ROM area (0x000000 - 0x3FFFFF)
        if (cpuAddress < 0x400000)
        {
            region = new MemoryRegionKey((UInt32)MemoryInformationRegion.ROM);
            return cpuAddress;
        }
        
        // Extended ROM area for larger games
        if (cpuAddress >= 0x400000 && cpuAddress < 0x800000)
        {
            region = new MemoryRegionKey((UInt32)MemoryInformationRegion.ROM);
            return cpuAddress;
        }
        
        region = new MemoryRegionKey((UInt32)MemoryInformationRegion.Invalid);
        return 0;
    }

    public ulong MapHardwareAddressToCpu(ulong linearAddress)
    {
        return linearAddress;
    }

    public UInt64 MapRomToCpu(UInt64 romAddress)
    {
        // ROM is directly mapped starting at 0x000000
        return romAddress;
    }
}
