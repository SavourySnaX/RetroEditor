namespace RetroEditor.Plugins
{
    /// <summary>
    /// Interface for a tile map palette - used by the TileMapWidget class
    /// </summary>
    public interface ITilePalette
    {
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
        /// Get the tile palette 
        /// </summary>
        /// <returns>Array of tiles</returns>
        ReadOnlySpan<ITile> FetchTiles();
        
        /// <summary>
        /// Current chosen palette index
        /// </summary>
        int SelectedTile { get; set; }

        /// <summary>
        /// X Scale of the tiles
        /// </summary>
        float ScaleX { get; }
        /// <summary>
        /// Y Scale of the tiles
        /// </summary>
        float ScaleY { get; }

        /// <summary>
        /// Number of tiles per row in the palette
        /// </summary>
        uint TilesPerRow { get; }
    }
}