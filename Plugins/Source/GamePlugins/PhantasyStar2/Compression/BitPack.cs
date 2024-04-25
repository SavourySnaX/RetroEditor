using System.Collections.Generic;
using RetroEditor.Plugins;

public class BitPack
{

    public byte[] Decompress(IMemoryAccess rom, uint sourceAddress)
    {
        byte[] scratch = new byte[32];
        List<byte> output = new List<byte>();

        while (true)
        {
            uint bitsUsed = 0;
            int packedDataCounter = (sbyte)rom.ReadBytes(ReadKind.Rom, sourceAddress++, 1)[0];
            uint UnpackedBitsRemain = 0xFFFFFFFF;
            if (packedDataCounter != 0)
            {
                if (packedDataCounter<0)
                {
                    return output.ToArray();
                }
                packedDataCounter--;

                while (packedDataCounter >= 0)
                {
                    var bytes=rom.ReadBytes(ReadKind.Rom, sourceAddress, 5);
                    sourceAddress += 5;
                    var splatByte = bytes[0];
                    uint bitsToUnpack = bytes[1];
                    bitsToUnpack <<= 8;
                    bitsToUnpack |= bytes[2];
                    bitsToUnpack <<= 8;
                    bitsToUnpack |= bytes[3];
                    bitsToUnpack <<= 8;
                    bitsToUnpack |= bytes[4];
                    bitsUsed |= bitsToUnpack;

                    for (int a=0;a<32;a++)
                    {
                        if ((bitsToUnpack & 0x80000000) == 0x80000000)
                        {
                            scratch[a] = splatByte;
                        }
                        bitsToUnpack <<= 1;
                    }
                    packedDataCounter--;
                }
                UnpackedBitsRemain ^= bitsUsed;
            }
            if (UnpackedBitsRemain != 0)
            {
                for (int a=0;a<32;a++)
                {
                    if ((bitsUsed & 0x80000000) == 0x00000000)
                    {
                        scratch[a] = rom.ReadBytes(ReadKind.Rom, sourceAddress++, 1)[0];
                    }
                    bitsUsed <<= 1;
                }
            }
            for (int a = 0; a < 32; a++)
            {
                output.Add(scratch[a]);
            }

        }

    }
}
