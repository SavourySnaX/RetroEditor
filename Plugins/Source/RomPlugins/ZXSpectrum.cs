using RetroEditor.Plugins;

class ZXSpectrum : ISystemPlugin
{
    public static string Name => "ZXSpectrum";

    public string LibRetroPluginName => "fuse_libretro";

    public MemoryEndian Endian => MemoryEndian.Little;

    public bool RequiresReload => false;
}
