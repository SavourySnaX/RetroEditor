

class MasterSystem : IRomPlugin
{
    public static string Name => "MasterSystem";

    public string LibRetroPluginName => "genesis_plus_gx_libretro";

    public MemoryEndian Endian => MemoryEndian.Little;

    public bool RequiresReload => true;
}
