
//Note not stored in spectrum screen order, but is a high res bitmap with low res colour - todo abstract this out for other systems with similar modes>

public class ZXSpectrum48ImageHelper
{
    uint pixelWidth, pixelHeight;
    uint attributeWidth, attributeHeight;
    byte[] bitmap;
    byte[] attributes;
    Pixel[] pixels;

    static readonly Pixel[] palette = new Pixel[]
    {
        new Pixel() { Red = 0, Green = 0, Blue = 0 },
        new Pixel() { Red = 0, Green = 0, Blue = 192 },
        new Pixel() { Red = 192, Green = 0, Blue = 0 },
        new Pixel() { Red = 192, Green = 0, Blue = 192 },
        new Pixel() { Red = 0, Green = 192, Blue = 0 },
        new Pixel() { Red = 0, Green = 192, Blue = 192 },
        new Pixel() { Red = 192, Green = 192, Blue = 0 },
        new Pixel() { Red = 192, Green = 192, Blue = 192 },
        new Pixel() { Red = 0, Green = 0, Blue = 0 },
        new Pixel() { Red = 0, Green = 0, Blue = 255 },
        new Pixel() { Red = 255, Green = 0, Blue = 0 },
        new Pixel() { Red = 255, Green = 0, Blue = 255 },
        new Pixel() { Red = 0, Green = 255, Blue = 0 },
        new Pixel() { Red = 0, Green = 255, Blue = 255 },
        new Pixel() { Red = 255, Green = 255, Blue = 0 },
        new Pixel() { Red = 255, Green = 255, Blue = 255 }
    };

    public ZXSpectrum48ImageHelper(uint widthPixels, uint heightPixels)
    {
        pixelHeight = heightPixels;
        pixelWidth = widthPixels;
        attributeHeight = heightPixels / 8;
        attributeWidth = widthPixels / 8;

        bitmap = new byte[pixelWidth * pixelHeight];
        attributes = new byte[attributeWidth * attributeHeight];
        pixels = new Pixel[pixelWidth * pixelHeight];
    }

    public void Clear(byte attribute)
    {
        Array.Fill(bitmap, (byte)0);
        Array.Fill(attributes, attribute);
    }

    public Pixel[] Render(float seconds)
    {
        var flashSwap = ((int)(seconds*2.0f)&1)==1;

        uint pixelOffset = 0;
        uint attributeOffset = 0;
        for (int y=0;y<pixelHeight;y++)
        {
            attributeOffset = (uint)(y>>3) * attributeWidth;
            for (int x=0;x<attributeWidth;x++)
            {
                var attribute = attributes[attributeOffset++];
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

                var inkColour = palette[ink+(bright?8:0)];
                var paperColour = palette[paper+(bright?8:0)];

                for (int b=0;b<8;b++)
                {
                    pixels[pixelOffset] = bitmap[pixelOffset] != 0 ? inkColour : paperColour;
                    pixelOffset++;
                }
            }
        }

        return pixels;
    }

    public void DrawBitNoAttribute(uint x, uint y, bool ink)
    {
        uint pixelOffset = (y * pixelWidth) + x;
        bitmap[pixelOffset] = (byte)(ink ? 1 : 0);
    }

    public void DrawBit(uint x, uint y, bool ink, byte attribute)
    {
        DrawBitNoAttribute(x, y, ink);
        uint attributeOffset = ((y / 8) * attributeWidth) + (x / 8);
        attributes[attributeOffset] = attribute;
    }

    public void Draw8BitsNoAttribute(uint x, uint y, byte bits, bool flipX)
    {
        uint pixelOffset = (y * pixelWidth) + x;
        if (flipX)
        {
            for (int px = 0; px < 8; px++)
            {
                bitmap[pixelOffset++] = (byte)((bits & 0x01) != 0 ? 1 : 0);
                bits >>= 1;
            }
        }
        else
        {
            for (int px = 0; px < 8; px++)
            {
                bitmap[pixelOffset++] = (byte)((bits & 0x80) != 0 ? 1 : 0);
                bits <<= 1;
            }
        }
    }

    public void Xor8BitsNoAttribute(uint x, uint y, byte bits, bool flipX)
    {
        uint pixelOffset = (y * pixelWidth) + x;
        if (flipX)
        {
            for (int px = 0; px < 8; px++)
            {
                bitmap[pixelOffset++] ^= (byte)((bits & 0x01) != 0 ? 1 : 0);
                bits >>= 1;
            }
        }
        else
        {
            for (int px = 0; px < 8; px++)
            {
                bitmap[pixelOffset++] ^= (byte)((bits & 0x80) != 0 ? 1 : 0);
                bits <<= 1;
            }
        }
    }

    public void Draw8Bits(uint x, uint y, byte bits, byte attribute, bool flipX)
    {
        Draw8BitsNoAttribute(x, y, bits, flipX);
        SetAttribute(x, y, attribute);
    }
    
    public void Xor8Bits(uint x, uint y, byte bits, byte attribute, bool flipX)
    {
        Xor8BitsNoAttribute(x, y, bits, flipX);
        SetAttribute(x, y, attribute);
    }

    public void SetAttribute(uint x, uint y, byte attribute)
    {
        uint attributeOffset = ((y / 8) * attributeWidth) + (x / 8);
        attributes[attributeOffset] = attribute;
    }
    
    public byte GetAttribute(uint x, uint y)
    {
        uint attributeOffset = ((y / 8) * attributeWidth) + (x / 8);
        return attributes[attributeOffset];
    }
}