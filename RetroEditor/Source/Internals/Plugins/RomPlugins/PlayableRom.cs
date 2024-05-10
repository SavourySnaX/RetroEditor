/*

 The basis for roms that can be played....

*/

using RetroEditor.Plugins;

internal class PlayableRom : IMemoryAccess
{
    public MemoryEndian Endian => endian;

    private LibRetroPlugin plugin;
    private IEditorInternal editorInterface;

    private byte[] state;
    private MemoryEndian endian;
    private bool needsReload;

    MemoryblockCollection temporaryBlocksRam;
    MemoryblockCollection temporaryBlocksRom;
    MemoryblockCollection serialisedBlocksRam { get; set; }
    MemoryblockCollection serialisedBlocksRom { get; set; }

    public delegate ReadOnlySpan<byte> CheckSumDelegate(IMemoryAccess rom,out int address);

    private CheckSumDelegate checkSumDelegate;

    public PlayableRom(IEditorInternal editorInterface,LibRetroPlugin plugin, MemoryEndian endian, bool needsReload, CheckSumDelegate checkSumDelegate)
    {
        this.endian = endian;
        this.editorInterface = editorInterface;
        this.plugin = plugin;
        this.needsReload = needsReload;
        this.checkSumDelegate = checkSumDelegate;
        temporaryBlocksRam = new MemoryblockCollection();
        temporaryBlocksRom = new MemoryblockCollection();
        serialisedBlocksRam = new MemoryblockCollection();
        serialisedBlocksRom = new MemoryblockCollection();
        state=Array.Empty<byte>();
    }

    public bool Setup(ProjectSettings settings, string filename, Func<IMemoryAccess,bool>? autoLoad = null)
    {
        var romData = File.ReadAllBytes(filename);
        plugin.LoadGame(settings.OriginalRomName, romData);

        // Load to a defined point (because we are loading from tape)
        if (autoLoad != null)
        {
            plugin.AutoLoad(this,autoLoad);
        }

        // We should save snapshot, so we don't need to load from tape again...
        var saveSize = plugin.GetSaveStateSize();
        state = new byte[saveSize];
        plugin.SaveState(state);
        editorInterface.SaveState(state, settings);

        return true;
    }

    public bool Reload(ProjectSettings settings)
    {
        var name = editorInterface.GetRomPath(settings);
        var romData = File.ReadAllBytes(name);
        plugin.LoadGame(settings.OriginalRomName, romData);
        state = editorInterface.LoadState(settings);

        var path=editorInterface.GetEditorDataPath(settings, "Ram");
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            serialisedBlocksRam.Deserialise(json);
        }
        path=editorInterface.GetEditorDataPath(settings, "Rom");
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            serialisedBlocksRom.Deserialise(json);
        }
        
        return true;
    }

    public void Reset(bool withTemporaryPatches)
    {
        if (withTemporaryPatches)
        {
            foreach (var block in temporaryBlocksRom.Blocks)
            {
                plugin.WriteRom(block.address, block.data);
            }
        }

        var chk = checkSumDelegate(this,out var address);
        if (chk.Length > 0)
        {
            plugin.WriteRom((uint)address, chk);
        }
        if (needsReload)
        {
            plugin.Reload();
        }
        plugin.RestoreState(state);
        if (withTemporaryPatches)
        {
            // Apply any Temporary Memory modifications
            foreach (var block in temporaryBlocksRam.Blocks)
            {
                plugin.SetMemory(block.address, block.data);
            }
        }
        // Apply any Serialised Memory modifications
        foreach (var block in serialisedBlocksRam.Blocks)
        {
            plugin.SetMemory(block.address, block.data);
        }
        foreach (var block in serialisedBlocksRom.Blocks)
        {
            plugin.WriteRom(block.address, block.data);
        }
    }

    public void Serialise(ProjectSettings settings)
    {
        var json = serialisedBlocksRam.Serialise();
        var path=editorInterface.GetEditorDataPath(settings, "Ram");
        File.WriteAllText(path, json);
        json = serialisedBlocksRom.Serialise();
        path=editorInterface.GetEditorDataPath(settings, "Rom");
        File.WriteAllText(path, json);
    }

    public void ClearTemporaryMemory()
    {
        temporaryBlocksRam.Clear();
        temporaryBlocksRom.Clear();
    }

    public ReadOnlySpan<byte> ReadMemory(MemoryRegion region, uint address, uint length)
    {
        switch (region)
        {
            case MemoryRegion.Rom:
                return plugin.FetchRom(address, length);
            case MemoryRegion.Ram:
                return plugin.GetMemory(address, length);
        }
        return plugin.GetMemory(address, length);
    }

    public void WriteTemporaryMemory(MemoryRegion region, uint address,ReadOnlySpan<byte> data)
    {
        if (region == MemoryRegion.Ram)
        {
            temporaryBlocksRam.AddRegion(address, data);
        }
        else
        {
            temporaryBlocksRom.AddRegion(address, data);
        }
    }

    public void WriteSerialisedMemory(MemoryRegion region, uint address, ReadOnlySpan<byte> data)
    {
        if (region == MemoryRegion.Ram)
        {
            serialisedBlocksRam.AddRegion(address, data);
            plugin.SetMemory(address, data);
        }
        else
        {
            serialisedBlocksRom.AddRegion(address, data);
            plugin.WriteRom(address, data);
        }
    }

    public ReadOnlySpan<byte> ReadBytes(ReadKind kind, uint address, uint length)
    {
        switch (kind)
        {
            case ReadKind.Ram:
                return ReadMemory(MemoryRegion.Ram, address, length);
            case ReadKind.Rom:
                return ReadMemory(MemoryRegion.Rom, address, length);
        }
        throw new Exception("Not implemented");
    }

    public void WriteBytes(WriteKind kind, uint address, ReadOnlySpan<byte> bytes)
    {
        switch (kind)
        {
            case WriteKind.TemporaryRam:
                WriteTemporaryMemory(MemoryRegion.Ram, address, bytes.ToArray());
                return;
            case WriteKind.TemporaryRom:
                WriteTemporaryMemory(MemoryRegion.Rom, address, bytes.ToArray());
                return;
            case WriteKind.SerialisedRam:
                WriteSerialisedMemory(MemoryRegion.Ram, address, bytes.ToArray());
                return;
            case WriteKind.SerialisedRom:
                WriteSerialisedMemory(MemoryRegion.Rom, address, bytes.ToArray());
                return;
        }
        throw new Exception("Not implemented");
    }

    public void Close()
    {
        plugin.Close();
    }

    public int RomSize => (int)plugin.RomLength();
}

internal enum MemoryRegion
{
    Rom,        // Applies to cartridge rom (so not useful for tape/disk based systems)
    Ram         // Applies to system ram (usually used for tape/disk based systems)
}