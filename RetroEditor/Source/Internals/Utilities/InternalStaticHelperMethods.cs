using MyMGui;
internal static class ImGuiHelper
{
    internal static ImCol MakeColour(byte r, byte g, byte b, byte a)
    {
        return new ImCol((uint)((r << 24) | (g << 16) | (b << 8) | a));
    }

}