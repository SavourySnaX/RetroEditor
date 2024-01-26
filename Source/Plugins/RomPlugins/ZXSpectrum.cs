
using System.Security.Cryptography;
using ZXSpectrumTape;

class ZXSpectrum : IRomPlugin
{
    public static string Name => "ZXSpectrum";

    public string LibRetroPluginName => "fuse_libretro";

    public MemoryEndian Endian => MemoryEndian.Little;

    private PlayableRom playableRom;    // For loading and testing

    public void Initialise(PlayableRom playableRom)
    {
        this.playableRom = playableRom;
    }


    public bool Export(string filename, IRetroPlugin retroPlugin)
    {
        return true;
    }

    public byte ReadByte(uint address)
    {
        return playableRom.ReadMemory(MemoryRegion.Ram, address, 1)[0];
    }

    public void WriteByte(uint address, byte value)
    {
        playableRom.WriteSerialisedMemory(MemoryRegion.Ram, address, new byte[] { value });
    }

    public ushort ReadWord(uint address)
    {
        return (ushort)(ReadByte(address+0) | (ReadByte(address+1) << 8));
    }

    public uint ReadLong(uint address)
    {
        return (uint)(ReadWord(address+2)<<16 | ReadWord(address+0));
    }

}
