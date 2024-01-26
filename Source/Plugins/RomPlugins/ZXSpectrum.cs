
using System.Security.Cryptography;
using ZXSpectrumTape;

class ZXSpectrum : IRomPlugin
{
    public static string Name => "ZXSpectrum";

    public string LibRetroPluginName => "fuse_libretro";

    public MemoryEndian Endian => MemoryEndian.Little;

    private PlayableRom playableRom;    // For loading and testing
    private IEditor editorInterface;

    public void Initialise(PlayableRom playableRom, IEditor editorInterface)
    {
        this.editorInterface = editorInterface;
        this.playableRom = playableRom;
    }

    public bool InitialLoad(ProjectSettings settings, IRetroPlugin retroPlugin)
    {
        playableRom.Setup(settings, editorInterface.GetRomPath(settings), retroPlugin.AutoLoadCondition);

        retroPlugin.SetupGameTemporaryPatches(playableRom);

        playableRom.Reset(true);

        return true;
    }

    public bool Reload(ProjectSettings settings, IRetroPlugin retroPlugin)
    {
        playableRom.Reload(settings);

        retroPlugin.SetupGameTemporaryPatches(playableRom);

        playableRom.Reset(true);

        return true;
    }


    public void Save(ProjectSettings settings)
    {
        playableRom.Serialise(settings);
    }

    public bool Export(string filename, IRetroPlugin retroPlugin)
    {
        // Before export, we need to restore the original state, and only apply the serialised part
        playableRom.Reset(false);

        retroPlugin.Export(playableRom).Save(filename);

        playableRom.Reset(true);
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
