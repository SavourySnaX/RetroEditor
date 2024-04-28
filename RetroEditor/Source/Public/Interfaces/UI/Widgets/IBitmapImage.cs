namespace RetroEditor.Plugins
{
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
        /// Width of a pixel in pixels
        /// </summary>
        uint PixelWidth { get; }
        /// <summary>
        /// Height of a pixel in pixels
        /// </summary>
        uint PixelHeight { get; }

        /// <summary>
        /// Palette for the image
        /// </summary>
        IBitmapPalette Palette { get; }

        /// <summary>
        /// Image data as a flat array of palette indices
        /// </summary>
        /// <param name="seconds">Number of seconds since startup</param>
        /// <returns>pixel index array</returns>
        ReadOnlySpan<uint> GetImageData(float seconds);

        /// <summary>
        /// Set a pixel in the image
        /// Is called when a pixel is set in the editor, this should be used to make the change in the games memory
        /// </summary>
        void SetPixel(uint x, uint y, uint paletteIndex);
    }
}