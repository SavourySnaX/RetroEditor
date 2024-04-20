using System;

class Megadrive : IRomPlugin
{
    public static string Name => "Megadrive";

    public string LibRetroPluginName => "genesis_plus_gx_libretro";

    public MemoryEndian Endian => MemoryEndian.Big;

    public bool RequiresReload => true;

    public ReadOnlySpan<byte> ChecksumCalculation(IRomAccess rom, out int address)
    {
        ushort chk = 0;
        int length = rom.RomSize;
        var romContents = rom.ReadBytes(ReadKind.Rom, 0, (uint)length);
        for (int a = 0x200; a < length; a += 2)
        {
            ushort word = rom.FetchMachineOrder16(a, romContents);
            chk += word;
        }
        var newBytes = new byte[2];
        rom.WriteMachineOrder16(0, newBytes, chk);
        address=0x18e;
        return newBytes;
    }
}