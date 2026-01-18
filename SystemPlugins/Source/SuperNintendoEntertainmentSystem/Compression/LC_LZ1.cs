using System;

namespace SuperNintendoEntertainmentSystem.Compression
{
    /// <summary>
    /// Provides LC-LZ1 compression/decompression utilities for Super Nintendo Entertainment System data.
    /// </summary>
    public static class LC_LZ1
    {
        private static void DecompressCode(ref byte[] decompBuffer, ref ReadOnlySpan<byte> data, ref int offset, int l, int c)
        {
            var count = l;
            switch (c)
            {
                case 0: // Direct Copy
                    {
                        for (int i = 0; i <= count; i++)
                        {
                            decompBuffer[offset++] = data[0];
                            data = data.Slice(1);
                        }
                    }
                    break;
                case 1: //Byte Fill
                    {
                        var value = data[0];
                        data = data.Slice(1);
                        for (int i = 0; i <= count; i++)
                        {
                            decompBuffer[offset++] = value;
                        }
                    }
                    break;
                case 2: //Word Fill
                    {
                        var v0 = data[0];
                        var v1 = data[1];
                        data = data.Slice(2);
                        bool oddeven = false;
                        for (int i = 0; i <= count; i++)
                        {
                            if (!oddeven)
                                decompBuffer[offset++] = v0;
                            else
                                decompBuffer[offset++] = v1;
                            oddeven = !oddeven;
                        }
                    }
                    break;
                case 3: //Increasing Fill
                    {
                        var value = data[0];
                        data = data.Slice(1);
                        for (int i = 0; i <= count; i++)
                        {
                            decompBuffer[offset++] = value++;
                        }
                    }
                    break;
                case 4: //Repeat
                    {
                        var h = data[0];    // Only difference between LC_LZ1 & LC_LZ2 - TODO REFACTOR THESE COMPRESSIONS
                        var L = data[1];
                        var src = (h << 8) | L;
                        data = data.Slice(2);
                        for (int i = 0; i <= count; i++)
                        {
                            decompBuffer[offset++] = decompBuffer[src++];
                        }
                    }
                    break;
                case 7: //LongLength
                    {
                        var nc = (l & 0x1C) >> 2;
                        var nl = (l & 3) << 8;
                        nl |= data[0];
                        data = data.Slice(1);
                        DecompressCode(ref decompBuffer, ref data, ref offset, nl, nc);
                    }
                    break;

            }
        }

        /// <summary>
        /// Decompresses LC-LZ1 compressed data.
        /// </summary>
        /// <param name="toDecompressBuffer">The buffer to store decompressed data.</param>
        /// <param name="data">The LC-LZ1 compressed data to decompress.</param>
        /// <param name="bytesRead">The number of bytes read from the compressed data.</param>
        /// <returns>The total number of bytes written to the decompression buffer.</returns>
        public static int Decompress(ref byte[] toDecompressBuffer, ReadOnlySpan<byte> data, out int bytesRead)
        {
            //LC_LZ1
            var offset = 0;
            var origDataLength = data.Length;

            while (data.Length > 0)
            {
                var b = data[0];
                if (b == 0xFF)
                {
                    break;
                }
                data = data.Slice(1);
                var l = b & 0x1F;
                var c = (b & 0xE0) >> 5;
                DecompressCode(ref toDecompressBuffer, ref data, ref offset, l, c);
            }
            bytesRead = origDataLength - data.Length;
            return offset;
        }
    }
}