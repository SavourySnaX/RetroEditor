
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using RetroEditor.Plugins;

public class StarFox : IRetroPlugin
{
    public static string Name => "Star Fox";

    public string RomPluginName => "SNES";

    public bool RequiresAutoLoad => false;

    byte[] starfox_us_rev2_headerless = [0xde, 0xf6, 0x6d, 0xb1, 0x2f, 0x5e, 0x64, 0x4c, 0x0c, 0xf0, 0x0c, 0x42, 0xcf, 0xa7, 0xae, 0x7b];

    public bool AutoLoadCondition(IMemoryAccess romAccess)
    {
        throw new System.NotImplementedException("AutoLoadCondition not required");
    }

    public bool CanHandle(string path)
    {
        if (!File.Exists(path))
            return false;
        var bytes = File.ReadAllBytes(path);
        var hash = MD5.Create().ComputeHash(bytes);
        return hash.SequenceEqual(starfox_us_rev2_headerless);
    }

    public ISave Export(IMemoryAccess romAcess)
    {
        throw new System.NotImplementedException();
    }

    public void SetupGameTemporaryPatches(IMemoryAccess romAccess)
    {
    }
}