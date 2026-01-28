
namespace RetroEditor.Source.Internals.ReverseEngineering.Platform.SNES;

/// <summary>
/// SNES/65816 memory mapper for LoROM cartridges
/// </summary>
internal class SNESMemoryMapper : IMemoryMapper
{
    public UInt64 MapCpuToRegion(UInt64 cpuAddress, out MemoryRegionKey region)
    {
        region = new MemoryRegionKey((UInt32)MemoryInformationRegion.ROM);

        // Check for common RAM areas first
        if (cpuAddress < 0x2000)
        {
            region = new MemoryRegionKey((UInt32)MemoryInformationRegion.RAM);
            return cpuAddress;
        }

        // Check for SRAM areas
        if (cpuAddress >= 0x6000 && cpuAddress < 0x8000)
        {
            region = new MemoryRegionKey((UInt32)MemoryInformationRegion.SRAM);
            return cpuAddress - 0x6000;
        }

        // Check for I/O areas
        if (cpuAddress >= 0x2000 && cpuAddress < 0x6000)
        {
            region = new MemoryRegionKey((UInt32)MemoryInformationRegion.IO);
            return cpuAddress;
        }

        // Handle bank 0x7E and 0x7F (extended RAM)
        if (cpuAddress >= 0x7E0000 && cpuAddress < 0x800000)
        {
            region = new MemoryRegionKey((UInt32)MemoryInformationRegion.RAM);
            return cpuAddress - 0x7E0000;
        }

        // Handle other banks with potential SRAM
        if ((cpuAddress & 0xFF0000) >= 0x700000 && (cpuAddress & 0xFF0000) < 0x7E0000)
        {
            if ((cpuAddress & 0xFFFF) >= 0x6000 && (cpuAddress & 0xFFFF) < 0x8000)
            {
                region = new MemoryRegionKey((UInt32)MemoryInformationRegion.SRAM);
                return (cpuAddress & 0xFFFF) - 0x6000;
            }
        }

        // Handle ROM mapping
        var bank = (cpuAddress >> 16) & 0xFF;
        var offset = cpuAddress & 0xFFFF;

        if (offset < 0x8000)
        {
            // Lower half of bank - not ROM in LoROM
            region = new MemoryRegionKey((UInt32)MemoryInformationRegion.Invalid);
            return 0;
        }

        // Map to LoROM format
        UInt64 romAddress = ((UInt64)(bank & 0x7F) << 15) + (offset - 0x8000);
        return romAddress;
    }

    public UInt64 MapRomToCpu(UInt64 romAddress)
    {
        // Convert linear ROM address back to SNES CPU address
        var bank = (romAddress >> 15) & 0x7F;
        var offset = (romAddress & 0x7FFF) + 0x8000;
        return (bank << 16) | offset;
    }

    public UInt64 MapHardwareAddressToCpu(UInt64 linearAddress)
    {
        UInt64 bank = (linearAddress >> 15) & 0x7F;
        UInt64 offset = linearAddress & 0x7FFF;

        if (bank < 0x40)
        {
            return (bank << 16) | 0x8000 | offset;
        }
        else if (bank < 0x70)
        {
            return (bank << 15) | offset;
        }
        else
        {
            return ((bank - 0x70) << 15) | offset;
        }
    }
    
    public UInt64 MapCpuToHardwareAddress(UInt64 address, out MemoryRegionKey region)
    {
        region = new MemoryRegionKey((UInt32)MemoryInformationRegion.ROM);
        var bank = address >> 16;
        var offset = address & 0xFFFF;

        if (bank == 0x7E || bank == 0x7F)
        {
            region = new MemoryRegionKey((UInt32)MemoryInformationRegion.RAM);
            return ((bank - 0x7E) << 16) | offset;
        }
        else if (bank == 0xFE || bank == 0xFF)
        {
            if (offset < 0x8000)
            {
                // SRAM
                region = new MemoryRegionKey((UInt32)MemoryInformationRegion.SRAM);
                return ((bank - 0xF0) << 15) | offset;
            }
            else
            {
                // ROM
                return 0x3F0000 | ((bank - 0xFE) << 15) | (offset & 0x7FFF);
            }
        }
        bank &= 0x7F;
        if (bank < 0x40)
        {
            if (offset < 0x2000)
            {
                // Low RAM
                region = new MemoryRegionKey((UInt32)MemoryInformationRegion.RAM);
                return offset;
            }
            else if (offset < 0x8000)
            {
                // IO
                region = new MemoryRegionKey((UInt32)MemoryInformationRegion.IO);
                return offset - 0x2000;
            }
            else
            {
                // ROM
                return (bank << 15) | (offset & 0x7FFF);
            }
        }
        else if (bank < 0x70)
        {
            // ROM
            return (bank << 15) | (offset & 0x7FFF);
        }
        else
        {
            // 70-7D
            if (offset < 0x8000)
            {
                region = new MemoryRegionKey((UInt32)MemoryInformationRegion.SRAM);
                return ((bank - 0x70) << 15) | offset;
            }
            else
            {
                //ROM
                return (bank << 15) | (offset & 0x7FFF);
            }
        }
    }
}