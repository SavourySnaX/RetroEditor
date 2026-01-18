using RetroEditor.Plugins;

namespace SuperNintendoEntertainmentSystem.Graphics
{
    /// <summary>
    /// SNES Colour conversion utilities
    /// </summary>
    public static class Colour
    {
        /// <summary>
        /// Converts a SNES color value to a Pixel.
        /// </summary>
        /// <param name="pixel">The Pixel to set the color for.</param>
        /// <param name="c">The SNES color value.</param>
        public static void FromSNES(this ref Pixel pixel, ushort c)
        {
            var r = ((c & 0x1F) << 3) | ((c & 1) != 0 ? 7 : 0);
            var g = (((c >> 5) & 0x1F) << 3) | ((c & 0x20) != 0 ? 7 : 0);
            var b = (((c >> 10) & 0x1F) << 3) | ((c & 0x400) != 0 ? 7 : 0);
            pixel = new Pixel((byte)r, (byte)g, (byte)b, 255);
        }

        /// <summary>
        /// Converts a Pixel to a SNES color value.
        /// </summary>
        /// <param name="pixel">The Pixel to convert.</param>
        /// <returns>The SNES color value.</returns>
        public static ushort ToSNES(this Pixel pixel)
        {
            var r = (pixel.Red >> 3) & 0x1F;
            var g = (pixel.Green >> 3) & 0x1F;
            var b = (pixel.Blue >> 3) & 0x1F;
            return (ushort)((r) | (g << 5) | (b << 10));
        }

    }
}