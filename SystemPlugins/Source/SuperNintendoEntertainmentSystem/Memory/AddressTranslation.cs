
namespace SuperNintendoEntertainmentSystem.Memory
{
    public interface AddressTranslation
    {
        uint ToImage(uint address);
        uint FromImage(uint address);
    }
}