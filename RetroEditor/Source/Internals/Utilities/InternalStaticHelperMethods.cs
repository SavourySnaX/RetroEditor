
internal static class ImGuiHelper
{
    internal static uint MakeColour(byte r, byte g, byte b, byte a)
    {
        return (uint)((r << 24) | (g << 16) | (b << 8) | a);
    }

}