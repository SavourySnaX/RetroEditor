namespace RetroEditor.Plugins
{
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
        ReadOnlySpan<Pixel> GetImageData(float seconds);
    }
}