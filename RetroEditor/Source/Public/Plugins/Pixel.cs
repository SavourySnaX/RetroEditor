namespace RetroEditor.Plugins
{
    /// <summary>
    /// A pixel in an image, 8:8:8:8 RGBA format
    /// </summary>
    public struct Pixel
    {
        /// <summary>
        /// Create a pixel with the default values
        /// </summary>
        public Pixel()
        {
            Red = 0;
            Green = 0;
            Blue = 0;
            Alpha = 255;
        }

        /// <summary>
        /// Create a pixel with the specified values
        /// </summary>
        /// <param name="r">red value</param>
        /// <param name="g">green value</param>
        /// <param name="b">blue value</param>
        /// <param name="a">alpha value</param>
        public Pixel(byte r, byte g, byte b, byte a = 255)
        {
            Red = r;
            Green = g;
            Blue = b;
            Alpha = a;
        }

        /// <summary>
        /// Red value
        /// </summary>
        public byte Red { get; private set; }
        /// <summary>
        /// Green value
        /// </summary>
        public byte Green { get; private set; }
        /// <summary>
        /// Blue value
        /// </summary>
        public byte Blue { get;  private set;}
        /// <summary>
        /// Alpha value
        /// </summary>
        public byte Alpha { get; private set; }
    }
}