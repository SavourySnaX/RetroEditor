
using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using RetroEditor.Plugins;
using rlImGui_cs;

/// <summary>
/// A widget that displays a bitmap image and allows editing of the image. (transitional, will be split later)
/// </summary>
internal class BitmapWidget : IWidgetItem, IWidgetUpdateDraw
{
    Texture2D bitmap;
    IBitmapImage iBitmap;

    float scale = 2.0f;

    public BitmapWidget(IBitmapImage iBitmap)
    {
        this.iBitmap = iBitmap;
        var image = new Image
        {
            Width = (int)iBitmap.Width,
            Height = (int)iBitmap.Height,
            Mipmaps = 1,
            Format = PixelFormat.UncompressedR8G8B8A8
        };
        bitmap=Raylib.LoadTextureFromImage(image);
    }

    public void Update(float seconds)
    {
        // Draw palette :
        var palette = iBitmap.Palette;
        var pixels = iBitmap.GetImageData(seconds);

        byte[] bitmapData = new byte[pixels.Length*4];
        for (int a = 0; a < pixels.Length; a++)
        {
            var colour = palette[pixels[a]];
            bitmapData[a * 4 + 0] = colour.Red;
            bitmapData[a * 4 + 1] = colour.Green;
            bitmapData[a * 4 + 2] = colour.Blue;
            bitmapData[a * 4 + 3] = colour.Alpha;
        }

        Raylib.UpdateTexture(bitmap, bitmapData);

    }

    int selectedColour = -1;

    public void Draw()
    {
        var palette = iBitmap.Palette;
        var pixels = iBitmap.GetImageData(0.0f);
        rlImGui.ImageSize(bitmap, (int)(iBitmap.Width * scale), (int)(iBitmap.Height * scale));

        ImGui.Text("Palette");

        // ColourPaletteWidget & interface
        int w = 20;
        int h = 20;

        var size = new Vector2(w,h*palette.Length);
        ImGui.BeginChild($"palette", size, 0, 0);

        var drawList = ImGui.GetWindowDrawList();
        Vector2 pos = ImGui.GetCursorScreenPos();

        for (int a=0;a<palette.Length;a++)
        {
            var colour = palette[a];
            drawList.AddRectFilled(new Vector2(pos.X, pos.Y + a * h), new Vector2(pos.X + w, pos.Y + a * h + h), MakeColour(colour.Red, colour.Green, colour.Blue, colour.Alpha));
        }

        ImGui.EndChild();

        ImGui.Text("Edit");

        size = new Vector2(iBitmap.Width * w, iBitmap.Height * h);
        ImGui.BeginChild($"pixels", size, 0, 0);
        pos = ImGui.GetCursorScreenPos();
        {
            var mousePos = ImGui.GetMousePos();
            var localPos = mousePos - pos;
            if (localPos.X >= 0 && localPos.Y >= 0 && localPos.X < size.X && localPos.Y < size.Y)
            {
                var x = (uint)(localPos.X / w);
                var y = (uint)(localPos.Y / h);

                if (x >= 0 && x < iBitmap.Width && y >= 0 && y < iBitmap.Height)
                {
                    if (selectedColour >= 0 && ImGui.IsMouseDown(ImGuiMouseButton.Left))
                    {
                        iBitmap.SetPixel(x, y, (uint)selectedColour);
                        pixels[x + y * iBitmap.Width] = (uint)selectedColour;
                    }
                    else if (ImGui.IsMouseDown(ImGuiMouseButton.Right))
                    {
                        selectedColour = (int)pixels[x + y * iBitmap.Width];
                    }
                }
            }
        }

        drawList = ImGui.GetWindowDrawList();

        for (int y=0;y<iBitmap.Height;y++)
        {
            for (int x=0;x<iBitmap.Width;x++)
            {
                var pixel = pixels[x + y * iBitmap.Width];
                var colour = palette[pixel];
                drawList.AddRectFilled(new Vector2(pos.X + x * w, pos.Y + y * w), new Vector2(pos.X + x * w + w, pos.Y + y * h + h), MakeColour(colour.Red, colour.Green, colour.Blue, colour.Alpha));
            }
        }

        ImGui.EndChild();
    }

    uint MakeColour(byte r, byte g, byte b, byte a)
    {
        return (uint)((r << 24) | (g << 16) | (b << 8) | a);
    }

}
