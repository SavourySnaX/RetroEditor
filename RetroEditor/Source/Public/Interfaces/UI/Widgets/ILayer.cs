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

    /// <summary>
    /// Flags to indicate the flip state of a tile in a layer
    /// </summary>
    [Flags]
    public enum FlipState : byte
    {
        /// <summary>
        /// No flip state
        /// </summary>
        None = 0,
        /// <summary>
        /// Flip the tile horizontally
        /// </summary>
        X = 1 << 0,
        /// <summary>
        /// Flip the tile vertically
        /// </summary>
        Y = 1 << 1,
        /// <summary>
        /// Flip the tile both horizontally and vertically
        /// </summary>
        XY = X | Y
    }

    /// <summary>
    /// Interface for a layer that supports flipping tiles
    /// </summary>
    public interface ILayerWithFlip : ILayer
    {
        /// <summary>
        /// Get the flip data for the layer
        /// </summary>
        /// <returns>Flat array of flip states for each tile</returns>
        ReadOnlySpan<FlipState> GetFlipData();
    }
}