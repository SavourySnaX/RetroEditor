
namespace SuperNintendoEntertainmentSystem.Memory
{
    /// <summary>
    /// Provides address translation between CPU addresses and ROM image offsets (interface).
    /// </summary>
    public interface AddressTranslation
    {
        /// <summary>
        /// Translates a CPU address to a ROM image offset.
        /// </summary>
        /// <param name="address">The CPU address to translate.</param>
        /// <returns>The corresponding ROM image offset.</returns>
        uint ToImage(uint address);

        /// <summary>
        /// Translates a ROM image offset to a CPU address.
        /// </summary>
        /// <param name="address">The ROM image offset to translate.</param>
        /// <returns>The corresponding CPU address.</returns>
        uint FromImage(uint address);
    }
}