
// Move to plugins folder, this doesn't need to be internal.
namespace RetroEditor.Plugins
{
    /// <summary>
    /// Helper class for rendering a ZX Spectrum 48K screen, this will probably be abstracted out to a more generic class in the future.
    /// </summary>
    /// <remarks>
    /// This is not stored in specutrm screen order, but is a high res bitmap with low res colour.
    /// </remarks>
    public class ZXSpectrum48ImageHelper
    {
        private uint _pixelWidth, _pixelHeight;
        private uint _attributeWidth, _attributeHeight;
        private byte[] _bitmap;
        private byte[] _attributes;
        private Pixel[] _pixels;

        private static readonly Pixel[] _palette = new Pixel[]
        {
            new Pixel(   0,   0,    0 ),
            new Pixel(   0,   0,  192 ),
            new Pixel( 192,   0,    0 ),
            new Pixel( 192,   0,  192 ),
            new Pixel(   0, 192,    0 ),
            new Pixel(   0, 192,  192 ),
            new Pixel( 192, 192,    0 ),
            new Pixel( 192, 192,  192 ),
            new Pixel(   0,   0,    0 ),
            new Pixel(   0,   0,  255 ),
            new Pixel( 255,   0,    0 ),
            new Pixel( 255,   0,  255 ),
            new Pixel(   0, 255,    0 ),
            new Pixel(   0, 255,  255 ),
            new Pixel( 255, 255,    0 ),
            new Pixel( 255, 255,  255 )
        };

        /// <summary>
        /// Create a new ZX Spectrum 48K image helper.
        /// </summary>
        /// <param name="widthPixels">width of image in pixels</param>
        /// <param name="heightPixels">height of image in pixels</param>
        public ZXSpectrum48ImageHelper(uint widthPixels, uint heightPixels)
        {
            _pixelHeight = heightPixels;
            _pixelWidth = widthPixels;
            _attributeHeight = heightPixels / 8;
            _attributeWidth = widthPixels / 8;

            _bitmap = new byte[_pixelWidth * _pixelHeight];
            _attributes = new byte[_attributeWidth * _attributeHeight];
            _pixels = new Pixel[_pixelWidth * _pixelHeight];
        }

        /// <summary>
        /// Clear the image to a specific attribute.
        /// </summary>
        /// <param name="attribute">attribute byte</param>
        public void Clear(byte attribute)
        {
            Array.Fill(_bitmap, (byte)0);
            Array.Fill(_attributes, attribute);
        }

        /// <summary>
        /// Render the image to a pixel array.
        /// </summary>
        /// <param name="seconds">Used to handle the flash attribute</param>
        /// <returns>a Pixel array representing the spectrum image at the time indicated</returns>
        public Pixel[] Render(float seconds)
        {
            var flashSwap = ((int)(seconds * 2.0f) & 1) == 1;

            uint pixelOffset = 0;
            uint attributeOffset = 0;
            for (int y = 0; y < _pixelHeight; y++)
            {
                attributeOffset = (uint)(y >> 3) * _attributeWidth;
                for (int x = 0; x < _attributeWidth; x++)
                {
                    var attribute = _attributes[attributeOffset++];
                    var ink = attribute & 0x07;
                    var paper = (attribute >> 3) & 0x07;
                    var bright = (attribute & 0x40) != 0;
                    var flash = (attribute & 0x80) != 0;

                    if (flash && flashSwap)
                    {
                        var temp = ink;
                        ink = paper;
                        paper = temp;
                    }

                    var inkColour = _palette[ink + (bright ? 8 : 0)];
                    var paperColour = _palette[paper + (bright ? 8 : 0)];

                    for (int b = 0; b < 8; b++)
                    {
                        _pixels[pixelOffset] = _bitmap[pixelOffset] != 0 ? inkColour : paperColour;
                        pixelOffset++;
                    }
                }
            }

            return _pixels;
        }

        /// <summary>
        /// Plot a single pixel onto the screen.
        /// </summary>
        /// <param name="x">x position in pixels</param>
        /// <param name="y">y position in pixels</param>
        /// <param name="ink">If true plots in ink colour, otherwise paper</param>
        public void DrawBitNoAttribute(uint x, uint y, bool ink)
        {
            uint pixelOffset = (y * _pixelWidth) + x;
            _bitmap[pixelOffset] = (byte)(ink ? 1 : 0);
        }

        /// <summary>
        /// Plot a single pixel onto the screen and update the attribute map.
        /// </summary>
        /// <param name="x">x position in pixels</param>
        /// <param name="y">y position in pixels</param>
        /// <param name="ink">If true plots in ink colour, otherwise paper</param>
        /// <param name="attribute"></param>
        public void DrawBit(uint x, uint y, bool ink, byte attribute)
        {
            DrawBitNoAttribute(x, y, ink);
            SetAttribute(ConvertXBitmapPosToYAttribute(x), ConvertYBitmapPosToYAttribute(y), attribute);
        }

        /// <summary>
        /// Get the value of a pixel at a specific location.
        /// </summary>
        /// <param name="x">x position in pixels</param>
        /// <param name="y">y position in pixels</param>
        /// <returns>True if the pixel is ink, false if it is paper</returns>
        public bool GetBit(uint x, uint y)
        {
            uint pixelOffset = (y * _pixelWidth) + x;
            return _bitmap[pixelOffset] != 0;
        }

        /// <summary>
        /// Draws a set of 8 pixels horizontally to the screen ignores paper bits.
        /// </summary>
        /// <param name="x">x position in pixels</param>
        /// <param name="y">y position in pixels</param>
        /// <param name="bits">byte representing 8 pixels, only set bits will update the image</param>
        /// <param name="flipX">if true bits are drawn from right to left.</param>
        public void Draw8BitsNoAttributeInkOnly(uint x, uint y, byte bits, bool flipX)
        {
            uint pixelOffset = (y * _pixelWidth) + x;
            if (flipX)
            {
                for (int px = 0; px < 8; px++)
                {
                    if ((bits & 0x01) != 0)
                    {
                        _bitmap[pixelOffset] = 1;
                    }
                    bits >>= 1;
                    pixelOffset++;
                }
            }
            else
            {
                for (int px = 0; px < 8; px++)
                {
                    if ((bits & 0x80) != 0)
                    {
                        _bitmap[pixelOffset] = 1;
                    }
                    bits <<= 1;
                    pixelOffset++;
                }
            }
        }

        /// <summary>
        /// Draws a set of 8 pixels horizontally to the screen. Bits set will be ink, bits not set will be paper.
        /// </summary>
        /// <param name="x">x position in pixels</param>
        /// <param name="y">y position in pixels</param>
        /// <param name="bits">byte representing 8 pixels</param>
        /// <param name="flipX">if true bits are drawn from right to left.</param>
        public void Draw8BitsNoAttribute(uint x, uint y, byte bits, bool flipX)
        {
            uint pixelOffset = (y * _pixelWidth) + x;
            if (flipX)
            {
                for (int px = 0; px < 8; px++)
                {
                    _bitmap[pixelOffset++] = (byte)((bits & 0x01) != 0 ? 1 : 0);
                    bits >>= 1;
                }
            }
            else
            {
                for (int px = 0; px < 8; px++)
                {
                    _bitmap[pixelOffset++] = (byte)((bits & 0x80) != 0 ? 1 : 0);
                    bits <<= 1;
                }
            }
        }

        /// <summary>
        /// Xor a set of 8 pixels horizontally to the screen. Performs an xor between the screen pixel and the bit in the bits byte.
        /// </summary>
        /// <param name="x">x position in pixels</param>
        /// <param name="y">y position in pixels</param>
        /// <param name="bits">byte representing a set of 8 pixels</param>
        /// <param name="flipX">if true bits are drawn from right to left.</param>
        public void Xor8BitsNoAttribute(uint x, uint y, byte bits, bool flipX)
        {
            uint pixelOffset = (y * _pixelWidth) + x;
            if (flipX)
            {
                for (int px = 0; px < 8; px++)
                {
                    _bitmap[pixelOffset++] ^= (byte)((bits & 0x01) != 0 ? 1 : 0);
                    bits >>= 1;
                }
            }
            else
            {
                for (int px = 0; px < 8; px++)
                {
                    _bitmap[pixelOffset++] ^= (byte)((bits & 0x80) != 0 ? 1 : 0);
                    bits <<= 1;
                }
            }
        }

        /// <summary>
        /// Draws a set of 8 pixels horizontally to the screen. Bits set will be ink, bits not set will be paper.
        /// Attribute will be updated for the 8 pixels drawn.
        /// </summary>
        /// <param name="x">x position in pixels</param>
        /// <param name="y">y position in pixels</param>
        /// <param name="bits">byte representing 8 pixels</param>
        /// <param name="attribute">attribute byte</param>
        /// <param name="flipX">if true bits are drawn from right to left.</param>
        public void Draw8Bits(uint x, uint y, byte bits, byte attribute, bool flipX)
        {
            Draw8BitsNoAttribute(x, y, bits, flipX);
            SetAttribute(ConvertXBitmapPosToYAttribute(x), ConvertYBitmapPosToYAttribute(y), attribute);
            SetAttribute(ConvertXBitmapPosToYAttribute(x+7), ConvertYBitmapPosToYAttribute(y), attribute);
        }

        private void UpdateInkAttributeOnly(uint x, uint y, byte attribute)
        {
            var attrX = ConvertXBitmapPosToYAttribute(x);
            var attrY = ConvertYBitmapPosToYAttribute(y);
            byte currentAttribute = GetAttribute(attrX, attrY);
            currentAttribute &= 0xF8;
            currentAttribute |= (byte)(attribute & 0x07);
            SetAttribute(attrX, attrY, currentAttribute);
        }


        /// <summary>
        /// Draws a set of 8 pixels horizontally to the screen ignores paper bits.
        /// Updates ink attribute only
        /// </summary>
        /// <param name="x">x position in pixels</param>
        /// <param name="y">y position in pixels</param>
        /// <param name="bits">byte representing 8 pixels, only set bits will update the image</param>
        /// <param name="attribute">attribute byte (only ink colour used)</param>
        /// <param name="flipX">if true bits are drawn from right to left.</param>
        public void Draw8BitsInkOnly(uint x, uint y, byte bits, byte attribute, bool flipX)
        {
            Draw8BitsNoAttributeInkOnly(x, y, bits, flipX);
            UpdateInkAttributeOnly(x, y, attribute);
            UpdateInkAttributeOnly(x+7, y, attribute);
        }

        /// <summary>
        /// Xor a set of 8 pixels horizontally to the screen. Performs an xor between the screen pixel and the bit in the bits byte.
        /// Attribute will be updated for the 8 pixels touched.
        /// </summary>
        /// <param name="x">x position in pixels</param>
        /// <param name="y">y position in pixels</param>
        /// <param name="bits">byte representing a set of 8 pixels</param>
        /// <param name="attribute">attribute byte</param>
        /// <param name="flipX">if true bits are drawn from right to left.</param>
        public void Xor8Bits(uint x, uint y, byte bits, byte attribute, bool flipX)
        {
            Xor8BitsNoAttribute(x, y, bits, flipX);
            SetAttribute(ConvertXBitmapPosToYAttribute(x), ConvertYBitmapPosToYAttribute(y), attribute);
            SetAttribute(ConvertXBitmapPosToYAttribute(x+7), ConvertYBitmapPosToYAttribute(y), attribute);
        }

        /// <summary>
        /// Helper function to convert from a pixel y position to an attribute y position.
        /// </summary>
        /// <param name="y">y position in pixels</param>
        /// <returns>y position in attribute cells</returns>
        public uint ConvertYBitmapPosToYAttribute(uint y)
        {
            return y / 8;
        }

        /// <summary>
        /// Helper function to convert from a pixel x position to an attribute x position.
        /// </summary>
        /// <param name="x">x position in pixels</param>
        /// <returns>x position in attribute cells</returns>
        public uint ConvertXBitmapPosToYAttribute(uint x)
        {
            return x / 8;
        }

        /// <summary>
        /// Set the attribute at a specific location.
        /// </summary>
        /// <param name="ax">x position in attribute cells</param>
        /// <param name="ay">y position in attribute cells</param>
        /// <param name="attribute">attribute byte</param>
        public void SetAttribute(uint ax, uint ay, byte attribute)
        {
            uint attributeOffset = ay * _attributeWidth + ax;
            _attributes[attributeOffset] = attribute;
        }

        /// <summary>
        /// Get the attribute at a specific location.
        /// </summary>
        /// <param name="ax">x position in attribute cells</param>
        /// <param name="ay">y position in attribute cells</param>
        /// <returns>attribute byte</returns>
        public byte GetAttribute(uint ax, uint ay)
        {
            uint attributeOffset = ay * _attributeWidth + ax;
            return _attributes[attributeOffset];
        }

        /// <summary>
        /// Copy the contents of specified image to this image.
        /// At present images should be the same size!
        /// </summary>
        /// <param name="source">Source image to copy from</param>
        public void CopyBitmapFrom(ZXSpectrum48ImageHelper source)
        {
            for (int a = 0; a < _bitmap.Length; a++)
            {
                _bitmap[a] = source._bitmap[a];
            }
        }

        /// <summary>
        /// Flip the current image vertically in place. Only affects bitmap.
        /// </summary>
        public void FlipVertical()
        {
            for (int y = 0; y < _pixelHeight / 2; y++)
            {
                for (int x = 0; x < _pixelWidth; x++)
                {
                    var temp = _bitmap[y * _pixelWidth + x];
                    _bitmap[y * _pixelWidth + x] = _bitmap[(_pixelHeight - y - 1) * _pixelWidth + x];
                    _bitmap[(_pixelHeight - y - 1) * _pixelWidth + x] = temp;
                }
            }
        }

        /// <summary>
        /// Draw an 8x8 tile to the screen, this will update the attribute map.
        /// The tile should be an array of 8 bytes. The first byte represents the first row.. etc.
        /// </summary>
        /// <param name="x">x position to draw tile in pixels</param>
        /// <param name="y">y position to draw tile in pixels</param>
        /// <param name="tile">array of 8 bytes representing the tile to draw</param>
        /// <param name="attribute">attribute byte</param>
        public void Draw8x8(uint x, uint y, ReadOnlySpan<byte> tile, byte attribute)
        {
            for (int a = 0; a < 8; a++)
            {
                Draw8Bits(x, (uint)(y + a), tile[a], attribute, false);
            }
        }

        /// <summary>
        /// Draw an 8x8 tile to the screen, ignores paper bits.
        /// The attribute map will be updated with the ink colour only.
        /// </summary>
        /// <param name="x">x position to draw tile in pixels</param>
        /// <param name="y">y position to draw tile in pixels</param>
        /// <param name="tile">array of 8 bytes representing the tile to draw, only set bits are transferred</param>
        /// <param name="attribute"></param>
        public void Draw8x8InkOnly(uint x, uint y, ReadOnlySpan<byte> tile, byte attribute)
        {
            for (int a = 0; a < 8; a++)
            {
                Draw8BitsInkOnly(x, (uint)(y + a), tile[a], attribute, false);
            }
        }

    }
}