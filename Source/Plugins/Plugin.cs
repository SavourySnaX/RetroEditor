
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

    bool Load(byte[] bytes, string kind);
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
    bool CanHandle(byte[] md5, byte[] bytes, string filename);

    // for now just build all the plugin functionality into here, split it later
    bool Init(IEditor editor,byte[] md5, byte[] bytes, string filename);

    IImages? GetImageInterface() { return null; }
    ITileMaps? GetTileMapInterface() { return null; }

    public string Name { get; }

    void Close();
}

// Null Plugins
public class NullRomPlugin : IRomPlugin
{
    public bool Load(byte[] bytes, string kind)
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

    public bool Save(string filename, string kind)
    {
        return false;
    }

    public void WriteByte(uint address, byte value)
    {
    }
}

