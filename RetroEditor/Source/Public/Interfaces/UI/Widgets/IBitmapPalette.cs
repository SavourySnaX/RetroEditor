namespace RetroEditor.Plugins
{
    /// <summary>
    /// Interface for a palette - used with the PaletteWidget class
    /// </summary>
    public interface IBitmapPalette
    {
        /// <summary>
        /// Colours to display per row
        /// </summary>
        uint ColoursPerRow{ get; }
        /// <summary>
        /// Width of each colour in pixels
        /// </summary>
        uint Width { get; }
        /// <summary>
        /// Height of each colour in pixels
        /// </summary>
        uint Height { get; }
        /// <summary>
        /// Get the palette
        /// </summary>
        /// <returns>Array of colours</returns>
        ReadOnlySpan<Pixel> GetPalette();

        /// <summary>
        /// Current chosen palette index
        /// </summary>
        int SelectedColour { get; set; }
    }
}