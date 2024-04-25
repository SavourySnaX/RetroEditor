using System.Diagnostics;
using System.Text;
using RetroEditor.Plugins;

namespace ZXSpectrumTape
{
    /// <summary>
    /// The kind of header block to create. Maps to the ZX Spectrum Rom Tape Header types.
    /// </summary>
    public enum HeaderKind
    {
        /// <summary>
        /// ZX Spectrum Basic Program Header
        /// </summary>
        Program = 0,
        /// <summary>
        /// ZX Spectrum Number Array Header
        /// </summary>
        NumberArray = 1,
        /// <summary>
        /// ZX Spectrum Character Array Header
        /// </summary>
        CharacterArray = 2,
        /// <summary>
        /// ZX Spectrum Machine Code Header
        /// </summary>
        Code = 3
    }

    /// <summary>
    /// Data Block for a ZX Spectrum Tape, generally follows a header block.
    /// </summary>
    public ref struct DataBlock
    {
        private ReadOnlySpan<byte> _data;

        /// <summary>
        /// Create a new Data Block from a byte array
        /// </summary>
        /// <param name="bytes">Contents of the data block, only the data block, headers are applied internally</param>
        public DataBlock(ReadOnlySpan<byte> bytes)
        {
            _data = bytes;
        }

        internal bool ReadDataBlock(ReadOnlySpan<byte> bytes, int length)
        {
            _data = bytes.Slice(0, length - 1);
            if (bytes[length - 1] != (CheckSum(_data) ^ 0xFF))
            {
                throw new Exception($"ChkSum Mismatch");
            }
            return true;
        }

        private static byte CheckSum(ReadOnlySpan<byte> slice)
        {
            byte chk = 0;
            foreach (var b in slice)
            {
                chk ^= b;
            }
            return chk;
        }


        /// <summary>
        /// Fetches a textual representation of the data block
        /// </summary>
        /// <returns>string representing the contents of the data block</returns>
        public override string ToString()
        {
            var s = new StringBuilder();
            s.AppendLine("Data:");
            s.AppendLine($"Length:{_data.Length}");
            int w = 0;
            var t = new byte[16];
            foreach (var b in _data)
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

        internal byte[] GetBytes()
        {
            var t = new byte[4 + _data.Length];  // Tap Length | Flag | bytes | chksum
            var s = new Span<byte>(t);
            WriteUInt16(s.Slice(0, 2), (UInt16)(t.Length - 2));
            s[2] = 0xFF;
            for (int a = 0; a < _data.Length; a++)
            {
                s[3 + a] = _data[a];
            }
            var chkSlice = new Span<byte>(t, 2, t.Length - 3);
            s[t.Length - 1] = CheckSum(chkSlice);
            return t;
        }

        private void WriteUInt16(Span<byte> bytes, UInt16 len)
        {
            bytes[0] = (byte)len;
            bytes[1] = (byte)(len >> 8);
        }


        /// <summary>
        /// Returns a ReadOnlySpan of the data in the block
        /// </summary>
        public ReadOnlySpan<byte> Data => _data;
    }

    /// <summary>
    /// Header Block for a ZX Spectrum Tape
    /// </summary>
    public struct HeaderBlock
    {
        /// <summary>
        /// Create a new Header Block
        /// </summary>
        /// <param name="inType">Header block type</param>
        /// <param name="inTitle">Name of block, limited to 10 characters, will be padded if less</param>
        /// <param name="inDataLength">Length of data block that follows</param>
        /// <param name="inParam1">If Header is Program - AutoStart Line Number, If Header is Code - StartAddress</param>
        /// <param name="inParam2">If Header is Program - Variable Area Address Offset, If Header is Code - (ignored)</param>
        public HeaderBlock(HeaderKind inType, string inTitle, UInt16 inDataLength, UInt16 inParam1, UInt16 inParam2)
        {
            _type = inType;
            _title = inTitle;
            _dataLength = inDataLength;
            _param1 = inParam1;
            _param2 = inParam2;
        }

        private static UInt16 ReadUInt16(ReadOnlySpan<byte> bytes)
        {
            UInt16 len = bytes[1];
            len <<= 8;
            len |= bytes[0];
            return len;
        }

        internal bool ReadHeaderBlock(ReadOnlySpan<byte> bytes)
        {
            var chkSlice = bytes.Slice(0, 17);
            switch (bytes[0])
            {
                case 0:
                    _type = HeaderKind.Program;
                    break;
                case 1:
                    _type = HeaderKind.NumberArray;
                    break;
                case 2:
                    _type = HeaderKind.CharacterArray;
                    break;
                case 3:
                    _type = HeaderKind.Code;
                    break;
                default:
                    return false;
            }
            bytes = bytes.Slice(1);
            _title = System.Text.ASCIIEncoding.ASCII.GetString(bytes.Slice(0, 10)).Trim();
            bytes = bytes.Slice(10);
            _dataLength = ReadUInt16(bytes.Slice(0, 2));
            bytes = bytes.Slice(2);
            _param1 = ReadUInt16(bytes.Slice(0, 2));
            bytes = bytes.Slice(2);
            _param2 = ReadUInt16(bytes.Slice(0, 2));
            bytes = bytes.Slice(2);
            if (bytes[0] != CheckSum(chkSlice))
            {
                throw new Exception("ChkSum Mismatch");
            }
            return true;
        }

        /// <summary>
        /// Fetches a textual representation of the header block
        /// </summary>
        /// <returns>string representing the contents of the header block</returns>
        public override string ToString()
        {
            var s = new StringBuilder();
            switch (_type)
            {
                case HeaderKind.Program:
                    s.AppendLine("Program:");
                    break;
                case HeaderKind.Code:
                    s.AppendLine("Code:");
                    break;
            }
            s.AppendLine($"Title: {_title}");
            s.AppendLine($"Length: {_dataLength}");
            switch (_type)
            {
                case HeaderKind.Program:
                    s.AppendLine($"AutoStart: {_param1}");
                    s.AppendLine($"Variables Offset: {_param2}");
                    break;
                case HeaderKind.Code:
                    s.AppendLine($"Start: {_param1}");
                    break;
            }
            return s.ToString();
        }

        internal byte[] GetBytes()
        {
            var t = new byte[21];   // Tap Length | Flag | Header | chksum
            var s = new Span<byte>(t, 0, 21);
            WriteUInt16(s.Slice(0, 2), 19);
            var chkSlice = new Span<byte>(t, 2, 18);
            s[2] = 00;    // Header Block
            s = s.Slice(3, 18);
            s[0] = (byte)_type;
            var name = System.Text.Encoding.ASCII.GetBytes(_title);
            for (int a = 0; a < 10; a++)
            {
                s[1 + a] = 0x20;
            }
            for (int a = 0; a < Math.Min(name.Length, 10); a++)
            {
                s[1 + a] = name[a];
            }
            WriteUInt16(s.Slice(11, 2), _dataLength);
            WriteUInt16(s.Slice(13, 2), _param1);
            WriteUInt16(s.Slice(15, 2), _param2);
            s[17] = CheckSum(chkSlice);

            return t;
        }

        private static byte CheckSum(ReadOnlySpan<byte> slice)
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


        private HeaderKind _type;
        private string _title;
        private UInt16 _dataLength;
        private UInt16 _param1;
        private UInt16 _param2;

        /// <summary>
        /// The kind of header of this header block
        /// </summary>
        public HeaderKind Kind => _type;

        /// <summary>
        /// The title of this header block
        /// </summary>
        public string Title => _title;
    }

    internal enum BlockKind
    {
        Header,
        Data
    }

    /// <summary>
    /// Represents a ZX Spectrum Tape image
    /// </summary>
    public class Tape : ISave
    {
        private byte[]? _tapeData;

        /// <summary>
        /// Create a new Tape object
        /// </summary>
        public Tape()
        {
            _tapeData = null;
        }

        /// <summary>
        /// Create a new Tape object from a file
        /// </summary>
        /// <param name="filename">Path to a .Tap format tape image</param>
        public void Load(string filename)
        {
            _tapeData = File.ReadAllBytes(filename);
        }
        
        /// <summary>
        /// Write the current tape object to a file (.Tap format image)
        /// </summary>
        /// <param name="path">Path to save tape image to</param>
        /// <exception cref="Exception">Throws an exception if the tape is empty</exception>
        public void Save(string path)
        {
            if (_tapeData == null)
            {
                throw new Exception($"Cannot save with empty tape data");
            }
            File.WriteAllBytes(path, _tapeData);
        }

        /// <summary>
        /// Add a header block to the tape
        /// </summary>
        /// <param name="header">HeaderBlock object to add</param>
        public void AddHeader(HeaderBlock header)
        {
            var headerBytes = header.GetBytes();
            if (_tapeData == null)
            {
                _tapeData = headerBytes;
            }
            else
            {
                var t = new byte[_tapeData.Length + headerBytes.Length];
                Array.Copy(_tapeData, t, _tapeData.Length);
                Array.Copy(headerBytes, 0, t, _tapeData.Length, headerBytes.Length);
                _tapeData = t;
            }
        }

        /// <summary>
        /// Add a data block to the tape
        /// </summary>
        /// <param name="data">DataBlock object to add</param>
        public void AddBlock(DataBlock data)
        {
            var dataBytes = data.GetBytes();
            if (_tapeData == null)
            {
                _tapeData = dataBytes;
            }
            else
            {
                var t = new byte[_tapeData.Length + dataBytes.Length];
                Array.Copy(_tapeData, t, _tapeData.Length);
                Array.Copy(dataBytes, 0, t, _tapeData.Length, dataBytes.Length);
                _tapeData = t;
            }
        }

        private UInt16 GetBlockLength(ReadOnlySpan<byte> bytes)
        {
            UInt16 len = bytes[1];
            len <<= 8;
            len |= bytes[0];
            return len;
        }

        private BlockKind GetBlockKind(ReadOnlySpan<byte> bytes)
        {
            return bytes[0] == 0 ? BlockKind.Header : BlockKind.Data;
        }

        private HeaderBlock GetHeaderBlock(ReadOnlySpan<byte> bytes)
        {
            HeaderBlock t = new HeaderBlock();
            t.ReadHeaderBlock(bytes);
            return t;
        }

        private DataBlock GetDataBlock(ReadOnlySpan<byte> bytes, int length)
        {
            DataBlock t = new DataBlock();
            t.ReadDataBlock(bytes, length);
            return t;
        }

        /// <summary>
        /// Allows iteration of all basic programs on the tape, returning the HeaderBlock and the contents of the data block
        /// </summary>
        /// <returns>A Tuple containing the HeaderBlock and the contents of the data block for each basic program</returns>
        public IEnumerable<(HeaderBlock header, byte[] data)> BasicPrograms()
        {
            var slice = new Memory<byte>(_tapeData);
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

        /// <summary>
        /// Allows iteration of all machine code files on the tape, returning the HeaderBlock and the contents of the data block
        /// </summary>
        /// <returns>A Tuple containing the HeaderBlock and the contents of the data block for each machine code block in the image</returns>
        public IEnumerable<(HeaderBlock header, byte[] data)> RegularCodeFiles()
        {
            var slice = new Memory<byte>(_tapeData);
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

        /// <summary>
        /// Fetches a textual representation of the tape
        /// </summary>
        /// <returns>A string representing the tape object</returns>
        public override string ToString()
        {
            var s = new StringBuilder();
            // Dump tape information
            var slice = new ReadOnlySpan<byte>(_tapeData);
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
                        s.AppendLine(block.ToString());
                        lastHeaderKind = block.Kind;
                        lastHeader = block;
                        break;
                    case BlockKind.Data:
                        var data = GetDataBlock(slice, len);
                        s.AppendLine(data.ToString());
                        break;
                }
                slice = slice.Slice(len);
            }

            return s.ToString();
        }
    }
}
