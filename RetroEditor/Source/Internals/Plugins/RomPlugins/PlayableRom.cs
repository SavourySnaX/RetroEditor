/*

 The basis for roms that can be played....

*/

using RetroEditor.Plugins;

internal class PlayableRom : IMemoryAccess
{
    public MemoryEndian Endian => romInterface.Endian;

    internal LibRetroPlugin systemPlugin;
    internal IRetroPlugin retroPlugin;
    private IEditorInternal editorInterface;

    private byte[] state;
    internal ISystemPlugin romInterface;

    MemoryblockCollection temporaryBlocksRam;
    MemoryblockCollection temporaryBlocksRom;
    MemoryblockCollection serialisedBlocksRam { get; set; }
    MemoryblockCollection serialisedBlocksRom { get; set; }

    public delegate ReadOnlySpan<byte> CheckSumDelegate(IMemoryAccess rom,out int address);

    public PlayableRom(IEditorInternal editorInterface, LibRetroPlugin plugin, ISystemPlugin romInterface, IRetroPlugin retroPlugin)
    {
        this.romInterface = romInterface;
        this.editorInterface = editorInterface;
        this.systemPlugin = plugin;
        this.retroPlugin = retroPlugin;
        temporaryBlocksRam = new MemoryblockCollection();
        temporaryBlocksRom = new MemoryblockCollection();
        serialisedBlocksRam = new MemoryblockCollection();
        serialisedBlocksRom = new MemoryblockCollection();
        state=Array.Empty<byte>();
    }

    internal void LoadMemoryBlocks(ProjectSettings settings)
    {
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
    }

    public bool Setup(ProjectSettings settings, string filename, Func<IMemoryAccess,bool>? autoLoad = null)
    {
        var romData = File.ReadAllBytes(filename);
        systemPlugin.LoadGame(settings.OriginalRomName, romData);

        // Load to a defined point (because we are loading from tape)
        if (autoLoad != null)
        {
            systemPlugin.AutoLoad(this,autoLoad);
        }

        // We should save snapshot, so we don't need to load from tape again...
        var saveSize = systemPlugin.GetSaveStateSize();
        state = new byte[saveSize];
        systemPlugin.SaveState(state);
        editorInterface.SaveState(state, settings);

        LoadMemoryBlocks(settings); // Since we may be upgrading/changing architecture, so there could be blocks to restore
        return true;
    }

    public bool Reload(ProjectSettings settings)
    {
        var name = editorInterface.GetRomPath(settings);
        var romData = File.ReadAllBytes(name);
        systemPlugin.LoadGame(settings.OriginalRomName, romData);
        state = editorInterface.LoadState(settings);

        LoadMemoryBlocks(settings);

        return true;
    }

    public void Reset(bool withTemporaryPatches)
    {
        if (withTemporaryPatches)
        {
            foreach (var block in temporaryBlocksRom.Blocks)
            {
                systemPlugin.WriteRom(block.address, block.data);
            }
        }

        var chk = romInterface.ChecksumCalculation(this,out var address);
        if (chk.Length > 0)
        {
            systemPlugin.WriteRom((uint)address, chk);
        }
        if (romInterface.RequiresReload)
        {
            systemPlugin.Reload();
        }
        systemPlugin.RestoreState(state);
        if (withTemporaryPatches)
        {
            // Apply any Temporary Memory modifications
            foreach (var block in temporaryBlocksRam.Blocks)
            {
                systemPlugin.SetMemory(block.address, block.data);
            }
        }
        // Apply any Serialised Memory modifications
        foreach (var block in serialisedBlocksRam.Blocks)
        {
            systemPlugin.SetMemory(block.address, block.data);
        }
        foreach (var block in serialisedBlocksRom.Blocks)
        {
            systemPlugin.WriteRom(block.address, block.data);
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
                return systemPlugin.FetchRom(address, length);
            case MemoryRegion.Ram:
                return systemPlugin.GetMemory(address, length);
        }
        return systemPlugin.GetMemory(address, length);
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
            systemPlugin.SetMemory(address, data);
        }
        else
        {
            serialisedBlocksRom.AddRegion(address, data);
            systemPlugin.WriteRom(address, data);
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
        systemPlugin.Close();
    }

    public int RomSize => (int)systemPlugin.RomLength();
}

internal enum MemoryRegion
{
    Rom,        // Applies to cartridge rom (so not useful for tape/disk based systems)
    Ram         // Applies to system ram (usually used for tape/disk based systems)
}