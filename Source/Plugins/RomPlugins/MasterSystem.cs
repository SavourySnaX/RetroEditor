/*
class MasterSystem : IRomPlugin
{
    public static string Name => "Master System";

    byte[] data;

    public MasterSystem()
    {
        data = Array.Empty<byte>();
    }

    public bool Load(byte[] bytes, string kind)
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
        return (ushort)(data[address + 0] | (data[address + 1] << 8));
    }

    public uint ReadLong(uint address)
    {
        throw new NotImplementedException();
    }

}
*/