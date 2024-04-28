namespace RetroEditor.Plugins
{
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
}