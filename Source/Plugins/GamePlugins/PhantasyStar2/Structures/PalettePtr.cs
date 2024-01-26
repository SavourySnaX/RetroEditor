/*
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

        public RGB GetColour(IRomPlugin rom, int index)
        {
            uint address = ptrToPalette + (uint)(index * 2);
            ushort colour = rom.ReadWord(address);
            byte b = (byte)((colour & 0x0E00) >> 4);
            byte g = (byte)((colour & 0x00E0) >> 0);
            byte r = (byte)((colour & 0x000E) << 4);
            return new RGB { r = r, g = g, b = b };
        }
    }

    PaletteEntry[] entries;

    public void Load(IRomPlugin rom)
    {
        LoadDataFromAddress(rom, fixedAddress, numPalettes);
    }

    private void LoadDataFromAddress(IRomPlugin rom, uint address, int numEntries)
    {
        entries = new PaletteEntry[numEntries];
        for (int a=0;a<numEntries;a++)
        {
            entries[a].ptrToPalette = rom.ReadLong(address);
            address += 4;
            entries[a].vdpTargetAddress = rom.ReadWord(address);
            address += 2;
            entries[a].length = rom.ReadWord(address);
            address += 2;
        }
    }

    public PaletteEntry GetEntry(int index)
    {
        return entries[index];
    }
}
*/
