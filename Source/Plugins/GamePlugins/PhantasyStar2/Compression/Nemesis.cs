/*

public class Nemesis
{
    ushort[] CodeTable = new ushort[256];

    private sbyte GetCodeLength(byte offset)
    {
        return (sbyte)((CodeTable[offset] >> 8) & 0xFF);
    }

    private byte GetPalIndex(byte offset)
    {
        return (byte)((CodeTable[offset])&0xF);
    }

    private int GetRepCount(byte offset)
    {
        return (byte)((CodeTable[offset] >> 4)&0xF);
    }

    public byte[] Decompress(IRomPlugin rom, uint sourceAddress)
    {
        bool xorMode = false;
        var numPatterns = rom.ReadWord(sourceAddress);
        sourceAddress += 2;
        if ((numPatterns&0x8000)==0x8000)
        {
            xorMode = true;
        }
        numPatterns <<= 3;


        BuildCodeTable(rom,ref sourceAddress);

        ushort firstCompWord = rom.ReadByte(sourceAddress++);
        firstCompWord <<= 8;
        firstCompWord |= rom.ReadByte(sourceAddress++);

        return ProcessCompData(rom,sourceAddress,firstCompWord,numPatterns,xorMode);
    }

    private byte[] ProcessCompData(IRomPlugin rom, uint address, ushort compWord, ushort numPatterns, bool xorMode)
    {
        var patternRow = 8;
        int d2 = 0;
        var d4 = 0;
        var initialShiftValue = 0x10;
        List<int> output = new List<int>();

        byte palIndex = 0;
        int repCnt = 0;
        while (true)
        {
            var shiftValue = initialShiftValue - 8;
            var d1 = compWord;
            d1 >>= shiftValue;
            if ((d1 & 0xFC) == 0xFC)
            {
                // Inline Data
                initialShiftValue = initialShiftValue - 6;
                if (initialShiftValue <= 8)
                {
                    initialShiftValue += 8;
                    compWord <<= 8;
                    compWord |= rom.ReadByte(address++);
                }
                initialShiftValue = initialShiftValue - 7;
                d1 = compWord;
                d1 >>= initialShiftValue;
                palIndex = (byte)(d1 & 0x0F);
                repCnt = (d1 & 0x70)>>4;
                if (initialShiftValue <= 8)
                {
                    initialShiftValue += 8;
                    compWord <<= 8;
                    compWord |= rom.ReadByte(address++);
                }
            }
            else
            {
                d1 &= 0xFF;
                int d0 = GetCodeLength((byte)d1);
                initialShiftValue = initialShiftValue - d0;
                if (initialShiftValue <= 8)
                {
                    initialShiftValue += 8;
                    compWord <<= 8;
                    compWord |= rom.ReadByte(address++);
                }

                palIndex = GetPalIndex((byte)d1);
                repCnt = GetRepCount((byte)d1);
            }

            while (repCnt >= 0)
            {
                d4 <<= 4;
                d4 |= palIndex;
                patternRow--;
                if (patternRow != 0)
                {
                    repCnt--;
                    continue;
                }

                // Write pattern row 
                if (xorMode)
                {
                    d2 ^= d4;
                }
                output.Add(d2);
                numPatterns--;
                if (numPatterns == 0)
                {
                    var retArray = new byte[output.Count * 8];
                    for (int a = 0; a < output.Count; a++)
                    {
                        var val = output[a];
                        retArray[a * 8 + 0] = (byte)((val >> 28) & 0xF);
                        retArray[a * 8 + 1] = (byte)((val >> 24) & 0xF);
                        retArray[a * 8 + 2] = (byte)((val >> 20) & 0xF);
                        retArray[a * 8 + 3] = (byte)((val >> 16) & 0xF);
                        retArray[a * 8 + 4] = (byte)((val >> 12) & 0xF);
                        retArray[a * 8 + 5] = (byte)((val >> 8) & 0xF);
                        retArray[a * 8 + 6] = (byte)((val >> 4) & 0xF);
                        retArray[a * 8 + 7] = (byte)((val >> 0) & 0xF);
                    }
                    return retArray;
                }
                d4 = 0;
                patternRow = 8;
                repCnt--;
            }

        }

    }

    private void BuildCodeTable(IRomPlugin rom,ref uint address)
    {
        var firstByte = rom.ReadByte(address++);
        var palIndex = firstByte;
        while (firstByte != 0xFF)
        {
            var nextByte = rom.ReadByte(address++);
            if ((nextByte & 0x80) == 0x80)
            {
                firstByte = nextByte;
                palIndex = firstByte;
                continue;
            }

            palIndex &= 0xF;
            byte repeatCount = (byte)(nextByte & 0x70);
            palIndex |= repeatCount;
            byte codeLength = (byte)(nextByte & 0xF);
            var tableEntry = (ushort)(256 * codeLength + palIndex);
            codeLength=(byte)(8-codeLength);
            if (codeLength == 0)
            {
                ushort code = rom.ReadByte(address++);
                CodeTable[code] = tableEntry;
            }
            else
            {
                // short code
                ushort code = rom.ReadByte(address++);
                code <<= codeLength;
                var countMask = (1 << codeLength) - 1;
                do
                {
                    CodeTable[code] = tableEntry;
                    code ++;
                    countMask--;
                } while (countMask >= 0);
            }
        }
    }
}

*/