
public struct PTR_ARRAY_9342
{
    const uint EntriesInRom = 7;

    PTR_9342[] ptrs;
    struct PTR_9342
    {
        internal uint compressedBitmapData;
        internal uint addressB;
    }

    public void Load(IRomPlugin rom, uint address)
    {
        ptrs=new PTR_9342[EntriesInRom];
        for (int a=0;a<EntriesInRom;a++)
        {
            ptrs[a].compressedBitmapData=rom.ReadLong(address+(uint)(a*8));
            ptrs[a].addressB=rom.ReadLong(address+(uint)(a*8)+4);
        }
    }

    public uint GetCompressedBitmapData(int index)
    {
        return ptrs[index].compressedBitmapData;
    }

    public uint GetAddressB(int index)
    {
        return ptrs[index].addressB;
    }
}
