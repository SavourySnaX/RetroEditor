
using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;

public class BitmapWindow : IWindow
{
    Texture2D bitmap;
    IBitmapImage map;
    IRetroPlugin plugin;

    float scale = 2.0f;

    public BitmapWindow(IRetroPlugin plugin,IBitmapImage map)
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
        // Draw palette :
        var palette = map.Palette;
        var pixels = map.GetImageData(seconds);

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

    public float UpdateInterval => 1.0f / 30.0f;

    int selectedColour = -1;

    public bool Draw()
    {
        var palette = map.Palette;
        var pixels = map.GetImageData(0.0f);
        rlImGui.ImageSize(bitmap, (int)(map.Width * scale), (int)(map.Height * scale));

        ImGui.Text("Palette");

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

        size = new Vector2(map.Width * w, map.Height * h);
        ImGui.BeginChild($"pixels", size, 0, 0);
        pos = ImGui.GetCursorScreenPos();
        {
            var mousePos = ImGui.GetMousePos();
            var localPos = mousePos - pos;
            if (localPos.X >= 0 && localPos.Y >= 0 && localPos.X < size.X && localPos.Y < size.Y)
            {
                var x = (uint)(localPos.X / w);
                var y = (uint)(localPos.Y / h);

                if (x >= 0 && x < map.Width && y >= 0 && y < map.Height)
                {
                    if (selectedColour >= 0 && ImGui.IsMouseDown(ImGuiMouseButton.Left))
                    {
                        map.SetPixel(x, y, (uint)selectedColour);
                        pixels[x + y * map.Width] = (uint)selectedColour;
                    }
                    else if (ImGui.IsMouseDown(ImGuiMouseButton.Right))
                    {
                        selectedColour = (int)pixels[x + y * map.Width];
                    }
                }
            }
        }

        drawList = ImGui.GetWindowDrawList();

        for (int y=0;y<map.Height;y++)
        {
            for (int x=0;x<map.Width;x++)
            {
                var pixel = pixels[x + y * map.Width];
                var colour = palette[pixel];
                drawList.AddRectFilled(new Vector2(pos.X + x * w, pos.Y + y * w), new Vector2(pos.X + x * w + w, pos.Y + y * h + h), MakeColour(colour.Red, colour.Green, colour.Blue, colour.Alpha));
            }
        }

        ImGui.EndChild();
        /*
        for (int y=0;y<map.Height;y++)
        {
            for (int x=0;x<map.Width;x++)
            {
                var pixel = pixels[x + y * map.Width];
                var colour = palette[pixel];
                Raylib.DrawRectangle(40+x*2, 10+y*2, 2, 2, new Color(colour.Red, colour.Green, colour.Blue, colour.Alpha));
            }
        }*/
        return false;
    }

    uint MakeColour(byte r, byte g, byte b, byte a)
    {
        return (uint)((r << 24) | (g << 16) | (b << 8) | a);
    }

    public void Close()
    {
    }
}