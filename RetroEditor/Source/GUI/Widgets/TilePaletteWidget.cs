using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using RetroEditor.Plugins;


internal class TilePaletteWidget : IWidgetItem, IWidgetUpdateDraw
{
    private TilePaletteStore _tilePaletteStore;

    public TilePaletteWidget(TilePaletteStore store)
    {
        _tilePaletteStore = store;
        _tilePaletteStore.TilePalette.SelectedTile = -1;
    }

    public void Update(float seconds)
    {
        _tilePaletteStore.Update(seconds);
    }
    
    public void Draw()
    {
        var tilePalette = _tilePaletteStore.TilePalette;
        var tiles = tilePalette.FetchTiles();
        var width = (uint)(_tilePaletteStore.LargestWidth * tilePalette.ScaleX);
        var height = (uint)(_tilePaletteStore.LargestHeight * tilePalette.ScaleY);
        var tilesPerRow = tilePalette.TilesPerRow;

        // Compute the size of the palette
        var rows = (tiles.Length + tilesPerRow - 1) / tilesPerRow;

        var size = new Vector2(width * tilesPerRow + 4 * tilesPerRow, height * rows + 4 * rows);
        ImGui.BeginChild($"tilepalette", size, 0, 0);

        var drawList = ImGui.GetWindowDrawList();
        Vector2 pos = ImGui.GetCursorScreenPos();
        var mousePos = ImGui.GetMousePos();
        var localPos = mousePos - pos;

        int a = 0;
        uint yOffs = 0;
        for (int y = 0; y < rows; y++)
        {
            uint xOffs = 0;
            for (int x = 0; x < tilesPerRow; x++)
            {
                if (tilePalette.SelectedTile == a)
                {
                    drawList.AddRect(new Vector2(pos.X + xOffs, pos.Y + yOffs), new Vector2(pos.X + xOffs + width + 4, pos.Y + yOffs + height + 4), ImGuiHelper.MakeColour(255, 255, 255, 255));
                }
                drawList.AddImage((nint)_tilePaletteStore.Bitmaps[a].Id, new Vector2(pos.X + xOffs+2, pos.Y + yOffs+2), new Vector2(pos.X + xOffs + width+2, pos.Y + yOffs + height+2));
                if (localPos.X >= xOffs && localPos.Y >= yOffs && localPos.X < xOffs + width && localPos.Y < yOffs + height)
                {
                    ImGui.BeginTooltip();
                    ImGui.Text(tiles[a].Name);
                    ImGui.EndTooltip();
                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        if (tilePalette.SelectedTile == a)
                        {
                            tilePalette.SelectedTile = -1;
                        }
                        else
                        {
                            tilePalette.SelectedTile = a;
                        }
                    }
                }
                a++;
                xOffs += width + 4;
            }
            yOffs += height + 4;
        }

        ImGui.EndChild();
    }
}

