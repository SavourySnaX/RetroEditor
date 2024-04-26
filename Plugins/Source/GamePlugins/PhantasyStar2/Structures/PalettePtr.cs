using RetroEditor.Plugins;
public struct PalettePtr
{
    const uint fixedAddress = 0x1197C;

    const int numPalettes = 78;

    public struct PaletteEntry
    {
        internal uint ptrToPalette;
        internal ushort vdpTargetAddress;  // VDP target address
        internal ushort length;  // length -1


        public uint PtrToPalette => ptrToPalette;
        public uint VdpTargetAddress => vdpTargetAddress;
        public uint Length => length;

        public struct RGB
        {
            internal byte r;
            internal byte g;
            internal byte b;

            public byte R => r;
            public byte G => g;
            public byte B => b;
        }

        public RGB GetColour(IMemoryAccess rom, int index)
        {
            uint address = ptrToPalette + (uint)(index * 2);
            var bytes=rom.ReadBytes(ReadKind.Rom, address, 2);
            ushort colour = rom.FetchMachineOrder16(0, bytes); //rom.ReadWord(address);
            byte b = (byte)((colour & 0x0E00) >> 4);
            byte g = (byte)((colour & 0x00E0) >> 0);
            byte r = (byte)((colour & 0x000E) << 4);
            return new RGB { r = r, g = g, b = b };
        }
    }

    PaletteEntry[] entries;

    public void Load(IMemoryAccess rom)
    {
        LoadDataFromAddress(rom, fixedAddress, numPalettes);
    }

    private void LoadDataFromAddress(IMemoryAccess rom, uint address, int numEntries)
    {
        entries = new PaletteEntry[numEntries];
        var bytes = rom.ReadBytes(ReadKind.Rom, address, (uint)(numEntries * 8));
        for (int a=0;a<numEntries;a++)
        {
            entries[a].ptrToPalette = rom.FetchMachineOrder32(a * 8 + 0, bytes); //rom.ReadLong(address);
            entries[a].vdpTargetAddress = rom.FetchMachineOrder16(a * 8 + 4, bytes);// rom.ReadWord(address);
            entries[a].length = rom.FetchMachineOrder16(a * 8 + 6, bytes); //rom.ReadWord(address);
        }
    }

    public PaletteEntry GetEntry(int index)
    {
        return entries[index];
    }
}