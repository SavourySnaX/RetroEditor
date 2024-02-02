

class MasterSystem : IRomPlugin
{
    public static string Name => "MasterSystem";

    public string LibRetroPluginName => "smsplus_libretro";

    public MemoryEndian Endian => MemoryEndian.Little;

    public bool RequiresReload => true;
}
