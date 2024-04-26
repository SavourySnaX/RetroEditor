using System.Text.Json;

namespace RetroEditor.Plugins
{

    /// <summary>
    /// A pixel in an image, 8:8:8:8 RGBA format
    /// </summary>
    public struct Pixel
    {
        /// <summary>
        /// Create a pixel with the default values
        /// </summary>
        public Pixel()
        {
            Red = 0;
            Green = 0;
            Blue = 0;
            Alpha = 255;
        }

        /// <summary>
        /// Create a pixel with the specified values
        /// </summary>
        /// <param name="r">red value</param>
        /// <param name="g">green value</param>
        /// <param name="b">blue value</param>
        /// <param name="a">alpha value</param>
        public Pixel(byte r, byte g, byte b, byte a = 255)
        {
            Red = r;
            Green = g;
            Blue = b;
            Alpha = a;
        }

        /// <summary>
        /// Red value
        /// </summary>
        public byte Red { get; private set; }
        /// <summary>
        /// Green value
        /// </summary>
        public byte Green { get; private set; }
        /// <summary>
        /// Blue value
        /// </summary>
        public byte Blue { get;  private set;}
        /// <summary>
        /// Alpha value
        /// </summary>
        public byte Alpha { get; private set; }
    }

    /// <summary>
    /// Memory region to read from
    /// </summary>
    public enum ReadKind
    {
        /// <summary>
        /// Read from RAM
        /// </summary>
        Ram = 0,
        /// <summary>
        /// Read from ROM
        /// </summary>
        Rom = 1,
    }

    /// <summary>
    /// Memory region to write to
    /// </summary>
    public enum WriteKind
    {
        /// <summary>
        /// Write to RAM (does not persist to final modded game - use for cheats, etc)
        /// </summary>
        TemporaryRam = 0,
        /// <summary>
        /// Write to ROM (does not persist to final modded game - use for cheats, etc)
        /// </summary>
        TemporaryRom = 1,
        /// <summary>
        /// Write to RAM (persists to final modded game - assuming ram area saved to disk)
        /// </summary>
        SerialisedRam = 0x1000,
        /// <summary>
        /// Write to ROM (persists to final modded game - assuming rom area saved to disk)
        /// </summary>
        SerialisedRom = 0x1001,
    }

    /// <summary>
    /// Interface for saving data, intended to be used to generate the final modded game
    /// At present only implemented by ZXSpectrumTape
    /// </summary>
    public interface ISave
    {
        /// <summary>
        /// Save the data to a file
        /// </summary>
        /// <param name="path">path to save file</param>
        public void Save(string path);
    }

    /// <summary>
    /// Endianess of memory - used to determine how to read/write multi-byte values
    /// </summary>
    public enum MemoryEndian
    {
        /// <summary>
        /// Little endian - least significant byte first
        /// </summary>
        Little,
        /// <summary>
        /// Big endian - most significant byte first
        /// </summary>
        Big,
    }

    /// <summary>
    /// Interface for accessing memory
    /// </summary>
    public interface IMemoryAccess
    {
        /// <summary>
        /// Read a number of bytes from the specified memory area
        /// </summary>
        /// <param name="kind">Memory kind</param>
        /// <param name="address">Address of first byte</param>
        /// <param name="length">Number of bytes to retrieve</param>
        /// <returns></returns>
        public ReadOnlySpan<byte> ReadBytes(ReadKind kind, uint address, uint length);
        /// <summary>
        /// Write a number of bytes to the specified memory area
        /// </summary>
        /// <param name="kind">Memory kind</param>
        /// <param name="address">Address to write to</param>
        /// <param name="bytes">Bytes to write to address</param>
        public void WriteBytes(WriteKind kind, uint address, ReadOnlySpan<byte> bytes);

        /// <summary>
        /// Size of the loaded rom (only applies to cartridge based systems)
        /// </summary>
        /// <returns>Size of rom in bytes</returns>
        public int RomSize { get; }

        /// <summary>
        /// Memory Endian of the system
        /// </summary>
        /// <returns>Endianess of memory</returns>
        public MemoryEndian Endian { get; }

        /// <summary>
        /// Given a byte array and offset, returns a 16 bit value in machine order
        /// </summary>
        /// <param name="offset">offset in bytes</param>
        /// <param name="bytes">array of bytes</param>
        /// <returns>16 bit value from requested offset</returns>
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

        /// <summary>
        /// Given a byte array and offset, returns a 32 bit value in machine order
        /// </summary>
        /// <param name="offset">offset in bytes</param>
        /// <param name="bytes">array of bytes</param>
        /// <returns>32 bit value from requested offset</returns>
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

        /// <summary>
        /// Given a byte array and offset, writes a 16bit value in machine order
        /// </summary>
        /// <param name="offset">offset in bytes</param>
        /// <param name="bytes">array of bytes</param>
        /// <param name="value">value to write</param>
        /// <returns>16 bit value from requested offset</returns>
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

        /// <summary>
        /// Given a byte array and offset, writes a 32bit value in machine order
        /// </summary>
        /// <param name="offset">offset in bytes</param>
        /// <param name="bytes">array of bytes</param>
        /// <param name="value">value to write</param>
        /// <returns>32 bit value from requested offset</returns>
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

        /// <summary>
        /// Given a byte array and offset, returns a 16 bit value in opposite machine order
        /// </summary>
        /// <param name="offset">offset in bytes</param>
        /// <param name="bytes">array of bytes</param>
        /// <returns>16 bit value from requested offset</returns>
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

        /// <summary>
        /// Given a byte array and offset, returns a 32 bit value in opposite machine order
        /// </summary>
        /// <param name="offset">offset in bytes</param>
        /// <param name="bytes">array of bytes</param>
        /// <returns>32 bit value from requested offset</returns>
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

        /// <summary>
        /// Given a byte array and offset, writes a 16bit value in opposite machine order
        /// </summary>
        /// <param name="offset">offset in bytes</param>
        /// <param name="bytes">array of bytes</param>
        /// <param name="value">value to write</param>
        /// <returns>16 bit value from requested offset</returns>
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

        /// <summary>
        /// Given a byte array and offset, writes a 32bit value in opposite machine order
        /// </summary>
        /// <param name="offset">offset in bytes</param>
        /// <param name="bytes">array of bytes</param>
        /// <param name="value">value to write</param>
        /// <returns>32 bit value from requested offset</returns>
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

    /// <summary>
    /// Interface for a bitmap image - used with the BitmapWidget class
    /// </summary>
    public interface IBitmapImage
    {
        /// <summary>
        /// Width of the image in pixels
        /// </summary>
        uint Width { get; }
        /// <summary>
        /// Height of the image in pixels
        /// </summary>
        uint Height { get; }

        /// <summary>
        /// Array representing the palette of the image.
        /// </summary>
        Pixel[] Palette { get; }

        /// <summary>
        /// Image data as a flat array of palette indices
        /// </summary>
        /// <param name="seconds">Number of seconds since startup</param>
        /// <returns>pixel index array</returns>
        uint[] GetImageData(float seconds);

        /// <summary>
        /// Set a pixel in the image
        /// Is called when a pixel is set in the editor, this should be used to make the change in the games memory
        /// </summary>
        void SetPixel(uint x, uint y, uint paletteIndex);
    }

    /// <summary>
    /// Defines a system plugin (e.g. Megadrive, NES, etc)
    /// </summary>
    public interface ISystemPlugin
    {
        /// <summary>
        /// Name of the system, used in IRetroPlugin to determine which system is needed for the particular game
        /// </summary>
        static abstract string Name { get; }
        /// <summary>
        /// Name of the libretro core plugin - used to retrieve and load the correct core
        /// </summary>
        string LibRetroPluginName { get; }
        /// <summary>
        /// Whether the system requires a reload of the core when the rom data is changed.
        /// In general cartridge based systems require a reload
        /// </summary>
        bool RequiresReload { get; }

        /// <summary>
        /// Memory Endianess of the system
        /// </summary>
        MemoryEndian Endian { get; }

        /// <summary>
        /// Checksum calculation for the system - used to recompute the checksum for systems that require it
        /// </summary>
        /// <param name="rom">Memory accessor</param>
        /// <param name="address">Offset to apply checksum to</param>
        /// <returns>Checksum bytes</returns>
        public ReadOnlySpan<byte> ChecksumCalculation(IMemoryAccess rom, out int address) { address = 0; return ReadOnlySpan<byte>.Empty; }
    }

    /// <summary>
    /// Interface for an image - used by the ImageView class
    /// </summary>
    public interface IImage
    {
        /// <summary>
        /// Width of the image in pixels
        /// </summary>
        uint Width { get; }
        /// <summary>
        /// Height of the image in pixels
        /// </summary>
        uint Height { get; }
        /// <summary>
        /// Amount to scale image by in X
        /// </summary>
        float ScaleX { get; }
        /// <summary>
        /// Amount to scale image by in Y
        /// </summary>
        float ScaleY { get; }

        /// <summary>
        /// Get the image data for the current time
        /// </summary>
        /// <param name="seconds">Time since editor started</param>
        /// <returns>Flat array of Pixel values representing the image</returns>
        Pixel[] GetImageData(float seconds);
    }

    /// <summary>
    /// Interface for a tile - used by the TileMapWidget class
    /// </summary>
    public interface ITile
    {
        /// <summary>
        /// Width of the tile in pixels
        /// </summary>
        uint Width { get; }
        /// <summary>
        /// Height of the tile in pixels
        /// </summary>
        uint Height { get; }
        /// <summary>
        /// Name of the tile - displayed in the tile palette
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Get the image data for the tile
        /// </summary>
        /// <returns>Flat array of Pixel values representing the image</returns>
        Pixel[] GetImageData();
    }

    /// <summary>
    /// Interface for a layer in a tile map
    /// </summary>
    public interface ILayer
    {
        /// <summary>
        /// Width of the layer in tiles
        /// </summary>
        uint Width { get; }
        /// <summary>
        /// Height of the layer in tiles
        /// </summary>
        uint Height { get; }

        /// <summary>
        /// Get the map data for the layer
        /// </summary>
        /// <returns>Flat array of tile indices</returns>
        uint[] GetMapData();

        /// <summary>
        /// Called when a tile is set in the editor, this should be used to make the change in the games memory
        /// </summary>
        /// <param name="x">x offset of modified tile in tiles</param>
        /// <param name="y">y offset of modified tile in tiles</param>
        /// <param name="tile">tile index</param>
        void SetTile(uint x, uint y, uint tile);
    }

    /// <summary>
    /// Interface for a tile map - used by the TileMapWidget class
    /// </summary>
    public interface ITileMap
    {
        /// <summary>
        /// Width in pixels of the map
        /// </summary>
        uint Width { get; }
        /// <summary>
        /// Height in pixels of the map
        /// </summary>
        uint Height { get; }

        /// <summary>
        /// Number of layers in the map
        /// </summary>
        uint NumLayers { get; }

        /// <summary>
        /// Maximum number of tiles that can be selected from
        /// </summary>
        uint MaxTiles { get; }

        /// <summary>
        /// Can be used to refresh the tile map graphics
        /// </summary>
        /// <param name="seconds">seconds since editor started</param>
        void Update(float seconds);

        /// <summary>
        /// Get the tile palette for the specified layer
        /// </summary>
        /// <param name="layer">layer to get tiles for</param>
        /// <returns>Array of tiles</returns>
        ITile[] FetchTiles(uint layer);

        /// <summary>
        /// Get the layer data for the specified layer
        /// </summary>
        /// <param name="layer">layer to get tiles for</param>
        /// <returns>Array of tiles</returns>
        ILayer FetchLayer(uint layer);


        // What else do we need - 
        // List of tiles that can be used for this map
        // Screen data (per layer)
        // List of mobile objects
    }

    /// <summary>
    /// Editor interface - used to interact with the editor
    /// </summary>
    public interface IEditor
    {
        /// <summary>
        /// Create a new window in the editor
        /// </summary>
        /// <param name="name">Name of the window</param>
        /// <param name="window">Objet implementing IUserWindow</param>
        public void OpenUserWindow(string name, IUserWindow window);
    }

    internal interface IEditorInternal
    {
        public byte[] LoadState(ProjectSettings settings);
        public void SaveState(byte[] state, ProjectSettings settings);
        public string GetRomPath(ProjectSettings settings);
        public string GetEditorDataPath(ProjectSettings settings, string name);

        public void OpenWindow(IWindow window, string name);
        public void CloseWindow(string name);

    }

    internal sealed class ProjectSettings
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
            this.serializedSettings = new SerializedSettings(Editor.EditorSettings.CurrentVersion, retroCoreName, retroPluginName, originalRomName);
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

    /// <summary>
    /// Interface for libretro emulator player
    /// </summary>
    public interface IPlayerControls
    {
        /// <summary>
        /// Reset the emulator
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// The primary interface to implement when developing a new plugin
    /// </summary>
    public interface IRetroPlugin
    {

        /// <summary>
        /// Name of the plugin
        /// </summary>
        static abstract string Name { get; }

        /// <summary>
        /// Name of the rom plugin required for this game
        /// </summary>
        string RomPluginName { get; }

        /// <summary>
        /// Does this game require loading to be skipped
        /// For instance games that are loaded from tape, should return true here
        /// and then implement the AutoLoadCondition method to determine when the loading is complete
        /// </summary>
        bool RequiresAutoLoad { get; }

        /// <summary>
        /// This is called when a game is opened, to determine which plugin can handle it
        /// </summary>
        /// <param name="path">Path of file to check</param>
        /// <returns></returns>
        /// <remarks>
        /// At present this cannot be used to determine the specific copy of the game that was loaded, as the plugin is recreated after this call
        /// </remarks>
        bool CanHandle(string path);

        /// <summary>
        /// If auto load is required, this method should determine when the loading is complete
        /// </summary>
        /// <param name="romAccess">memory interface</param>
        /// <returns>true if condition met, else false</returns>
        bool AutoLoadCondition(IMemoryAccess romAccess);

        /// <summary>
        /// This is called to allow initial patches to be applied to the game being editing,
        /// for instance to allow cheats to be applied, or to skip loading screens
        /// </summary>
        /// <param name="romAccess">memory interface</param>
        void SetupGameTemporaryPatches(IMemoryAccess romAccess);

        /// <summary>
        /// This is called when an export is required, it should return a save object that can be used to generate the final modded game
        /// </summary>
        /// <param name="romAcess">memory interface</param>
        /// <returns>Saveable object</returns>
        ISave Export(IMemoryAccess romAcess);

    }
}