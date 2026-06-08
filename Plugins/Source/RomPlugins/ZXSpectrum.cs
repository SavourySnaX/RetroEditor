using RetroEditor.Plugins;
using System.Collections.Generic;

class ZXSpectrum : ISystemPlugin
{
    public static string Name => "ZXSpectrum";

    public string LibRetroPluginName => "fuse_libretro";

    public MemoryEndian Endian => MemoryEndian.Little;

    public bool RequiresReload => false;
}

class ZXSpectrum128 : ISystemPlugin
{
    public static string Name => "ZXSpectrum128";

    public string LibRetroPluginName => "fuse_libretro";

    public MemoryEndian Endian => MemoryEndian.Little;

    public bool RequiresReload => false;

    public Dictionary<string, string> OverrideCoreOptions => new() { { "fuse_machine", "Spectrum 128K" } };
}

