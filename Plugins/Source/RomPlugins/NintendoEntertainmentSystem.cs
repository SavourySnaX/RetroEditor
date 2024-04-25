using RetroEditor.Plugins;
class NintendEntertainmentSystem : ISystemPlugin
{
    public static string Name => "NES";

    public string LibRetroPluginName => "fceumm_libretro";

    public MemoryEndian Endian => MemoryEndian.Little;

    public bool RequiresReload => true;
}

