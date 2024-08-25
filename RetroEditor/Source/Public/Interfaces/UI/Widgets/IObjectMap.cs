namespace RetroEditor.Plugins
{
    /// <summary>
    /// Interface for an object map - used by the ObjectMapWidget class
    /// </summary>
    public interface IObjectMap
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
        /// X Scale of the objects
        /// </summary>
        float ScaleX { get; }
        /// <summary>
        /// Y Scale of the objects
        /// </summary>
        float ScaleY { get; }

        /// <summary>
        /// Fetch tile palette for the map
        /// </summary>
        /// <returns>Tile palette storage</returns>
        TilePaletteStore FetchPalette();

        /// <summary>
        /// Fetch the objects for the map (they will be rendered in the order they are returned)
        /// </summary>
        IEnumerable<IObject> FetchObjects { get; }

        /// <summary>
        /// Called when an object is moved, allows the map to adjust the object position
        /// </summary>
        /// <param name="obj">Object being moved</param>
        /// <param name="x">X position</param>
        /// <param name="y">Y position</param>
        void ObjectMove(IObject obj, uint x, uint y);
    }
}