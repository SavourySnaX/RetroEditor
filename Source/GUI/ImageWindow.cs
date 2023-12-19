

using System.Numerics;
using ImGuiNET;
using Veldrid;

public class ImageWindow : IWindow
{
    Texture? bitmap;
    IImage map;
    IRetroPlugin plugin;
    nint bitmapId;

    float scale = 2.0f;

    public ImageWindow(IRetroPlugin plugin,IImage map)
    {
        this.plugin = plugin; 
        this.map = map;
    }

    public bool Initialise(ImGuiController controller,GraphicsDevice graphicsDevice)
    {
        bitmap=graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(map.Width, map.Height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
        bitmap.Name = $"TestMap{plugin.Name}{map.Name}";
        bitmapId = controller.GetOrCreateImGuiBinding(graphicsDevice.ResourceFactory, bitmap);
        return true;
    }

    public void Update(ImGuiController controller,GraphicsDevice graphicsDevice, float seconds)
    {
        var pixels = map.GetImageData(seconds);

        RgbaByte[] bitmapData = new RgbaByte[pixels.Length];
        for (int a=0;a<pixels.Length;a++)
        {
            bitmapData[a] = new RgbaByte(pixels[a].Red, pixels[a].Green, pixels[a].Blue, 255);
        }

        graphicsDevice.UpdateTexture(bitmap, bitmapData, 0, 0, 0, map.Width, map.Height, 1, 0, 0);
    }

    public bool Draw()
    {
        bool open = true;
        ImGui.Begin($"Image Viewer - {plugin.Name} - {map.Name}",ref open);

        ImGui.Image(bitmapId, new Vector2(map.Width * scale, map.Height * scale));

        ImGui.End();

        return open;
    }

    public void Close(ImGuiController controller,GraphicsDevice graphicsDevice)
    {
    }
}