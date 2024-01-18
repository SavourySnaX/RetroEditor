/*
class Megadrive : IRomPlugin
{
    public static string Name => "Megadrive";
    byte[] data;

    public Megadrive()
    {
        data = Array.Empty<byte>();
    }

    public bool Load(byte[] bytes,string kind)
    {
        data = bytes;
        return true;
    }

    public bool Save(string filename, string kind)
    {
        throw new NotImplementedException();
    }

    public byte ReadByte(uint address)
    {
        return data[address];
    }

    public void WriteByte(uint address, byte value)
    {
        data[address] = value;
    }

    public ushort ReadWord(uint address)
    {
        return (ushort)(data[address + 1] | (data[address + 0] << 8));
    }

    public uint ReadLong(uint address)
    {
        return (uint)(data[address + 3] | (data[address + 2] << 8) | (data[address + 1] << 16) | (data[address + 0] << 24));
    }

}
*/