/*
public struct MapDataEntry
{
    byte data0;
    byte data1;
    uint offset0;
    uint offset1;
    uint offset2;
    uint transitionData;
    byte data2; // loc_283ee  offset (high bit cleared, high bit means ? ? )
    byte musicID;

    public void LoadDataFromAddress(IRomPlugin rom, uint address)
    {
        data0=rom.ReadByte(address);
        data1=rom.ReadByte(address+1);
        offset0=rom.ReadLong(address+2);
        offset1=rom.ReadLong(address+6);
        offset2=rom.ReadLong(address+10);
        transitionData=rom.ReadLong(address+14);
        data2=rom.ReadByte(address+18);
        musicID=rom.ReadByte(address+19);
    }

    public int PlanetIndex => (int)(data0 & 0x80)>>7;

    public int Table9342Index => (int)(data0 & 0x0F);

    public int MaxCameraPosY => ((int)(data1 & 0xF0) << 4);
    public int MaxCameraPosX => ((int)(data1 & 0x0F) << 8);

    public ushort Formation1Index => (ushort)(offset0 >> 24);
    public uint MapLayoutBG => offset0 & 0x00FFFFFF;
    public ushort Formation2Index => (ushort)(offset1 >> 24);
    public uint MapLayoutFG => offset1 & 0x00FFFFFF;

    public int PaletteIndex => (int)(transitionData >> 24);

    public int SpriteDataTableOffset => (int)(data2 & 0x7F);
}
*/