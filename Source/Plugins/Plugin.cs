using System.Text.Json;

public struct Pixel
{
    public Pixel(byte r, byte g, byte b)
    {
        Red = r;
        Green = g;
        Blue = b;
        Alpha = 255;
    }
    public byte Red;
    public byte Green;
    public byte Blue;
    public byte Alpha;
}

public enum ReadKind
{
    Ram=0,
    Rom=1,
}

public enum WriteKind
{
    TemporaryRam=0,
    TemporaryRom=1,
    SerialisedRam=0x1000,
    SerialisedRom=0x1001,
}

public interface ISave
{
    public void Save(string filename);
}

public enum MemoryEndian
{
    Little,
    Big,
}

public interface IRomAccess
{
    public ReadOnlySpan<byte> ReadBytes(ReadKind kind, uint address, uint length);
    public void WriteBytes(WriteKind kind, uint address, ReadOnlySpan<byte> bytes);

    public int RomSize { get; }

    public MemoryEndian Endian {get;}

    public UInt16 FetchMachineOrder16(int offset, ReadOnlySpan<byte> bytes)
    {
        if (Endian == MemoryEndian.Little)
        {
            return (UInt16)(bytes[offset] + (bytes[offset + 1] << 8));
        }
        else
        {
            return (UInt16)((bytes[offset] << 8) + bytes[offset + 1]);
        }
    }
    public UInt32 FetchMachineOrder32(int offset, ReadOnlySpan<byte> bytes)
    {
        if (Endian == MemoryEndian.Little)
        {
            return (UInt32)(bytes[offset] + (bytes[offset + 1] << 8) + (bytes[offset + 2] << 16) + (bytes[offset + 3] << 24));
        }
        else
        {
            return (UInt32)((bytes[offset] << 24) + (bytes[offset + 1] << 16) + (bytes[offset + 2] << 8) + bytes[offset + 3]);
        }
    }
    public void WriteMachineOrder16(int offset, Span<byte> bytes, UInt16 value)
    {
        if (Endian == MemoryEndian.Little)
        {
            bytes[offset] = (byte)(value & 0xff);
            bytes[offset + 1] = (byte)((value >> 8) & 0xff);
        }
        else
        {
            bytes[offset] = (byte)((value >> 8) & 0xff);
            bytes[offset + 1] = (byte)(value & 0xff);
        }
    }
    public void WriteMachineOrder32(int offset, Span<byte> bytes, UInt32 value)
    {
        if (Endian == MemoryEndian.Little)
        {
            bytes[offset] = (byte)(value & 0xff);
            bytes[offset + 1] = (byte)((value >> 8) & 0xff);
            bytes[offset + 2] = (byte)((value >> 16) & 0xff);
            bytes[offset + 3] = (byte)((value >> 24) & 0xff);
        }
        else
        {
            bytes[offset] = (byte)((value >> 24) & 0xff);
            bytes[offset + 1] = (byte)((value >> 16) & 0xff);
            bytes[offset + 2] = (byte)((value >> 8) & 0xff);
            bytes[offset + 3] = (byte)(value & 0xff);
        }
    }

    public UInt16 FetchOppositeMachineOrder16(int offset, ReadOnlySpan<byte> bytes)
    {
        if (Endian != MemoryEndian.Little)
        {
            return (UInt16)(bytes[offset] + (bytes[offset + 1] << 8));
        }
        else
        {
            return (UInt16)((bytes[offset] << 8) + bytes[offset + 1]);
        }
    }
    public UInt32 FetchOppositeMachineOrder32(int offset, ReadOnlySpan<byte> bytes)
    {
        if (Endian != MemoryEndian.Little)
        {
            return (UInt32)(bytes[offset] + (bytes[offset + 1] << 8) + (bytes[offset + 2] << 16) + (bytes[offset + 3] << 24));
        }
        else
        {
            return (UInt32)((bytes[offset] << 24) + (bytes[offset + 1] << 16) + (bytes[offset + 2] << 8) + bytes[offset + 3]);
        }
    }
    public void WriteOppositeMachineOrder16(int offset, Span<byte> bytes, UInt16 value)
    {
        if (Endian != MemoryEndian.Little)
        {
            bytes[offset] = (byte)(value & 0xff);
            bytes[offset + 1] = (byte)((value >> 8) & 0xff);
        }
        else
        {
            bytes[offset] = (byte)((value >> 8) & 0xff);
            bytes[offset + 1] = (byte)(value & 0xff);
        }
    }
    public void WriteOppositeMachineOrder32(int offset, Span<byte> bytes, UInt32 value)
    {
        if (Endian != MemoryEndian.Little)
        {
            bytes[offset] = (byte)(value & 0xff);
            bytes[offset + 1] = (byte)((value >> 8) & 0xff);
            bytes[offset + 2] = (byte)((value >> 16) & 0xff);
            bytes[offset + 3] = (byte)((value >> 24) & 0xff);
        }
        else
        {
            bytes[offset] = (byte)((value >> 24) & 0xff);
            bytes[offset + 1] = (byte)((value >> 16) & 0xff);
            bytes[offset + 2] = (byte)((value >> 8) & 0xff);
            bytes[offset + 3] = (byte)(value & 0xff);
        }
    }
}


public interface IRomPlugin
{
    static abstract string? Name { get; }
    string LibRetroPluginName { get; } 
    bool RequiresReload { get; }

    MemoryEndian Endian { get; }

    public ReadOnlySpan<byte> ChecksumCalculation(IRomAccess rom,out int address) { address = 0; return ReadOnlySpan<byte>.Empty; }
}

public interface IImage
{
    uint Width { get; }
    uint Height { get; }
    string Name { get; }

    Pixel[] GetImageData(float seconds);
}

public interface IImages
{
    int GetImageCount(IRomAccess rom);

    IImage GetImage(IRomAccess rom, int mapIndex);
}

public interface ITile
{
    uint Width { get; }
    uint Height { get; }
    string Name { get; }

    Pixel[] GetImageData();
}

public interface ILayer
{
    uint Width { get; }
    uint Height { get; }

     uint[] GetMapData();

    void SetTile(uint x, uint y, uint tile);
}

public interface ITileMap
{
    uint Width { get; }     // Size in pixels of the map
    uint Height { get; }
    string Name { get; }

    uint NumLayers { get; } // maybe layer could be abstract, e.g. sprites or tiles

    uint MaxTiles { get; }  // Max number of tiles to select from

    void Update(float deltaTime);

    void Close();

    ITile[] FetchTiles(uint layer); // Tile palette for layer

    ILayer FetchLayer(uint layer);
    // What else do we need - 
    // List of tiles that can be used for this map
    // Screen data (per layer)
    // List of mobile objects
}

public interface ITileMaps
{
    int GetMapCount(IRomAccess rom);

    ITileMap GetMap(IRomAccess rom, int mapIndex);
}


public interface IEditor
{
    public LibRetroPlugin? GetLibRetroInstance(string pluginName, ProjectSettings settings);
    public byte[] LoadState(ProjectSettings settings);
    public void SaveState(byte[] state, ProjectSettings settings);
    public string GetRomPath(ProjectSettings settings);
    public string GetEditorDataPath(ProjectSettings settings, string name);

    public void OpenWindow(IWindow window, string name);
    public void CloseWindow(string name);
}

public sealed class ProjectSettings
{
    public sealed class SerializedSettings
    {
        public SerializedSettings(string version, string retroCoreName, string retroPluginName, string originalRomName)
        {
            Version = version;
            RetroCoreName = retroCoreName;
            RetroPluginName = retroPluginName;
            OriginalRomName = originalRomName;
        }
        public string Version { get; private set; }
        public string RetroCoreName { get; private set; }
        public string RetroPluginName { get; private set; }
        public string OriginalRomName { get; private set; }
    }
    
    public ProjectSettings(string projectName, string projectPath, string retroCoreName, string retroPluginName, string originalRomName)
    {
        this.serializedSettings = new SerializedSettings("0.0.1", retroCoreName, retroPluginName, originalRomName);
        this.projectPath = projectPath;
        this.projectName = projectName;
    }

    internal void Save(string projectFile)
    {
        var json = JsonSerializer.Serialize(serializedSettings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(projectFile, json);

    }
    internal void Load(string projectFile)
    {
        var temp = JsonSerializer.Deserialize<SerializedSettings>(File.ReadAllText(projectFile));
        if (temp != null)
        {
            if (temp.Version != serializedSettings.Version)
            {
                // TODO - upgrade
            }
            serializedSettings = temp;
        }
    }

    internal SerializedSettings serializedSettings;

    public string Version => serializedSettings.Version;
    public string RetroCoreName => serializedSettings.RetroCoreName;
    public string RetroPluginName => serializedSettings.RetroPluginName;
    public string OriginalRomName => serializedSettings.OriginalRomName;
    internal readonly string projectName;
    internal readonly string projectPath;
}



public interface IRetroPlugin
{
    static abstract string? Name { get; }

    string RomPluginName { get; }

    bool RequiresAutoLoad { get; }

    bool CanHandle(string filename);    //TODO at present you cant use this to record which rom was actually loaded, since its loaded into a different instance

    void Menu(IRomAccess rom,IEditor editorInterface);

    IImages? GetImageInterface() { return null; }
    ITileMaps? GetTileMapInterface() { return null; }


    void Close();

    // Rom/Ram handling


    bool AutoLoadCondition(IRomAccess romAccess);

    void SetupGameTemporaryPatches(IRomAccess romAccess);

    ISave Export(IRomAccess romAcess);

}
