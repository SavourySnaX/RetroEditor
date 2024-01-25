/*

 The basis for roms that can be played....

*/

internal class PlayableRom : IRomAccess
{
    private LibRetroPlugin plugin;
    private IEditor editorInterface;
    private bool requiresLoad;

    private byte[] state;

    MemoryblockCollection temporaryBlocksRam;
    MemoryblockCollection serialisedBlocksRam { get; set; }


    public PlayableRom(IEditor editorInterface,LibRetroPlugin plugin, bool requiresLoad)
    {
        this.editorInterface = editorInterface;
        this.plugin = plugin;
        this.requiresLoad = requiresLoad;
        temporaryBlocksRam = new MemoryblockCollection();
        serialisedBlocksRam = new MemoryblockCollection();
        state=Array.Empty<byte>();
    }

    public bool Setup(ProjectSettings settings, string filename, Func<IRomAccess,bool>? autoLoad = null)
    {
        plugin.LoadGame(filename);

        // Load to a defined point (because we are loading from tape)
        if (requiresLoad && autoLoad != null)
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
        plugin.LoadGame(editorInterface.GetRomPath(settings));
        state = editorInterface.LoadState(settings);

        var path=editorInterface.GetEditorDataPath(settings, "Ram");
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            serialisedBlocksRam.Deserialise(json);
        }
        
        return true;
    }

    public void Reset(bool withTemporaryPatches)
    {
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
    }

    public void Serialise(ProjectSettings settings)
    {
        var json = serialisedBlocksRam.Serialise();
        var path=editorInterface.GetEditorDataPath(settings, "Ram");
        File.WriteAllText(path, json);
    }

    public ReadOnlySpan<byte> ReadMemory(MemoryRegion region, uint address, uint length)
    {
        switch (region)
        {
            case MemoryRegion.Rom:
                throw new NotImplementedException($"Rom not done");
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
            throw new Exception("Not implemented");
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
            throw new Exception("Not implemented");
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
                break;
            case WriteKind.TemporaryRom:
                WriteTemporaryMemory(MemoryRegion.Rom, address, bytes.ToArray());
                break;
            case WriteKind.SerialisedRam:
                WriteSerialisedMemory(MemoryRegion.Ram, address, bytes.ToArray());
                break;
            case WriteKind.SerialisedRom:
                WriteSerialisedMemory(MemoryRegion.Rom, address, bytes.ToArray());
                break;
        }
        throw new Exception("Not implemented");
    }
}

public enum MemoryRegion
{
    Rom,        // Applies to cartridge rom (so not useful for tape/disk based systems)
    Ram         // Applies to system ram (usually used for tape/disk based systems)
}