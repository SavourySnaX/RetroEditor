
namespace SuperNintendoEntertainmentSystem.Memory
{
    /// <summary>
    /// Provides address translation for SNES LoROM cartridge format.
    /// Handles conversion between CPU address space and ROM image offsets.
    /// </summary>
    public class LoRom : AddressTranslation
    {
        private bool _isHeadered;
        private uint bankOr;

        /// <summary>
        /// Initializes a new instance of the LoRom class.
        /// </summary>
        /// <param name="isHeadered">Whether the ROM has a 512-byte header.</param>
        /// <param name="bank80">Whether to set the bank base to 0x80 or 0x00.</param>
        public LoRom(bool isHeadered, bool bank80)
        {
            _isHeadered = isHeadered;
            bankOr = bank80 ? 0x80u : 0x00;
        }

        /// <summary>
        /// Converts a CPU address to a ROM image offset.
        /// </summary>
        /// <param name="address">The CPU address in the 0x8000-0xFFFF range per bank.</param>
        /// <returns>The corresponding offset in the ROM image.</returns>
        /// <exception cref="System.Exception">Thrown when the address is invalid (below 0x8000).</exception>
        public uint ToImage(uint address)
        {
            uint bank = (address >> 16) & 0x7F;
            uint rom = address & 0xFFFF;

            if (rom < 0x8000)
            {
                throw new System.Exception("Invalid address");
            }
            rom -= 0x8000;
            return bank * 0x8000 + rom;
        }

        /// <summary>
        /// Converts a ROM image offset to a CPU address.
        /// </summary>
        /// <param name="address">The offset in the ROM image.</param>
        /// <returns>The corresponding CPU address with bank in the upper byte.</returns>
        public uint FromImage(uint address)
        {
            uint bank = address / 0x8000;
            bank |= bankOr; // Set the bank to 80 if needed
            uint rom = address % 0x8000;
            return (bank << 16) | rom;
        }
    }
}