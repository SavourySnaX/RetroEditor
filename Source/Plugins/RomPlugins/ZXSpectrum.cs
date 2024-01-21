
using System.Security.Cryptography;
using ZXSpectrumTape;

class ZXSpectrum : IRomPlugin
{
    public static string Name => "ZXSpectrum";

    public string LibRetroPluginName => "fuse_libretro";

    private LibRetroPlugin plugin;      // For loading and testing
    private PlayableRom playableRom;    // For loading and testing
    private IEditor editorInterface;

    public void Initialise(LibRetroPlugin libRetroInterface, IEditor editorInterface)
    {
        this.editorInterface = editorInterface;
        this.plugin = libRetroInterface;
        playableRom = new PlayableRom(editorInterface, plugin, true);
    }

    public bool InitialLoad(ProjectSettings settings)
    {
        playableRom.Setup(settings, editorInterface.GetRomPath(settings), ()=> 
        {
            var memory = plugin.GetMemory(0x5800, 768);     // Load until attribute memory contains the pattern...
            var hash = MD5.Create().ComputeHash(memory);
            return hash.SequenceEqual(screenHash);          // TODO - move to JSW
        });

        TODOApplyJSW();

        playableRom.Reset(true);

        return true;
    }

    private void TODOApplyJSW()
    {
        playableRom.WriteTemporaryMemory(MemoryRegion.Ram, 0x8785, new byte[] { 0xC9 });          // Store return to force out of cheat code key wait
        playableRom.WriteTemporaryMemory(MemoryRegion.Ram, 0x872C, new byte[] { 0xCA, 0x87 });    // Jump to game start
        playableRom.WriteTemporaryMemory(MemoryRegion.Ram, 0x88AC, new byte[] { 0xFC, 0x88 });    // start game

        byte yPos = 13 * 8;
        byte xPos = 1 * 8;
        byte roomNumber = 0x22;

        ushort attributeAddress = (ushort)(0x5C00 + ((yPos / 8) * 32) + (xPos / 8));

        playableRom.WriteTemporaryMemory(MemoryRegion.Ram, 0x87E6, new byte[] { (byte)(yPos * 2) });          // willys y cordinate
        playableRom.WriteTemporaryMemory(MemoryRegion.Ram, 0x87F0, new byte[] { (byte)(attributeAddress & 0xFF), (byte)(attributeAddress >> 8) });    // willys cordinate
        playableRom.WriteTemporaryMemory(MemoryRegion.Ram, 0x87EB, new byte[] { (byte)(roomNumber) });
    }

    public bool Reload(ProjectSettings settings)
    {
        playableRom.Reload(settings);

        TODOApplyJSW();

        playableRom.Reset(true);

        return true;
    }

    // tODO move
    public readonly byte[] screenHash = { 27, 10, 249, 194, 93, 180, 162, 138, 198, 11, 210, 12, 245, 143, 226, 53 };


    public void Save(ProjectSettings settings)
    {
        playableRom.Serialise(settings);
    }

    public bool Export(string filename, string kind)
    {
        // Before export, we need to restore the original state, and only apply the serialised part
        playableRom.Reset(false);

        // encode start address is spectrum basic format :
        ushort clear = 25000;
        var clearInteger = new byte [] { 0x0E,0x00,0x00, (byte)(clear & 0xFF), (byte)(clear >> 8), 0x00 };
        var clearAscii = System.Text.Encoding.ASCII.GetBytes($"{clear}");
        var start = 0x8400;
        var integer = new byte[] { 0x0E, 0x00, 0x00, (byte)(start & 0xFF), (byte)(start >> 8), 0x00 };
        var ascii = System.Text.Encoding.ASCII.GetBytes($"{start}");
        var clearCode = new byte[] { 0xFD };
        var loadCodeRandUsr = new byte[] { 0x3A, 0xEF, 0x22, 0x22, 0xAF, 0x3A, 0xF9, 0xC0 };
        var endBasic= new byte[] { 0x0D };
        ushort length = (ushort)(clearInteger.Length + clearAscii.Length + clearCode.Length + integer.Length + ascii.Length + loadCodeRandUsr.Length + endBasic.Length);
        var loader = new byte[] { 0x00, 0x0A, (byte)(length & 0xFF), (byte)(length >> 8) }.
            Concat(clearCode).Concat(clearAscii).Concat(clearInteger).
            Concat(loadCodeRandUsr).Concat(ascii).Concat(integer).Concat(endBasic).ToArray();
        var assembled = playableRom.ReadMemory(MemoryRegion.Ram, 0x8000,0x8000);
        var outTape = new Tape();
        var basicHeader = new HeaderBlock(HeaderKind.Program, "JSW", (UInt16)loader.Length, 10, (UInt16)loader.Length);
        outTape.AddHeader(basicHeader);
        var basicBlock = new DataBlock(loader);
        outTape.AddBlock(basicBlock);
        var header = new HeaderBlock(HeaderKind.Code, "JSW", (UInt16)assembled.Length,0x8000, 0);
        outTape.AddHeader(header);
        var block = new DataBlock(assembled);
        outTape.AddBlock(block);
        outTape.Save(filename);

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
