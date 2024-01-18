
using System.Security.Cryptography;
using ZXSpectrumTape;

class ZXSpectrum : IRomPlugin
{
    public static string Name => "ZXSpectrum";

    private ZXSpectrumTape.Tape tape;   // For saving
    private LibRetroPlugin plugin;      // For loading and testing
    private PlayableRom playableRom;    // For loading and testing

    public LibRetroPlugin? Initialise()
    {
        plugin = LibRetroPluginFactory.Create("fuse_libretro","C:\\work\\editor\\RetroEditor\\data\\2.dll");

        if (plugin.Version() != 1)
        {
            return null;
        }
        plugin.Init();

        playableRom = new PlayableRom(plugin);

        return plugin;
    }

    public bool Load(string filename)
    {
        plugin.LoadGame(filename);//"c:\\work\\editor\\jsw\\Jet Set Willy (1984)(Software Projects).tzx");

            // Load to a defined point (because we are loading from tape)
        plugin.AutoLoad(() =>
        {
            var memory = plugin.GetMemory(0x5800, 768);     // Load until attribute memory contains the pattern...
            var hash = MD5.Create().ComputeHash(memory);
            return hash.SequenceEqual(screenHash);          // TODO - move to JSW
        });

        // We should save snapshot, so we don't need to load from tape again...
        var saveSize = plugin.GetSaveStateSize();
        var state = new byte[saveSize];
        plugin.SaveState(state);

    /// TODO MOVE
            // FROM HERE
            plugin.RestoreState(state);

            // Kill copy protection code, and jump to screen to test...
            plugin.SetMemory(0x8785, new byte[] { 0xC9 });          // Store return to force out of cheat code key wait
            plugin.SetMemory(0x872C, new byte[] { 0xCA, 0x87 });    // Jump to game start
            plugin.SetMemory(0x88AC, new byte[] { 0xFC, 0x88 });    // start game

            byte yPos = 13 * 8;
            byte xPos = 1 * 8;
            byte roomNumber = 0x22;

            ushort attributeAddress = (ushort)(0x5C00 + ((yPos / 8) * 32) + (xPos / 8));

            plugin.SetMemory(0x87E6, new byte[] { (byte)(yPos * 2) });          // willys y cordinate
            plugin.SetMemory(0x87F0, new byte[] { (byte)(attributeAddress & 0xFF), (byte)(attributeAddress >> 8) });    // willys cordinate
            plugin.SetMemory(0x87EB, new byte[] { (byte)(roomNumber) });
            // TO HERE
        return true;
    }


    public void something()
    {
        // 
/*
   MOVE TO JSW

        aVInfo = plugin.GetSystemAVInfo();
        frameHeight= aVInfo.geometry.maxHeight;
        frameWidth = aVInfo.geometry.maxWidth;
        var image = Raylib.GenImageColor((int)aVInfo.geometry.maxWidth, (int)aVInfo.geometry.maxHeight, Color.BLACK);
        image = new Image
        {
            Width = (int)aVInfo.geometry.maxWidth,
            Height = (int)aVInfo.geometry.maxHeight,
            Mipmaps = 1,
            Format = PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8
        };

        bitmap = Raylib.LoadTextureFromImage(image);
*/

    }

    // tODO move
    public readonly byte[] screenHash = { 27, 10, 249, 194, 93, 180, 162, 138, 198, 11, 210, 12, 245, 143, 226, 53 };

    public bool Save(string filename, string kind)
    {
        return false;   // To implement
/*
        if (kind == "TAP")
        {
            // TODO - This hack won't work for most things :)`
            var newTape = new ZXSpectrumTape.Tape();
            foreach (var basic in tape.BasicPrograms())
            {
                newTape.AddHeader(basic.header);
                newTape.AddBlock(new ZXSpectrumTape.DataBlock(basic.data));
            }
            foreach (var block in tape.RegularCodeFiles())
            {
                newTape.AddHeader(block.header);
                var bytes = ram.AsSpan().Slice(block.header.CodeStart - 16384, block.data.Length).ToArray();
                newTape.AddBlock(new ZXSpectrumTape.DataBlock(bytes));
            }
            newTape.Save(filename);
            return true;
        }
        return false;*/
    }

    public byte ReadByte(uint address)
    {
        //TODO - this probably needs caching
        return plugin.GetMemory(address, 1)[0];
        /*if (address < 16384)
        {
            return rom[address];
        }
        return ram[address - 16384];*/
    }

    public void WriteByte(uint address, byte value)
    {
        // TODO - this probably needs caching
        plugin.SetMemory(address, new byte[] { value });
        // TODO
        /*
        if (address < 16384)
            return;
        ram[address - 16384] = value;*/
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
