
namespace SuperNintendoEntertainmentSystem.Memory
{
    public class LoRom : AddressTranslation
    {
        private bool _isHeadered;
        private uint bankOr;
        public LoRom(bool isHeadered, bool bank80)
        {
            _isHeadered = isHeadered;
            bankOr = bank80 ? 0x80u : 0x00;
        }

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

        public uint FromImage(uint address)
        {
            uint bank = address / 0x8000;
            bank |= bankOr; // Set the bank to 80 if needed
            uint rom = address % 0x8000;
            return (bank << 16) | rom;
        }
    }
}