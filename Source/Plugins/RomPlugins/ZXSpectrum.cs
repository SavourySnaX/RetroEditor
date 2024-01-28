
class ZXSpectrum : IRomPlugin
{
    public static string Name => "ZXSpectrum";

    public string LibRetroPluginName => "fuse_libretro";

    public MemoryEndian Endian => MemoryEndian.Little;

    public bool RequiresReload => false;
}
