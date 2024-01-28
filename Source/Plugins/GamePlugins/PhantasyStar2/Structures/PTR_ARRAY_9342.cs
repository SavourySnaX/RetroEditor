public struct PTR_ARRAY_9342
{
    const uint EntriesInRom = 7;

    PTR_9342[] ptrs;
    struct PTR_9342
    {
        internal uint compressedBitmapData;
        internal uint addressB;
    }

    public void Load(IRomAccess rom, uint address)
    {
        ptrs=new PTR_9342[EntriesInRom];
        var bytes = rom.ReadBytes(ReadKind.Rom, address, (uint)(EntriesInRom * 8));
        for (int a=0;a<EntriesInRom;a++)
        {
            ptrs[a].compressedBitmapData = rom.FetchMachineOrder32(a * 8, bytes);//    rom.ReadLong(address+(uint)(a*8));
            ptrs[a].addressB = rom.FetchMachineOrder32(a * 8 + 4, bytes); //rom.ReadLong(address+(uint)(a*8)+4);
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