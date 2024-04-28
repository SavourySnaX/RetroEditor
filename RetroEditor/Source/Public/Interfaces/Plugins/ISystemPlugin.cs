namespace RetroEditor.Plugins
{
    /// <summary>
    /// Defines a system plugin (e.g. Megadrive, NES, etc)
    /// </summary>
    public interface ISystemPlugin
    {
        /// <summary>
        /// Name of the system, used in IRetroPlugin to determine which system is needed for the particular game
        /// </summary>
        static abstract string Name { get; }
        /// <summary>
        /// Name of the libretro core plugin - used to retrieve and load the correct core
        /// </summary>
        string LibRetroPluginName { get; }
        /// <summary>
        /// Whether the system requires a reload of the core when the rom data is changed.
        /// In general cartridge based systems require a reload
        /// </summary>
        bool RequiresReload { get; }

        /// <summary>
        /// Memory Endianess of the system
        /// </summary>
        MemoryEndian Endian { get; }

        /// <summary>
        /// Checksum calculation for the system - used to recompute the checksum for systems that require it
        /// </summary>
        /// <param name="rom">Memory accessor</param>
        /// <param name="address">Offset to apply checksum to</param>
        /// <returns>Checksum bytes</returns>
        public ReadOnlySpan<byte> ChecksumCalculation(IMemoryAccess rom, out int address) { address = 0; return ReadOnlySpan<byte>.Empty; }
    }
}