using System.Runtime.InteropServices;
using System.Text;

namespace RetroEditor.Source.Internals.ReverseEngineering.Platform;

/// <summary>
/// Data provider that stores data in an in-memory buffer
/// Used for physical memory regions (ROM, SRAM) that have actual data
/// </summary>
internal class BufferDataProvider : IMemoryRegionDataProvider
{
    private byte[] buffer;
    
    public string RegionName { get; }
    public UInt64 DataSize => (UInt64)buffer.Length;

    public BufferDataProvider(string regionName)
    {
        RegionName = regionName;
        buffer = new byte[0];
    }

    public BufferDataProvider(string regionName, byte[] initialData)
    {
        RegionName = regionName;
        buffer = new byte[initialData.Length];
        initialData.CopyTo(buffer, 0);
    }

    public byte GetByte(UInt64 address)
    {
        if (address >= (UInt64)buffer.Length)
            return 0;
        return buffer[address];
    }

    public ReadOnlySpan<byte> FetchBytes(UInt64 address, UInt64 length)
    {
        if (address >= (UInt64)buffer.Length)
            return new ReadOnlySpan<byte>();
        
        length = Math.Min(length, (UInt64)buffer.Length - address);
        return new ReadOnlySpan<byte>(buffer, (int)address, (int)length);
    }

    /// <summary>
    /// Loads data from a span directly into the buffer
    /// </summary>
    public void LoadBuffer(ReadOnlySpan<byte> data)
    {
        if (buffer.Length < data.Length)
        {
            System.Array.Resize(ref buffer, data.Length);
        }
        data.CopyTo(buffer.AsSpan(0, data.Length));
    }

    /// <summary>
    /// Resizes the buffer to the specified size
    /// </summary>
    public void Resize(UInt64 newSize)
    {
        if ((UInt64)buffer.Length != newSize)
        {
            System.Array.Resize(ref buffer, (int)newSize);
        }
    }

    /// <summary>
    /// Sets a byte at the specified address
    /// </summary>
    public void SetByte(UInt64 address, byte value)
    {
        if (address < (UInt64)buffer.Length)
        {
            buffer[address] = value;
        }
    }

    /// <summary>
    /// Copies data into the buffer at the specified offset
    /// </summary>
    public void WriteBytes(UInt64 offset, ReadOnlySpan<byte> data)
    {
        if (offset < (UInt64)buffer.Length)
        {
            var length = Math.Min((UInt64)data.Length, (UInt64)buffer.Length - offset);
            data[..(int)length].CopyTo(new Span<byte>(buffer, (int)offset, (int)length));
        }
    }

    public virtual void LoadFromDebugger(LibMameDebugger debugger)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Data provider for virtual memory regions that don't have actual data
/// Used for RAM or other regions where data doesn't need to be stored
/// Returns placeholder values
/// </summary>
internal class VirtualDataProvider : IMemoryRegionDataProvider
{
    private readonly UInt64 size;

    public string RegionName { get; }
    public UInt64 DataSize => size;

    public VirtualDataProvider(string regionName, UInt64 regionSize)
    {
        RegionName = regionName;
        size = regionSize;
    }

    public byte GetByte(UInt64 address)
    {
        if (address >= size)
            return 0;
        // Return placeholder value for virtual memory
        return 0x00;
    }

    public ReadOnlySpan<byte> FetchBytes(UInt64 address, UInt64 length)
    {
        // Virtual regions don't store data, so we can't return a real span
        // Instead, return empty span - callers should handle this
        return new ReadOnlySpan<byte>();
    }

    public void LoadFromDebugger(LibMameDebugger debugger)
    {
        // Virtual regions don't load data from debugger
    }
}

/// <summary>
/// Data provider for debugger-loaded physical memory regions
/// Handles parsing data from MAME debugger memory views into a read-only buffer
/// Data is loaded once and cached
/// </summary>
internal class DebuggerDataReadOnlyProvider : BufferDataProvider
{
    private readonly string mameViewName;
    private const int BYTES_PER_LINE = 16;

    public DebuggerDataReadOnlyProvider(string regionName, string mameViewName)
        : base(regionName)
    {
        this.mameViewName = mameViewName;
    }

    public override void LoadFromDebugger(LibMameDebugger debugger)
    {
        var view = new LibMameDebugger.DView(
            debugger.AllocView(LibMameDebuggerRetroPlugin.debug_view_type.Memory),
            0, 0, 256, 256, "");

        try
        {
            // Find the memory source for this region
            int sourceCount = debugger.GetSourcesCount(ref view);
            var sources = debugger.GetSourcesList(ref view);

            int sourceIndex = -1;
            for (int i = 0; i < sourceCount; i++)
            {
                if (sources[i].Contains(mameViewName))
                {
                    sourceIndex = i;
                    debugger.SetSource(ref view, i);
                    break;
                }
            }

            if (sourceIndex == -1)
                throw new Exception($"Could not find memory source for region: {mameViewName}");

            // Find the size of the region
            view.view.Expression = $"${UInt64.MaxValue:X}";
            debugger.SetExpression(ref view);
            debugger.SetDataFormat(ref view, LibMameDebuggerRetroPlugin.debug_format.DataFormat2ByteHex);
            debugger.UpdateDView(ref view);
            debugger.SetDataFormat(ref view, LibMameDebuggerRetroPlugin.debug_format.DataFormat1ByteHex);
            debugger.UpdateDView(ref view);

            var regionSize = FindLastAddress(view);
            Resize(regionSize);

            // Load data from the region
            UInt64 offset = 0;
            while (offset < regionSize)
            {
                view.view.Expression = $"${offset:X}";
                debugger.SetExpression(ref view);
                debugger.UpdateDView(ref view);
                offset = ParseChunk(view, offset);
            }
        }
        finally
        {
            debugger.FreeView(view.view);
        }
    }

    private UInt64 ParseChunk(LibMameDebugger.DView view, UInt64 firstOffset)
    {
        UInt64 expectedOffset = firstOffset;
        int bytesPerLine = view.view.W * 2; // Each character is 2 bytes (char + attribute)

        for (int y = 0; y < view.view.H; y++)
        {
            int lineStart = y * bytesPerLine;
            int x = 0;

            // Skip initial spaces
            while (x < view.view.W && (char)view.state[lineStart + x * 2] == ' ')
                x++;

            // Parse address
            StringBuilder addressStr = new StringBuilder();
            while (x < view.view.W && (char)view.state[lineStart + x * 2] != ' ')
            {
                addressStr.Append((char)view.state[lineStart + x * 2]);
                x++;
            }
            
            if (addressStr.Length == 0)
                break;
                
            var address = UInt64.Parse(addressStr.ToString(), System.Globalization.NumberStyles.HexNumber);
            if (address != expectedOffset)
                throw new Exception($"Address mismatch at offset {expectedOffset}");
            expectedOffset += BYTES_PER_LINE;

            // Skip spaces between address and bytes
            while (x < view.view.W && (char)view.state[lineStart + x * 2] == ' ')
                x++;

            // Parse bytes
            for (int byteCount = 0; byteCount < BYTES_PER_LINE && x < view.view.W - 1; byteCount++)
            {
                char highNibble = (char)view.state[lineStart + x * 2];
                char lowNibble = (char)view.state[lineStart + (x + 1) * 2];

                if (char.IsLetterOrDigit(highNibble) && char.IsLetterOrDigit(lowNibble))
                {
                    byte value = Convert.ToByte($"{highNibble}{lowNibble}", 16);
                    SetByte(address + (UInt64)byteCount, value);
                }
                else
                {
                    throw new Exception($"Invalid byte at offset {address + (UInt64)byteCount}");
                }

                x += 3;
            }
        }

        return expectedOffset;
    }

    private UInt64 FindLastAddress(LibMameDebugger.DView view)
    {
        UInt64 lastAddress = 0;
        int bytesPerLine = view.view.W * 2;
        int lastValidRow = -1;

        for (int y = 0; y < view.view.H; y++)
        {
            int lineStart = y * bytesPerLine;
            int x = 0;

            while (x < view.view.W && (char)view.state[lineStart + x * 2] == ' ')
                x++;

            if (x < view.view.W && char.IsLetterOrDigit((char)view.state[lineStart + x * 2]))
            {
                lastValidRow = y;
            }
        }

        if (lastValidRow >= 0)
        {
            int lineStart = lastValidRow * bytesPerLine;
            int x = 0;

            while (x < view.view.W && (char)view.state[lineStart + x * 2] != ' ')
                x++;

            while (x < view.view.W && (char)view.state[lineStart + x * 2] == ' ')
                x++;

            StringBuilder addressStr = new StringBuilder();
            while (x < view.view.W && (char)view.state[lineStart + x * 2] != ' ')
            {
                addressStr.Append((char)view.state[lineStart + x * 2]);
                x++;
            }

            if (addressStr.Length > 0 && UInt64.TryParse(addressStr.ToString(), System.Globalization.NumberStyles.HexNumber, null, out var addr))
            {
                lastAddress = addr;

                // Count valid bytes on this line
                int validBytes = 0;
                while (x < view.view.W - 1)
                {
                    char highNibble = (char)view.state[lineStart + x * 2];
                    char lowNibble = (char)view.state[lineStart + (x + 1) * 2];

                    if (char.IsLetterOrDigit(highNibble) && char.IsLetterOrDigit(lowNibble))
                        validBytes++;
                    else
                        break;

                    x += 3;
                }

                if (validBytes > 0)
                {
                    lastAddress += (UInt64)validBytes;
                }
            }
        }

        return lastAddress;
    }

    /// <summary>
    /// Returns the internal buffer (for backward compatibility with GetRomData)
    /// </summary>
    public ReadOnlySpan<byte> GetBuffer()
    {
        // Access the protected buffer field through the property
        return FetchBytes(0, DataSize);
        /*
        var tempSpan = FetchBytes(0, DataSize);
        var bytes = new byte[tempSpan.Length];
        tempSpan.CopyTo(bytes);
        return bytes;
        */
    }
}

/// <summary>
/// Data provider that reads live memory directly from the debugger
/// Does not cache data - each read queries MAME directly
/// Useful for RAM regions that change dynamically
/// </summary>
internal class DebuggerDataLiveProvider : IMemoryRegionDataProvider
{
    private const int BYTES_PER_LINE = 16;
    private const int VIEW_WIDTH = 256;
    private const int MAX_LINES_PER_CHUNK = 64;

    private readonly string mameViewName;
    private readonly UInt64 addressStart;
    private readonly UInt64 addressEnd;
    private LibMameDebugger? debugger;
    private int? sourceIndex;

    public string RegionName { get; }
    public UInt64 DataSize => addressEnd >= addressStart ? addressEnd - addressStart + 1 : 0;

    public DebuggerDataLiveProvider(string regionName, string mameViewName, UInt64 addressStart, UInt64 addressEnd)
    {
        RegionName = regionName;
        this.mameViewName = mameViewName;
        this.addressStart = addressStart;
        this.addressEnd = addressEnd;
    }

    public void LoadFromDebugger(LibMameDebugger debugger)
    {
        this.debugger = debugger;
        sourceIndex = null;
    }

    public byte GetByte(UInt64 address)
    {
        var span = FetchBytes(address, 1);
        if (span.Length == 0)
            return 0;
        return span[0];
    }

    public ReadOnlySpan<byte> FetchBytes(UInt64 address, UInt64 length)
    {
        if (debugger == null || length == 0)
            return new ReadOnlySpan<byte>();

        if (address < addressStart || address > addressEnd)
            return new ReadOnlySpan<byte>();

        var maxLength = addressEnd - address + 1;
        length = Math.Min(length, maxLength);

        var output = new byte[length];
        ReadBytesFromDebugger(address, output);
        return output;
    }

    private void ReadBytesFromDebugger(UInt64 address, byte[] output)
    {
        if (debugger == null || output.Length == 0)
            return;

        int outputOffset = 0;
        while (outputOffset < output.Length)
        {
            int remaining = output.Length - outputOffset;
            int lines = Math.Min(MAX_LINES_PER_CHUNK, (remaining + BYTES_PER_LINE - 1) / BYTES_PER_LINE);

            var view = new LibMameDebugger.DView(
                debugger.AllocView(LibMameDebuggerRetroPlugin.debug_view_type.Memory),
                0, 0, VIEW_WIDTH, lines, "");

            try
            {
                EnsureSource(ref view);

                view.view.Expression = $"${address + (UInt64)outputOffset:X}";
                debugger.SetExpression(ref view);
                debugger.SetDataFormat(ref view, LibMameDebuggerRetroPlugin.debug_format.DataFormat1ByteHex);
                debugger.UpdateDView(ref view);

                ParseChunk(view, address + (UInt64)outputOffset, output, outputOffset);
            }
            finally
            {
                debugger.FreeView(view.view);
            }

            outputOffset += lines * BYTES_PER_LINE;
        }
    }

    private void EnsureSource(ref LibMameDebugger.DView view)
    {
        if (debugger == null)
            return;

        if (!sourceIndex.HasValue)
        {
            int sourceCount = debugger.GetSourcesCount(ref view);
            var sources = debugger.GetSourcesList(ref view);

            int index = -1;
            for (int i = 0; i < sourceCount; i++)
            {
                if (sources[i].Contains(mameViewName))
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
                throw new Exception($"Could not find memory source for region: {mameViewName}");

            sourceIndex = index;
        }

        debugger.SetSource(ref view, sourceIndex.Value);
    }

    private void ParseChunk(LibMameDebugger.DView view, UInt64 chunkStartAddress, byte[] output, int outputOffset)
    {
        int bytesPerLine = view.view.W * 2; // Each character is 2 bytes (char + attribute)

        for (int y = 0; y < view.view.H; y++)
        {
            int lineStart = y * bytesPerLine;
            int x = 0;

            // Skip initial spaces
            while (x < view.view.W && (char)view.state[lineStart + x * 2] == ' ')
                x++;

            // Parse address
            StringBuilder addressStr = new StringBuilder();
            while (x < view.view.W && (char)view.state[lineStart + x * 2] != ' ')
            {
                addressStr.Append((char)view.state[lineStart + x * 2]);
                x++;
            }

            if (addressStr.Length == 0)
                break;

            var address = UInt64.Parse(addressStr.ToString(), System.Globalization.NumberStyles.HexNumber);

            // Skip spaces between address and bytes
            while (x < view.view.W && (char)view.state[lineStart + x * 2] == ' ')
                x++;

            // Parse bytes
            for (int byteCount = 0; byteCount < BYTES_PER_LINE && x < view.view.W - 1; byteCount++)
            {
                char highNibble = (char)view.state[lineStart + x * 2];
                char lowNibble = (char)view.state[lineStart + (x + 1) * 2];

                if (char.IsLetterOrDigit(highNibble) && char.IsLetterOrDigit(lowNibble))
                {
                    byte value = Convert.ToByte($"{highNibble}{lowNibble}", 16);
                    var absoluteAddress = address + (UInt64)byteCount;
                    if (absoluteAddress >= chunkStartAddress)
                    {
                        var outputIndex = (long)(absoluteAddress - chunkStartAddress) + outputOffset;
                        if (outputIndex >= 0 && outputIndex < output.Length)
                        {
                            output[outputIndex] = value;
                        }
                    }
                }
                else
                {
                    // Stop parsing this line if we hit invalid data
                    break;
                }

                x += 3;
            }
        }
    }
}
