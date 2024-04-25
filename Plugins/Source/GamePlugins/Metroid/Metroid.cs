using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using RetroEditor.Plugins;

public class Metroid : IRetroPlugin
{
    public static string Name => "Metroid";

    public string RomPluginName => "NES";

    public bool RequiresAutoLoad => false;

    byte[] chkSum = new byte[]
    {
        0x5f, 0xdd, 0x33, 0x42, 0xaa, 0x23, 0x06, 0xb2, 0x94, 0xae, 0xd9, 0x0a, 0x3f, 0xa0, 0xe8, 0x59
    };

    public bool AutoLoadCondition(IMemoryAccess romAccess)
    {
        throw new NotImplementedException();
    }

    public bool CanHandle(string filename)
    {
        if (!File.Exists(filename))
            return false;
        var bytes = File.ReadAllBytes(filename);
        var hash = MD5.Create().ComputeHash(bytes);
        return hash.SequenceEqual(chkSum);
    }

    public ISave Export(IMemoryAccess romAcess)
    {
        throw new NotImplementedException();
    }

    public void SetupGameTemporaryPatches(IMemoryAccess romAccess)
    {
        romAccess.WriteBytes(WriteKind.TemporaryRom, 16+0x0a, new byte[] { 0x80 }); // Press Start - advance to new game screen
        romAccess.WriteBytes(WriteKind.TemporaryRom, 16+0x10DD, new byte[] { 0x80 }); // Press new game

        //romAccess.WriteBytes(WriteKind.TemporaryRom, 16+0x1325, new byte[] { 0x34 }); // Y POS
        //romAccess.WriteBytes(WriteKind.TemporaryRom, 16+0x1328, new byte[] { 0x78 }); // X POS

        romAccess.WriteBytes(WriteKind.TemporaryRom, 16+0x55D7, new byte[] { 3 }); // 0x4000 + (0x95D7-0x8000)     MAP X Position
        romAccess.WriteBytes(WriteKind.TemporaryRom, 16+0x55D8, new byte[] { 2 }); // 0x4000 + (0x95D8-0x8000)     MAP Y Position
        romAccess.WriteBytes(WriteKind.TemporaryRom, 16+0x55D9, new byte[] { 0x50 }); // 0x4000 + (0x95D9-0x8000)     Start Y

        //romAccess.WriteBytes(WriteKind.TemporaryRom, 16+0x253E + (14 * 32) + 3, new byte[] { 0x17 });   // Modify map room number....
    }
}