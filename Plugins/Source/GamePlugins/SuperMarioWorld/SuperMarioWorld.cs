using System.IO;
using System.Security.Cryptography;
using System.Linq;

using RetroEditor.Plugins;
using System;

using RetroEditorPlugin_SuperMarioWorld;

using SuperNintendoEntertainmentSystem.Memory;
public class SuperMarioWorld : IRetroPlugin, IMenuProvider, ISave
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

    ExportMemoryAccess _exportedData;
    private struct ExportMemoryAccess : IMemoryAccess
    {
        private byte[] _exportedData;
        public ExportMemoryAccess(int size)
        {
            _exportedData = new byte[size];
            Array.Fill<byte>(_exportedData, 0xFF);
        }

        public int RomSize => _exportedData.Length;

        public MemoryEndian Endian => MemoryEndian.Little;

        public ReadOnlySpan<byte> ReadBytes(ReadKind kind, uint address, uint length)
        {
            return new ReadOnlySpan<byte>(_exportedData).Slice((int)address, (int)length);
        }

        public void WriteBytes(WriteKind kind, uint address, ReadOnlySpan<byte> bytes)
        {
            Array.Copy(bytes.ToArray(), 0, _exportedData, (int)address, bytes.Length);
        }
    }

    public ISave Export(IMemoryAccess memoryAccess)
    {
        // Lets attempt a test export
        //
        // Compute checksum of ROM
        UInt16 checksum = 0;
        _exportedData = new ExportMemoryAccess(memoryAccess.RomSize * 2);
        var allBytes = memoryAccess.ReadBytes(ReadKind.Rom, 0, (uint)memoryAccess.RomSize);
        _exportedData.WriteBytes(WriteKind.SerialisedRom, 0, allBytes);
        // Modify header rom size
        var oldHeaderSize = _exportedData.ReadBytes(ReadKind.Rom, 0x7FD7, 1)[0];
        oldHeaderSize++;
        _exportedData.WriteBytes(WriteKind.SerialisedRom, 0x7FD7, new byte[] { oldHeaderSize });

        // Save level modifications

        var levelSelect = 199u;
        var addressTranslation = new LoRom(false, false);
        var smwRom = new SuperMarioWorldRomHelpers(memoryAccess, addressTranslation, levelSelect);


        // Compute checksum and re-record

        for (uint a = 0; a < _exportedData.RomSize; a++)
        {
            checksum += _exportedData.ReadBytes(ReadKind.Rom, a, 1)[0];
        }
        // Write complement and actual checksum values to rom
        var complement = new byte[2];
        var check = new byte[2];
        memoryAccess.WriteMachineOrder16(0, complement, (UInt16)~checksum);
        memoryAccess.WriteMachineOrder16(0, check, checksum);

        _exportedData.WriteBytes(WriteKind.SerialisedRom, 0x7FDC, complement);
        _exportedData.WriteBytes(WriteKind.SerialisedRam, 0x7FDE, check);

        return this;
    }

    public void Save(string path)
    {
        File.WriteAllBytes(path, _exportedData.ReadBytes(ReadKind.Rom, 0, (uint)_exportedData.RomSize).ToArray());
        _exportedData = new ExportMemoryAccess(0);
    }

    public void ConfigureMenu(IMemoryAccess rom, IMenu menu)
    {
        menu.AddItem("Level",
            (editorInterface, menuItem) =>
            {
                editorInterface.OpenUserWindow($"Level View", GetImage(editorInterface, rom));
            });
        menu.AddItem("GFX",
            (editorInterface, menuItem) =>
            {
                editorInterface.OpenUserWindow($"GFX View", GetGFXPageImage(editorInterface, rom));
            });
        menu.AddItem("VRAM",
            (editorInterface, menuItem) =>
            {
                editorInterface.OpenUserWindow($"VRAM View", GetVRAMImage(editorInterface, rom));
            });
        menu.AddItem("Palette",
            (editorInterface, menuItem) =>
            {
                editorInterface.OpenUserWindow($"Palette View", GetPaletteImage(editorInterface, rom));
            });
        menu.AddItem("Map 16",
            (editorInterface, menuItem) =>
            {
                editorInterface.OpenUserWindow($"Map16 View", GetMap16Image(editorInterface, rom));
            });
        menu.AddItem("Level Editor",
            (editorInterface, menuItem) =>
            {
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
