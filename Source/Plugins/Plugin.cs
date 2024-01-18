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

public interface IRomPlugin
{
    static string? Name { get; }

    public LibRetroPlugin? Initialise(ProjectSettings projectSettings, IEditor editorInterface);

    bool InitialLoad(ProjectSettings settings);
    bool Reload(ProjectSettings settings);
    bool Save(string filename, string kind);

    byte ReadByte(uint address);
    ushort ReadWord(uint address);
    uint ReadLong(uint address);

    void WriteByte(uint address, byte value);

    byte[] ReadBytes(uint address, uint length)
    {
        byte[] result = new byte[length];
        for (uint i = 0; i < length; i++)
        {
            result[i] = ReadByte(address + i);
        }
        return result;
    }

    void WriteBytes(uint address, byte[] bytes)
    {
        for (uint i = 0; i < bytes.Length; i++)
        {
            WriteByte(address + i, bytes[i]);
        }
    }
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
    int GetImageCount();

    IImage GetImage(int mapIndex);
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
    Pixel[] GetMapImage();

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
    int GetMapCount();

    ITileMap GetMap(int mapIndex);
}


public interface IEditor
{
    public IRomPlugin? GetRomInstance(string romKind);
    public LibRetroPlugin? GetLibRetroInstance(string pluginName, ProjectSettings settings);
    public byte[] LoadState(ProjectSettings settings);
    public void SaveState(byte[] state, ProjectSettings settings);
    public string GetRomPath(ProjectSettings settings);
}

public sealed class ProjectSettings
{
    public sealed class SerializedSettings
    {
        public SerializedSettings(string version, string retroCoreName, string retroPluginName)
        {
            Version = version;
            RetroCoreName = retroCoreName;
            RetroPluginName = retroPluginName;
        }
        public string Version { get; set; }
        public string RetroCoreName { get; set; }
        public string RetroPluginName { get; set; }
    }
    
    public ProjectSettings(string projectPath, string retroCoreName, string retroPluginName)
    {
        this.serializedSettings = new SerializedSettings("0.0.1", retroCoreName, retroPluginName);
        this.projectPath = projectPath;
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
    internal readonly string projectPath;
}



public interface IRetroPlugin
{
    /// <summary>
    /// Returns true if the plugin can handle the file
    /// </summary>
    /// <param name="md5">md5 of the inputs</param>
    /// <param name="bytes">bytes of the input - in case plugin wishes to use alternate method of identifying supported file</param>
    /// <param name="filename">filename - in case plugin wishes to use alternate method of identifying supported file</param>
    /// <returns></returns>
    bool CanHandle(string filename);

    // Called when creating project
    bool Init(IEditor editor, ProjectSettings project, out LibRetroPlugin? plugin);

    // Called when opening an existing project
    bool Open(IEditor editorInterface, ProjectSettings projectSettings, out LibRetroPlugin? plugin);

    IImages? GetImageInterface() { return null; }
    ITileMaps? GetTileMapInterface() { return null; }

    public string Name { get; }

    void Close();
}

// Null Plugins
public class NullRomPlugin : IRomPlugin
{
    public LibRetroPlugin? Initialise(ProjectSettings projectSettings, IEditor editorInterface)
    {
        return null;
    }

    public bool InitialLoad(ProjectSettings settings)
    {
        return false;
    }

    public byte ReadByte(uint address)
    {
        return 0;
    }

    public uint ReadLong(uint address)
    {
        return 0;
    }

    public ushort ReadWord(uint address)
    {
        return 0;
    }

    public bool Reload(ProjectSettings settings)
    {
        return false;
    }

    public bool Save(string filename, string kind)
    {
        return false;
    }

    public void WriteByte(uint address, byte value)
    {
    }
}

