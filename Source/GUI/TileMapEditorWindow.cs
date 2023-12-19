

using System.Numerics;
using ImGuiNET;
using Veldrid;
using Vulkan.Xcb;

public class TileMapEditorWindow : IWindow
{
    Texture[] bitmaps;
    ITileMap map;
    IRetroPlugin plugin;
    nint[] bitmapIds;

    float scale = 2.0f;

    int selectedTile = -1;
    public TileMapEditorWindow(IRetroPlugin plugin, ITileMap map)
    {
        this.plugin = plugin;
        this.map = map;
        bitmaps = Array.Empty<Texture>();
        bitmapIds = Array.Empty<nint>();
    }

    public bool Initialise(ImGuiController controller,GraphicsDevice graphicsDevice)
    {
        bitmaps = new Texture[map.MaxTiles+1];
        bitmapIds = new nint[map.MaxTiles+1];
        bitmaps[0]=graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(map.Width, map.Height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
        bitmaps[0].Name = $"TestMap{plugin.Name}{map.Name}";
        bitmapIds[0] = controller.GetOrCreateImGuiBinding(graphicsDevice.ResourceFactory, bitmaps[0]);
        var tiles = map.FetchTiles(0);
        for (int a=0;a<tiles.Length;a++)
        {
            bitmaps[a+1] = graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(tiles[a].Width, tiles[a].Height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
            bitmaps[a+1].Name = $"TestMap{plugin.Name}{map.Name}_Tile{a}";
            bitmapIds[a+1] = controller.GetOrCreateImGuiBinding(graphicsDevice.ResourceFactory, bitmaps[a+1]);
        }
        return true;
    }

    public void Update(ImGuiController controller,GraphicsDevice graphicsDevice, float seconds)
    {
        map.Update(seconds);

        // Grab latest version of tile data?
        var tiles = map.FetchTiles(0);
        for (int a=0;a<tiles.Length;a++)
        {
            var pixels = tiles[a].GetImageData();

            RgbaByte[] bitmapData = new RgbaByte[pixels.Length];
            for (int b=0;b<pixels.Length;b++)
            {
                bitmapData[b] = new RgbaByte(pixels[b].Red, pixels[b].Green, pixels[b].Blue, pixels[b].Alpha);
            }

            graphicsDevice.UpdateTexture(bitmaps[a+1], bitmapData, 0, 0, 0, tiles[a].Width, tiles[a].Height, 1, 0, 0);
        }
/*
        // Grab latest version of map data?
        for (int a=0;a<map.NumLayers;a++)
        {
            var layer = map.FetchLayer((uint)a);

            var mapData = layer.GetMapData();

            var bitmap = new RgbaByte[map.Width * map.Height];

            for (uint y = 0; y < layer.Height; y++)
            {
                for (uint x = 0; x < layer.Width; x++)
                {
                    var tileData = tiles[mapData[y * layer.Width + x]];
                    var tile = tileData.GetImageData();

                    uint xpos = x * tileData.Width;
                    uint ypos = y * tileData.Height;

                    for (uint ty = 0; ty < tileData.Height; ty++)
                    {
                        for (uint tx = 0; tx < tileData.Width; tx++)
                        {
                            var tilePixel = tile[ty * tileData.Width + tx];
                            bitmap[(ypos + ty) * 256 + (xpos + tx)] = new RgbaByte(tilePixel.Red, tilePixel.Green, tilePixel.Blue, tilePixel.Alpha);
                        }
                    }
                }
            }
            graphicsDevice.UpdateTexture(bitmaps[0], bitmap, 0, 0, 0, map.Width, map.Height, 1, 0, 0);
        }*/
    }

    public bool Draw()
    {
        bool open = true;
        ImGui.Begin($"Tile Map Editor - {plugin.Name} - {map.Name}",ref open);

        var tiles = map.FetchTiles(0);
        for (int a=0;a<tiles.Length;a++)
        {
            ImGui.BeginGroup();
            ImGui.Image(bitmapIds[a + 1], new Vector2(tiles[a].Width * scale, tiles[a].Height * scale));
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text(tiles[a].Name);
                ImGui.EndTooltip();
            }
            if (ImGui.IsItemClicked())
            {
                if (selectedTile ==a)
                {
                    selectedTile = -1;
                }
                else
                {
                    selectedTile = a;
                }
            }
            ImGui.EndGroup();
        }

//        ImGui.Image(bitmapIds[0], new Vector2(map.Width * scale, map.Height * scale));

        var currentPos = ImGui.GetCursorPos();
        // Grab latest version of map data?
        for (int a=0;a<map.NumLayers;a++)
        {
            var layer = map.FetchLayer((uint)a);

            var mapData = layer.GetMapData();

            var bitmap = new RgbaByte[map.Width * map.Height];

            for (uint y = 0; y < layer.Height; y++)
            {
                for (uint x = 0; x < layer.Width; x++)
                {
                    var tilenum = mapData[y * layer.Width + x];
                    var tileData = tiles[tilenum];
                    var tile = tileData.GetImageData();

                    uint xpos = x * tileData.Width;
                    uint ypos = y * tileData.Height;

                    ImGui.SetCursorPos(new Vector2((currentPos.X + xpos) * scale, (currentPos.Y + ypos) * scale));
                    ImGui.Image(bitmapIds[1+tilenum], new Vector2(tileData.Width * scale, tileData.Height * scale));

                    if (ImGui.IsItemHovered() && ImGui.IsMouseDown(ImGuiMouseButton.Left))
                    {
                        if (selectedTile!=-1)
                        {
                            layer.SetTile(x, y, (uint)selectedTile);
                        }
                    }

                }
            }
        }


        ImGui.End();

        return open;
    }
    public void Close(ImGuiController controller,GraphicsDevice graphicsDevice)
    {
        map.Close();
    }
}

