class NintendEntertainmentSystem : IRomPlugin
{
    public static string Name => "NES";

    public string LibRetroPluginName => "fceumm_libretro";

    public MemoryEndian Endian => MemoryEndian.Little;

    public bool RequiresReload => true;
}

