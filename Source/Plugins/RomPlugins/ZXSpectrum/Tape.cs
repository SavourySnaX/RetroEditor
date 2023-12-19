using System.Text; 

namespace ZXSpectrumTape
{
    public enum HeaderKind
    {
        Program = 0,
        NumberArray = 1,
        CharacterArray = 2,
        Code = 3
    }

    public ref struct DataBlock
    {
        ReadOnlySpan<byte> data;

        public DataBlock(ReadOnlySpan<byte> bytes)
        {
            data = bytes;
        }

        public bool ReadDataBlock(ReadOnlySpan<byte> bytes, int length)
        {
            data = bytes.Slice(0, length - 1);
            if (bytes[length - 1] != (CheckSum(data) ^ 0xFF))
            {
                throw new Exception($"ChkSum Mismatch");
            }
            return true;
        }

        byte CheckSum(ReadOnlySpan<byte> slice)
        {
            byte chk = 0;
            foreach (var b in slice)
            {
                chk ^= b;
            }
            return chk;
        }


        public string Dump()
        {
            var s = new StringBuilder();
            s.AppendLine("Data:");
            s.AppendLine($"Length:{data.Length}");
            int w = 0;
            var t = new byte[16];
            foreach (var b in data)
            {
                s.Append($"{b:X2} ");
                t[w] = b;
                w++;
                if (w >= 16)
                {
                    w = 0;
                    s.Append("    ");
                    foreach (var n in t)
                    {
                        if (n >= 32)
                            s.Append($"{(char)n}");
                        else
                            s.Append(".");
                    }
                    s.AppendLine();
                }
            }
            if (w != 0)
            {
                for (int p = w; p < 16; p++)
                {
                    s.Append("   ");
                }
                s.Append("    ");
                for (int p = 0; p < w; p++)
                {
                    var n = t[p];
                    if (n >= 32)
                        s.Append($"{(char)n}");
                    else
                        s.Append(".");
                }
                s.AppendLine();
            }

            return s.ToString();
        }

        public byte[] GetBytes()
        {
            var t = new byte[4 + data.Length];  // Tap Length | Flag | bytes | chksum
            var s = new Span<byte>(t);
            WriteUInt16(s.Slice(0, 2), (UInt16)(t.Length - 2));
            s[2] = 0xFF;
            for (int a = 0; a < data.Length; a++)
            {
                s[3 + a] = data[a];
            }
            var chkSlice = new Span<byte>(t, 2, t.Length - 3);
            s[t.Length - 1] = CheckSum(chkSlice);
            return t;
        }

        void WriteUInt16(Span<byte> bytes, UInt16 len)
        {
            bytes[0] = (byte)len;
            bytes[1] = (byte)(len >> 8);
        }


        public ReadOnlySpan<byte> Data => data;
    }

    public struct HeaderBlock
    {
        public HeaderBlock(HeaderKind inType, string inTitle, UInt16 inDataLength, UInt16 inParam1, UInt16 inParam2)
        {
            type = inType;
            title = inTitle;
            dataLength = inDataLength;
            param1 = inParam1;
            param2 = inParam2;
        }

        public UInt16 ReadUInt16(ReadOnlySpan<byte> bytes)
        {
            UInt16 len = bytes[1];
            len <<= 8;
            len |= bytes[0];
            return len;
        }

        public bool ReadHeaderBlock(ReadOnlySpan<byte> bytes)
        {
            var chkSlice = bytes.Slice(0, 17);
            switch (bytes[0])
            {
                case 0:
                    type = HeaderKind.Program;
                    break;
                case 1:
                    type = HeaderKind.NumberArray;
                    break;
                case 2:
                    type = HeaderKind.CharacterArray;
                    break;
                case 3:
                    type = HeaderKind.Code;
                    break;
                default:
                    return false;
            }
            bytes = bytes.Slice(1);
            title = System.Text.ASCIIEncoding.ASCII.GetString(bytes.Slice(0, 10)).Trim();
            bytes = bytes.Slice(10);
            dataLength = ReadUInt16(bytes.Slice(0, 2));
            bytes = bytes.Slice(2);
            param1 = ReadUInt16(bytes.Slice(0, 2));
            bytes = bytes.Slice(2);
            param2 = ReadUInt16(bytes.Slice(0, 2));
            bytes = bytes.Slice(2);
            if (bytes[0] != CheckSum(chkSlice))
            {
                throw new Exception("ChkSum Mismatch");
            }
            return true;
        }

        public string Dump()
        {
            var s = new StringBuilder();
            switch (type)
            {
                case HeaderKind.Program:
                    s.AppendLine("Program:");
                    break;
                case HeaderKind.Code:
                    s.AppendLine("Code:");
                    break;
            }
            s.AppendLine($"Title: {title}");
            s.AppendLine($"Length: {dataLength}");
            switch (type)
            {
                case HeaderKind.Program:
                    s.AppendLine($"AutoStart: {param1}");
                    s.AppendLine($"Variables Offset: {param2}");
                    break;
                case HeaderKind.Code:
                    s.AppendLine($"Start: {param1}");
                    break;
            }
            return s.ToString();
        }

        public byte[] GetBytes()
        {
            var t = new byte[21];   // Tap Length | Flag | Header | chksum
            var s = new Span<byte>(t, 0, 21);
            WriteUInt16(s.Slice(0, 2), 19);
            var chkSlice = new Span<byte>(t, 2, 18);
            s[2] = 00;    // Header Block
            s = s.Slice(3, 18);
            s[0] = (byte)type;
            var name = System.Text.Encoding.ASCII.GetBytes(title);
            for (int a = 0; a < 10; a++)
            {
                s[1 + a] = 0x20;
            }
            for (int a = 0; a < Math.Min(name.Length, 10); a++)
            {
                s[1 + a] = name[a];
            }
            WriteUInt16(s.Slice(11, 2), dataLength);
            WriteUInt16(s.Slice(13, 2), param1);
            WriteUInt16(s.Slice(15, 2), param2);
            s[17] = CheckSum(chkSlice);

            return t;
        }

        byte CheckSum(ReadOnlySpan<byte> slice)
        {
            byte chk = 0;
            foreach (var b in slice)
            {
                chk ^= b;
            }
            return chk;
        }

        void WriteUInt16(Span<byte> bytes, UInt16 len)
        {
            bytes[0] = (byte)len;
            bytes[1] = (byte)(len >> 8);
        }


        HeaderKind type;
        string title;
        UInt16 dataLength;
        UInt16 param1;
        UInt16 param2;

        public HeaderKind Kind => type;

        public int VariablesOffset => (type == HeaderKind.Program) ? param2 : throw new Exception($"Not a Program Block");
        public int CodeStart => (type == HeaderKind.Code) ? param1 : throw new Exception($"Not a Code Block");

        public string Title => title;
    }

    public enum BlockKind
    {
        Header,
        Data
    }

    public class Tape
    {
        private string filepath;
        private byte[]? tapeData;

        public string Filename => Path.GetFileNameWithoutExtension(filepath);
        public string Filepath => filepath;

        public Tape()
        {
            filepath = "";
            tapeData = null;
        }

        public void Load(string filename)
        {
            filepath = filename;
            tapeData = File.ReadAllBytes(filename);
        }
        
        public void Load(byte[] data)
        {
            filepath = "";
            tapeData = data;
        }

        public void Save(string filename)
        {
            if (tapeData == null)
            {
                throw new Exception($"Cannot save with empty tape data");
            }
            File.WriteAllBytes(filename, tapeData);
            filepath = filename;
        }

        public void AddHeader(HeaderBlock header)
        {
            var headerBytes = header.GetBytes();
            if (tapeData == null)
            {
                tapeData = headerBytes;
            }
            else
            {
                var t = new byte[tapeData.Length + headerBytes.Length];
                Array.Copy(tapeData, t, tapeData.Length);
                Array.Copy(headerBytes, 0, t, tapeData.Length, headerBytes.Length);
                tapeData = t;
            }
        }

        public void AddBlock(DataBlock data)
        {
            var dataBytes = data.GetBytes();
            if (tapeData == null)
            {
                tapeData = dataBytes;
            }
            else
            {
                var t = new byte[tapeData.Length + dataBytes.Length];
                Array.Copy(tapeData, t, tapeData.Length);
                Array.Copy(dataBytes, 0, t, tapeData.Length, dataBytes.Length);
                tapeData = t;
            }
        }


        UInt16 GetBlockLength(ReadOnlySpan<byte> bytes)
        {
            UInt16 len = bytes[1];
            len <<= 8;
            len |= bytes[0];
            return len;
        }

        BlockKind GetBlockKind(ReadOnlySpan<byte> bytes)
        {
            return bytes[0] == 0 ? BlockKind.Header : BlockKind.Data;
        }

        HeaderBlock GetHeaderBlock(ReadOnlySpan<byte> bytes)
        {
            HeaderBlock t = new HeaderBlock();
            t.ReadHeaderBlock(bytes);
            return t;
        }

        DataBlock GetDataBlock(ReadOnlySpan<byte> bytes, int length)
        {
            DataBlock t = new DataBlock();
            t.ReadDataBlock(bytes, length);
            return t;
        }

        public IEnumerable<(HeaderBlock header, byte[] data)> BasicPrograms()
        {
            var slice = new Memory<byte>(tapeData);
            HeaderKind lastHeaderKind = HeaderKind.NumberArray;
            HeaderBlock lastHeader = default;
            while (slice.Length > 0)
            {
                var len = GetBlockLength(slice.Span);
                slice = slice.Slice(2);
                var blockKind = GetBlockKind(slice.Span);
                slice = slice.Slice(1);
                len -= 1;
                switch (blockKind)
                {
                    case BlockKind.Header:
                        var block = GetHeaderBlock(slice.Span);
                        lastHeaderKind = block.Kind;
                        lastHeader = block;
                        break;
                    case BlockKind.Data:
                        var data = GetDataBlock(slice.Span, len);
                        if (lastHeaderKind == HeaderKind.Program)
                        {
                            yield return (lastHeader, data.Data.ToArray());
                        }
                        break;
                }
                slice = slice.Slice(len);
            }

        }

        public IEnumerable<(HeaderBlock header, byte[] data)> RegularCodeFiles()
        {
            var slice = new Memory<byte>(tapeData);
            HeaderKind lastHeaderKind = HeaderKind.NumberArray;
            HeaderBlock lastHeader = default;
            while (slice.Length > 0)
            {
                var len = GetBlockLength(slice.Span);
                slice = slice.Slice(2);
                var blockKind = GetBlockKind(slice.Span);
                slice = slice.Slice(1);
                len -= 1;
                switch (blockKind)
                {
                    case BlockKind.Header:
                        var block = GetHeaderBlock(slice.Span);
                        lastHeaderKind = block.Kind;
                        lastHeader = block;
                        break;
                    case BlockKind.Data:
                        var data = GetDataBlock(slice.Span, len);
                        if (lastHeaderKind == HeaderKind.Code)
                        {
                            yield return (lastHeader, data.Data.ToArray());
                        }
                        break;
                }
                slice = slice.Slice(len);
            }

        }


        public string Dump()
        {
            var s = new StringBuilder();
            // Dump tape information
            var slice = new ReadOnlySpan<byte>(tapeData);
            HeaderKind lastHeaderKind = HeaderKind.NumberArray;
            HeaderBlock lastHeader = default;
            while (slice.Length > 0)
            {
                var len = GetBlockLength(slice);
                slice = slice.Slice(2);
                var blockKind = GetBlockKind(slice);
                slice = slice.Slice(1);
                len -= 1;
                switch (blockKind)
                {
                    case BlockKind.Header:
                        var block = GetHeaderBlock(slice);
                        s.AppendLine(block.Dump());
                        lastHeaderKind = block.Kind;
                        lastHeader = block;
                        break;
                    case BlockKind.Data:
                        var data = GetDataBlock(slice, len);
                        s.AppendLine(data.Dump());
                        break;
                }
                slice = slice.Slice(len);
            }

            return s.ToString();
        }
    }
}
