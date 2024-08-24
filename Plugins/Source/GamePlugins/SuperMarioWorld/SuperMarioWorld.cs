using System.IO;
using System.Security.Cryptography;
using System.Linq;

using RetroEditor.Plugins;
using System;

using RetroEditorPlugin_SuperMarioWorld;

public class SuperMarioWorld : IRetroPlugin , IMenuProvider
{
	public static string Name => "Super Mario World";
	public string RomPluginName => "SNES";
	public bool RequiresAutoLoad => false;

    byte[] smw_japan = new byte[] { 0x4e, 0x4f, 0x8f, 0x4c, 0xfd, 0xaa, 0xbf, 0xfd, 0xde, 0x20, 0xc8, 0x05, 0x32, 0x02, 0xd4, 0xf0 };

    public bool AutoLoadCondition(IMemoryAccess romAccess)
    {
        throw new System.NotImplementedException("AutoLoadCondition not required");
    }

    public bool CanHandle(string filename)
    {
        if (!File.Exists(filename))
            return false;
        var bytes = File.ReadAllBytes(filename);
        var hash = MD5.Create().ComputeHash(bytes);
        return hash.SequenceEqual(smw_japan);
    }

	public void SetupGameTemporaryPatches(IMemoryAccess memoryAccess) 
    { 

    }

	public ISave Export(IMemoryAccess memoryAccess)
    {
        throw new System.NotImplementedException("Export not implemented");
    }

    public void ConfigureMenu(IMemoryAccess rom, IMenu menu)
    {
            menu.AddItem("Level", 
                (editorInterface,menuItem) => {
                    editorInterface.OpenUserWindow($"Level View", GetImage(editorInterface, rom));
                });
            menu.AddItem("GFX",
                (editorInterface,menuItem) => {
                    editorInterface.OpenUserWindow($"GFX View", GetGFXPageImage(editorInterface, rom));
                });
            menu.AddItem("VRAM",
                (editorInterface,menuItem) => {
                    editorInterface.OpenUserWindow($"VRAM View", GetVRAMImage(editorInterface, rom));
                });
            menu.AddItem("Palette",
                (editorInterface,menuItem) => {
                    editorInterface.OpenUserWindow($"Palette View", GetPaletteImage(editorInterface, rom));
                });
            menu.AddItem("Map 16",
                (editorInterface,menuItem) => {
                    editorInterface.OpenUserWindow($"Map16 View", GetMap16Image(editorInterface, rom));
                });
            menu.AddItem("Level Editor", 
                (editorInterface,menuItem) => {
                    editorInterface.OpenUserWindow($"Level Editor", GetLevelEditor(editorInterface, rom));
                });
    }

    public SuperMarioWorldLevelViewImage GetImage(IEditor editorInterface, IMemoryAccess rom)
    {
        return new SuperMarioWorldLevelViewImage(editorInterface, rom);
    }

    public SuperMarioWorldGFXPageImage GetGFXPageImage(IEditor editorInterface, IMemoryAccess rom)
    {
        return new SuperMarioWorldGFXPageImage(editorInterface, rom);
    }
    
    public SuperMarioWorldVramImage GetVRAMImage(IEditor editorInterface, IMemoryAccess rom)
    {
        return new SuperMarioWorldVramImage(editorInterface, rom);
    }

    public SuperMarioWorldPaletteImage GetPaletteImage(IEditor editorInterface, IMemoryAccess rom)
    {
        return new SuperMarioWorldPaletteImage(editorInterface, rom);
    }
    
    public SuperMarioWorldMap16Image GetMap16Image(IEditor editorInterface, IMemoryAccess rom)
    {
        return new SuperMarioWorldMap16Image(editorInterface, rom);
    }

    public SuperMarioWorldLevelEditor GetLevelEditor(IEditor editorInterface, IMemoryAccess rom)
    {
        return new SuperMarioWorldLevelEditor(editorInterface, rom);
    }
}
