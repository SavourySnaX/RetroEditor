namespace RetroEditor.Plugins
{
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
        ReadOnlySpan<uint> GetMapData();

        /// <summary>
        /// Called when a tile is set in the editor, this should be used to make the change in the games memory
        /// </summary>
        /// <param name="x">x offset of modified tile in tiles</param>
        /// <param name="y">y offset of modified tile in tiles</param>
        /// <param name="tile">tile index</param>
        void SetTile(uint x, uint y, uint tile);
    }
}