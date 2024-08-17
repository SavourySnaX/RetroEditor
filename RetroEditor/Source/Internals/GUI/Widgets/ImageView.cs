
using Raylib_cs;
using RetroEditor.Plugins;
using rlImGui_cs;

internal class ImageView : IWidgetItem, IWidgetUpdateDraw
{
    private Texture2D _bitmap;
    private IImage _iImage;
    private byte[] _bitmapData;

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
        _bitmapData = new byte[image.Width * image.Height * 4];
        _bitmap = Raylib.LoadTextureFromImage(image);
    }

    public void Update(IWidgetLog logger, float seconds)
    {
        ReadOnlySpan<Pixel> pixels = default;
        try
        {
            pixels = _iImage.GetImageData(seconds);
        }
        catch (Exception e)
        {
            logger.Log(LogType.Error, $"Failed to get image data - {e.Message}");
            return;
        }
        if (pixels == null)
        {
            logger.Log(LogType.Error, $"Failed to get image data - GetImageData returned null");
            return;
        }
        if (pixels.Length != _bitmapData.Length / 4)
        {
            logger.Log(LogType.Error, $"Image data length mismatch expected {_bitmapData.Length / 4} got {pixels.Length} total pixels");
            return;
        }

        Raylib.UpdateTexture(_bitmap, pixels.ToArray());
    }

    public void Draw(IWidgetLog logger)
    {
        int width, height;
        try
        {
            width = (int)(_iImage.Width * _iImage.ScaleX);
            height = (int)(_iImage.Height * _iImage.ScaleY);
        }
        catch (Exception e)
        {
            logger.Log(LogType.Error, $"Failed to get image size - {e.Message}");
            return;

        }
        rlImGui.ImageSize(_bitmap, (int)(_iImage.Width * _iImage.ScaleX), (int)(_iImage.Height * _iImage.ScaleY));
    }
}