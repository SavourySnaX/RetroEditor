
using ZXSpectrumTape;

class ZXSpectrum : IRomPlugin
{
    public static string Name => "ZXSpectrum";

    // For now, we assume the whole file fits in the standard memory of the spectrum
    byte[] rom = new byte[16384];
    byte[] ram = new byte[65536 - 16384];
    ZXSpectrumTape.Tape tape = new ZXSpectrumTape.Tape();

    public bool Load(byte[] bytes, string kind)
    {
        var romFile = "c:\\mamesys64\\src\\mame\\roms\\spectrum\\spectrum.rom";
        if (File.Exists(romFile))
            rom = File.ReadAllBytes(romFile);    //make configurable
        if (kind == "TAP")
        {
            tape.Load(bytes);
            // TODO - This hack won't work for most things :)
            foreach (var block in tape.RegularCodeFiles())
            {
                if (block.data.Length > 0)
                {
                    System.Array.Copy(block.data, 0, ram, block.header.CodeStart - 16384, block.data.Length);
                }
            }
            return true;
        }
        if (kind == "MEM")
        {
            // just load a binary file straight into memory - 0x4000-0xFFFF
            System.Array.Copy(bytes, 0, ram, 0, bytes.Length);
            return true;
        }
        return false;
    }

    public bool Save(string filename, string kind)
    {
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
        return false;
    }

    public byte ReadByte(uint address)
    {
        if (address < 16384)
        {
            return rom[address];
        }
        return ram[address - 16384];
    }

    public void WriteByte(uint address, byte value)
    {
        if (address < 16384)
            return;
        ram[address - 16384] = value;
    }

    public ushort ReadWord(uint address)
    {
        return (ushort)(ReadByte(address+1) | (ReadByte(address+0) << 8));
    }

    public uint ReadLong(uint address)
    {
        return (uint)(ReadWord(address+2)<<16 | ReadWord(address+0));
    }

}
