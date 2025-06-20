
using System;

namespace SuperNintendoEntertainmentSystem.Compression
{
    public static class LC_LZ5
    {
        private static void DecompressCode(ref byte[] decompBuffer, ref ReadOnlySpan<byte> data, ref int offset, int l, int c, bool LongLength)
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
                        var h = data[1];
                        var L = data[0];
                        var src = (h << 8) | L;
                        data = data.Slice(2);
                        for (int i = 0; i <= count; i++)
                        {
                            decompBuffer[offset++] = decompBuffer[src++];
                        }
                    }
                    break;
                case 5: //EOR Repeat
                    {
                        var h = data[1];
                        var L = data[0];
                        var src = (h << 8) | L;
                        data = data.Slice(2);
                        for (int i = 0; i <= count; i++)
                        {
                            decompBuffer[offset++] = (byte)(decompBuffer[src++] ^ 0xFF);
                        }
                    }
                    break;
                case 6: //Minus Copy
                    {
                        var negOffs = data[0];
                        var src = offset - negOffs;
                        data = data.Slice(1);
                        for (int i = 0; i <= count; i++)
                        {
                            decompBuffer[offset++] = decompBuffer[src++];
                        }
                    }
                    break;
                case 7: //LongLength
                    {
                        if (LongLength)
                        {
                            throw new NotImplementedException("LongLength exclusive code not implemented yet");
                        }
                        else
                        {
                            var nc = (l & 0x1C) >> 2;
                            var nl = (l & 3) << 8;
                            nl |= data[0];
                            data = data.Slice(1);
                            DecompressCode(ref decompBuffer, ref data, ref offset, nl, nc, true);
                        }
                    }
                    break;

            }

        }

        public static int Decompress(ref byte[] toDecompressBuffer, ReadOnlySpan<byte> data, out int bytesRead)
        {
            //LC_LZ5
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
                DecompressCode(ref toDecompressBuffer, ref data, ref offset, l, c, false);
            }

            bytesRead = origDataLength - data.Length;
            return offset;
        }
    }
}
