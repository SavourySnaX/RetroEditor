namespace RetroEditor.Plugins
{
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
        /// Get the layer data for the specified layer
        /// </summary>
        /// <param name="layer">layer to get tiles for</param>
        /// <returns>Array of tiles</returns>
        ILayer FetchLayer(uint layer);

        /// <summary>
        /// Fetch tile palette for the map
        /// </summary>
        /// <param name="layer">layer to get tiles for</param>
        /// <returns>Tile palette storage</returns>
        TilePaletteStore FetchPalette(uint layer);

        /// <summary>
        /// X Scale of the tiles
        /// </summary>
        float ScaleX { get; }
        /// <summary>
        /// Y Scale of the tiles
        /// </summary>
        float ScaleY { get; }

        // What else do we need - 
        // List of tiles that can be used for this map
        // Screen data (per layer)
        // List of mobile objects
    }
}