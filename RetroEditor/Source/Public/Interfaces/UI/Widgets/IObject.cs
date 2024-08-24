
namespace RetroEditor.Plugins
{
    /// <summary>
    /// Represents an interface for UI widgets that defines basic properties and methods.
    /// </summary>
    public interface IObject
    {
        /// <summary>
        /// Gets the width of the object.
        /// </summary>
        uint Width { get; }

        /// <summary>
        /// Gets the height of the object.
        /// </summary>
        uint Height { get; }

        /// <summary>
        /// Gets the X-coordinate of the object.
        /// </summary>
        uint X { get; }

        /// <summary>
        /// Gets the Y-coordinate of the object.
        /// </summary>
        uint Y { get; }

        /// <summary>
        /// Gets the name of the object.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Get the map data for the layer
        /// </summary>
        /// <returns>Flat array of tile indices</returns>
        ReadOnlySpan<uint> GetMapData();
    }
}