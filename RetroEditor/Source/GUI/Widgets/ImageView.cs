
using Raylib_cs;
using rlImGui_cs;

internal class ImageView : IWidgetItem, IWidgetUpdateDraw
{
    private Texture2D _bitmap;
    private IImage _iImage;

    public ImageView(IImage iImage)
    {
        _iImage = iImage;

        var image = new Image
        {
            Width = (int)iImage.Width,
            Height = (int)iImage.Height,
            Mipmaps = 1,
            Format = PixelFormat.UncompressedR8G8B8A8
        };
        _bitmap=Raylib.LoadTextureFromImage(image);
    }

    public void Update(float seconds)
    {
        var pixels = _iImage.GetImageData(seconds);

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
        rlImGui.ImageSize(_bitmap, (int)(_iImage.Width * _iImage.ScaleX), (int)(_iImage.Height * _iImage.ScaleY));
    }
}