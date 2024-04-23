
using Raylib_cs;
using rlImGui_cs;

internal class ImageView : IWidgetItem, IWidgetUpdateDraw
{
    private Texture2D _bitmap;
    private IImage _map;

    public ImageView(IImage map)
    {
        _map = map;

        var image = new Image
        {
            Width = (int)map.Width,
            Height = (int)map.Height,
            Mipmaps = 1,
            Format = PixelFormat.UncompressedR8G8B8A8
        };
        _bitmap=Raylib.LoadTextureFromImage(image);
    }

    public void Update(float seconds)
    {
        var pixels = _map.GetImageData(seconds);

        byte[] bitmapData = new byte[pixels.Length*4];
        for (int a = 0; a < pixels.Length; a++)
        {
            bitmapData[a * 4 + 0] = pixels[a].Red;
            bitmapData[a * 4 + 1] = pixels[a].Green;
            bitmapData[a * 4 + 2] = pixels[a].Blue;
            bitmapData[a * 4 + 3] = 255;
        }

        Raylib.UpdateTexture(_bitmap, bitmapData);
    }

    public void Draw()
    {
        rlImGui.ImageSize(_bitmap, (int)(_map.Width * _map.ScaleX), (int)(_map.Height * _map.ScaleY));
    }
}