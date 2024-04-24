using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;

internal class TileMapWidget : IWidgetItem, IWidgetUpdateDraw
{
    Texture2D[] bitmaps;
    ITileMap _iTileMap;

    float scale = 2.0f;

    int selectedTile = -1;

    public TileMapWidget(ITileMap iTileMap)
    {
        _iTileMap = iTileMap;
        bitmaps = new Texture2D[iTileMap.MaxTiles+1];
        var image = new Image
        {
            Width = (int)iTileMap.Width,
            Height = (int)iTileMap.Height,
            Mipmaps = 1,
            Format = PixelFormat.UncompressedR8G8B8A8
        };
        bitmaps[0]=Raylib.LoadTextureFromImage(image);
        var tiles = iTileMap.FetchTiles(0);
        for (int a=0;a<tiles.Length;a++)
        {
            image = new Image
            {
                Width = (int)tiles[a].Width,
                Height = (int)tiles[a].Height,
                Mipmaps = 1,
                Format = PixelFormat.UncompressedR8G8B8A8
            };
            bitmaps[a+1] = Raylib.LoadTextureFromImage(image);
        }
    }

    public void Update(float seconds)
    {
        _iTileMap.Update(seconds);

        // Grab latest version of tile data?
        var tiles = _iTileMap.FetchTiles(0);
        for (int a=0;a<tiles.Length;a++)
        {
            var pixels = tiles[a].GetImageData();

            byte[] bitmapData = new byte[pixels.Length*4];
            for (int b = 0; b < pixels.Length; b++)
            {
                bitmapData[b * 4 + 0] = pixels[b].Red;
                bitmapData[b * 4 + 1] = pixels[b].Green;
                bitmapData[b * 4 + 2] = pixels[b].Blue;
                bitmapData[b * 4 + 3] = pixels[b].Alpha;
            }

            Raylib.UpdateTexture(bitmaps[a+1], bitmapData);
        }
    }
    
    public void Draw()
    {
        var tiles = _iTileMap.FetchTiles(0);
        for (int a=0;a<tiles.Length;a++)
        {
            ImGui.BeginGroup();
            rlImGui.ImageSize(bitmaps[a + 1], (int)(tiles[a].Width * scale), (int)(tiles[a].Height * scale));
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

        var currentPos = ImGui.GetCursorPos();
        // Grab latest version of map data?
        for (int a=0;a<_iTileMap.NumLayers;a++)
        {
            var layer = _iTileMap.FetchLayer((uint)a);

            var mapData = layer.GetMapData();

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
                    rlImGui.ImageSize(bitmaps[1+tilenum], (int)(tileData.Width * scale), (int)(tileData.Height * scale));

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
    }
}
