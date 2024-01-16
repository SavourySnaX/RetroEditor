
using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;

public class ImageWindow : IWindow
{
    Texture2D bitmap;
    IImage map;
    IRetroPlugin plugin;

    float scale = 2.0f;

    public ImageWindow(IRetroPlugin plugin,IImage map)
    {
        this.plugin = plugin; 
        this.map = map;
    }

    public bool Initialise()
    {
        var image = new Image
        {
            Width = (int)map.Width,
            Height = (int)map.Height,
            Mipmaps = 1,
            Format = PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8
        };
        bitmap=Raylib.LoadTextureFromImage(image);
        return true;
    }

    public void Update(float seconds)
    {
        var pixels = map.GetImageData(seconds);

        byte[] bitmapData = new byte[pixels.Length*4];
        for (int a = 0; a < pixels.Length; a++)
        {
            bitmapData[a * 4 + 0] = pixels[a].Red;
            bitmapData[a * 4 + 1] = pixels[a].Green;
            bitmapData[a * 4 + 2] = pixels[a].Blue;
            bitmapData[a * 4 + 3] = 255;
        }

        Raylib.UpdateTexture(bitmap, bitmapData);
    }

    public float UpdateInterval => 1.0f / 60.0f;

    public bool Draw()
    {
        bool open = true;
        ImGui.Begin($"Image Viewer - {plugin.Name} - {map.Name}",ref open);

        rlImGui.ImageSize(bitmap, (int)(map.Width * scale), (int)(map.Height * scale));

        ImGui.End();

        return open;
    }

    public void Close()
    {
    }
}