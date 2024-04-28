namespace RetroEditor.Plugins
{
    /// <summary>
    /// Memory region to read from
    /// </summary>
    public enum ReadKind
    {
        /// <summary>
        /// Read from RAM
        /// </summary>
        Ram = 0,
        /// <summary>
        /// Read from ROM
        /// </summary>
        Rom = 1,
    }

    /// <summary>
    /// Memory region to write to
    /// </summary>
    public enum WriteKind
    {
        /// <summary>
        /// Write to RAM (does not persist to final modded game - use for cheats, etc)
        /// </summary>
        TemporaryRam = 0,
        /// <summary>
        /// Write to ROM (does not persist to final modded game - use for cheats, etc)
        /// </summary>
        TemporaryRom = 1,
        /// <summary>
        /// Write to RAM (persists to final modded game - assuming ram area saved to disk)
        /// </summary>
        SerialisedRam = 0x1000,
        /// <summary>
        /// Write to ROM (persists to final modded game - assuming rom area saved to disk)
        /// </summary>
        SerialisedRom = 0x1001,
    }


    /// <summary>
    /// Endianess of memory - used to determine how to read/write multi-byte values
    /// </summary>
    public enum MemoryEndian
    {
        /// <summary>
        /// Little endian - least significant byte first
        /// </summary>
        Little,
        /// <summary>
        /// Big endian - most significant byte first
        /// </summary>
        Big,
    }

    /// <summary>
    /// Interface for accessing memory
    /// </summary>
    public interface IMemoryAccess
    {
        /// <summary>
        /// Read a number of bytes from the specified memory area
        /// </summary>
        /// <param name="kind">Memory kind</param>
        /// <param name="address">Address of first byte</param>
        /// <param name="length">Number of bytes to retrieve</param>
        /// <returns></returns>
        public ReadOnlySpan<byte> ReadBytes(ReadKind kind, uint address, uint length);
        /// <summary>
        /// Write a number of bytes to the specified memory area
        /// </summary>
        /// <param name="kind">Memory kind</param>
        /// <param name="address">Address to write to</param>
        /// <param name="bytes">Bytes to write to address</param>
        public void WriteBytes(WriteKind kind, uint address, ReadOnlySpan<byte> bytes);

        /// <summary>
        /// Size of the loaded rom (only applies to cartridge based systems)
        /// </summary>
        /// <returns>Size of rom in bytes</returns>
        public int RomSize { get; }

        /// <summary>
        /// Memory Endian of the system
        /// </summary>
        /// <returns>Endianess of memory</returns>
        public MemoryEndian Endian { get; }

        /// <summary>
        /// Given a byte array and offset, returns a 16 bit value in machine order
        /// </summary>
        /// <param name="offset">offset in bytes</param>
        /// <param name="bytes">array of bytes</param>
        /// <returns>16 bit value from requested offset</returns>
        public UInt16 FetchMachineOrder16(int offset, ReadOnlySpan<byte> bytes)
        {
            if (Endian == MemoryEndian.Little)
            {
                return (UInt16)(bytes[offset] + (bytes[offset + 1] << 8));
            }
            else
            {
                return (UInt16)((bytes[offset] << 8) + bytes[offset + 1]);
            }
        }

        /// <summary>
        /// Given a byte array and offset, returns a 32 bit value in machine order
        /// </summary>
        /// <param name="offset">offset in bytes</param>
        /// <param name="bytes">array of bytes</param>
        /// <returns>32 bit value from requested offset</returns>
        public UInt32 FetchMachineOrder32(int offset, ReadOnlySpan<byte> bytes)
        {
            if (Endian == MemoryEndian.Little)
            {
                return (UInt32)(bytes[offset] + (bytes[offset + 1] << 8) + (bytes[offset + 2] << 16) + (bytes[offset + 3] << 24));
            }
            else
            {
                return (UInt32)((bytes[offset] << 24) + (bytes[offset + 1] << 16) + (bytes[offset + 2] << 8) + bytes[offset + 3]);
            }
        }

        /// <summary>
        /// Given a byte array and offset, writes a 16bit value in machine order
        /// </summary>
        /// <param name="offset">offset in bytes</param>
        /// <param name="bytes">array of bytes</param>
        /// <param name="value">value to write</param>
        /// <returns>16 bit value from requested offset</returns>
        public void WriteMachineOrder16(int offset, Span<byte> bytes, UInt16 value)
        {
            if (Endian == MemoryEndian.Little)
            {
                bytes[offset] = (byte)(value & 0xff);
                bytes[offset + 1] = (byte)((value >> 8) & 0xff);
            }
            else
            {
                bytes[offset] = (byte)((value >> 8) & 0xff);
                bytes[offset + 1] = (byte)(value & 0xff);
            }
        }

        /// <summary>
        /// Given a byte array and offset, writes a 32bit value in machine order
        /// </summary>
        /// <param name="offset">offset in bytes</param>
        /// <param name="bytes">array of bytes</param>
        /// <param name="value">value to write</param>
        /// <returns>32 bit value from requested offset</returns>
        public void WriteMachineOrder32(int offset, Span<byte> bytes, UInt32 value)
        {
            if (Endian == MemoryEndian.Little)
            {
                bytes[offset] = (byte)(value & 0xff);
                bytes[offset + 1] = (byte)((value >> 8) & 0xff);
                bytes[offset + 2] = (byte)((value >> 16) & 0xff);
                bytes[offset + 3] = (byte)((value >> 24) & 0xff);
            }
            else
            {
                bytes[offset] = (byte)((value >> 24) & 0xff);
                bytes[offset + 1] = (byte)((value >> 16) & 0xff);
                bytes[offset + 2] = (byte)((value >> 8) & 0xff);
                bytes[offset + 3] = (byte)(value & 0xff);
            }
        }

        /// <summary>
        /// Given a byte array and offset, returns a 16 bit value in opposite machine order
        /// </summary>
        /// <param name="offset">offset in bytes</param>
        /// <param name="bytes">array of bytes</param>
        /// <returns>16 bit value from requested offset</returns>
        public UInt16 FetchOppositeMachineOrder16(int offset, ReadOnlySpan<byte> bytes)
        {
            if (Endian != MemoryEndian.Little)
            {
                return (UInt16)(bytes[offset] + (bytes[offset + 1] << 8));
            }
            else
            {
                return (UInt16)((bytes[offset] << 8) + bytes[offset + 1]);
            }
        }

        /// <summary>
        /// Given a byte array and offset, returns a 32 bit value in opposite machine order
        /// </summary>
        /// <param name="offset">offset in bytes</param>
        /// <param name="bytes">array of bytes</param>
        /// <returns>32 bit value from requested offset</returns>
        public UInt32 FetchOppositeMachineOrder32(int offset, ReadOnlySpan<byte> bytes)
        {
            if (Endian != MemoryEndian.Little)
            {
                return (UInt32)(bytes[offset] + (bytes[offset + 1] << 8) + (bytes[offset + 2] << 16) + (bytes[offset + 3] << 24));
            }
            else
            {
                return (UInt32)((bytes[offset] << 24) + (bytes[offset + 1] << 16) + (bytes[offset + 2] << 8) + bytes[offset + 3]);
            }
        }

        /// <summary>
        /// Given a byte array and offset, writes a 16bit value in opposite machine order
        /// </summary>
        /// <param name="offset">offset in bytes</param>
        /// <param name="bytes">array of bytes</param>
        /// <param name="value">value to write</param>
        /// <returns>16 bit value from requested offset</returns>
        public void WriteOppositeMachineOrder16(int offset, Span<byte> bytes, UInt16 value)
        {
            if (Endian != MemoryEndian.Little)
            {
                bytes[offset] = (byte)(value & 0xff);
                bytes[offset + 1] = (byte)((value >> 8) & 0xff);
            }
            else
            {
                bytes[offset] = (byte)((value >> 8) & 0xff);
                bytes[offset + 1] = (byte)(value & 0xff);
            }
        }

        /// <summary>
        /// Given a byte array and offset, writes a 32bit value in opposite machine order
        /// </summary>
        /// <param name="offset">offset in bytes</param>
        /// <param name="bytes">array of bytes</param>
        /// <param name="value">value to write</param>
        /// <returns>32 bit value from requested offset</returns>
        public void WriteOppositeMachineOrder32(int offset, Span<byte> bytes, UInt32 value)
        {
            if (Endian != MemoryEndian.Little)
            {
                bytes[offset] = (byte)(value & 0xff);
                bytes[offset + 1] = (byte)((value >> 8) & 0xff);
                bytes[offset + 2] = (byte)((value >> 16) & 0xff);
                bytes[offset + 3] = (byte)((value >> 24) & 0xff);
            }
            else
            {
                bytes[offset] = (byte)((value >> 24) & 0xff);
                bytes[offset + 1] = (byte)((value >> 16) & 0xff);
                bytes[offset + 2] = (byte)((value >> 8) & 0xff);
                bytes[offset + 3] = (byte)(value & 0xff);
            }
        }
    }
}