using RetroEditor.Plugins;
class SuperNintendEntertainmentSystem : ISystemPlugin
{
    public static string Name => "SNES";

    public string LibRetroPluginName => "snes9x_libretro";

    public MemoryEndian Endian => MemoryEndian.Little;

    public bool RequiresReload => true;
}


