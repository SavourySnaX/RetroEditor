using MyMGui;
using RetroEditor.Plugins;

internal class TileMapWidget : IWidgetItem, IWidgetUpdateDraw
{
    private ITileMap _iTileMap;

    public TileMapWidget(ITileMap iTileMap)
    {
        _iTileMap = iTileMap;
    }

    public void Update(IWidgetLog logger, float seconds)
    {
    }
    
    public void Draw(IWidgetLog logger)
    {
        var drawList = ImGui.GetWindowDrawList();
        var size = new ImVec2(_iTileMap.Width * _iTileMap.ScaleX, _iTileMap.Height * _iTileMap.ScaleY);
        if (ImGui.BeginChild($"map", size, 0, 0))
        {
            var pos = ImGui.GetCursorScreenPos();
            var hx = -1;
            var hy = -1;
            // Grab latest version of map data?
            for (int a = 0; a < _iTileMap.NumLayers; a++)
            {
                var layer = _iTileMap.FetchLayer((uint)a);
                var palette = _iTileMap.FetchPalette((uint)a);
                var bitmaps = palette.Bitmaps;

                var mapData = layer.GetMapData();
                ReadOnlySpan<FlipState> flipStates = ReadOnlySpan<FlipState>.Empty;
                if (layer is ILayerWithFlip layerWithFlip)
                {
                    flipStates = layerWithFlip.GetFlipData();
                }

                var mousePos = ImGui.GetMousePos();
                var localPos = mousePos - pos;
                if (localPos.X >= 0 && localPos.Y >= 0 && localPos.X < size.X && localPos.Y < size.Y)
                {
                    var x = (uint)(localPos.X / (palette.LargestWidth * _iTileMap.ScaleX));
                    var y = (uint)(localPos.Y / (palette.LargestHeight * _iTileMap.ScaleY));

                    if (x >= 0 && x < _iTileMap.Width && y >= 0 && y < _iTileMap.Height)
                    {
                        hx = (int)x;
                        hy = (int)y;
                        if (palette.TilePalette.SelectedTile >= 0 && ImGui.IsMouseDown(ImGuiMouseButton.Left))
                        {
                            try
                            {
                                layer.SetTile(x, y, (uint)palette.TilePalette.SelectedTile);
                            }
                            catch (Exception e)
                            {
                                logger.Log(LogType.Error, $"Failed to set tile at {x},{y} - {e.Message}");
                            }
                        }
                        else if (palette.TilePalette.SelectedTile >= 0 && ImGui.IsMouseDown(ImGuiMouseButton.Right))
                        {
                            var tilenum = mapData[(int)(y * layer.Width + x)];
                            palette.TilePalette.SelectedTile = (int)tilenum;
                        }
                    }
                }

                var uvMinX = new ImVec2(1, 0);
                var uvMaxX = new ImVec2(0, 1);
                var uvMinY = new ImVec2(0, 1);
                var uvMaxY = new ImVec2(1, 0);
                var uvMinXY = new ImVec2(1, 1);
                var uvMaxXY = new ImVec2(0, 0);

                var offY = 0;
                for (uint y = 0; y < layer.Height; y++)
                {
                    var offX = 0;
                    for (uint x = 0; x < layer.Width; x++)
                    {
                        var tilenum = mapData[(int)(y * layer.Width + x)];
                        var tiles = palette.TilePalette.FetchTiles();
                        var tileData = tiles[(int)tilenum];

                        var uvMin = new ImVec2(0, 0);
                        var uvMax = new ImVec2(1, 1);
                        if (flipStates.Length > 0)
                        {
                            var flipState = flipStates[(int)(y * layer.Width + x)];
                            if (flipState.HasFlag(FlipState.X) && flipState.HasFlag(FlipState.Y))
                            {
                                uvMin = uvMinXY;
                                uvMax = uvMaxXY;
                            }
                            else if (flipState.HasFlag(FlipState.X))
                            {
                                uvMin = uvMinX;
                                uvMax = uvMaxX;
                            }
                            else if (flipState.HasFlag(FlipState.Y))
                            {
                                uvMin = uvMinY;
                                uvMax = uvMaxY;
                            }
                        }

                        drawList.AddImage(new ImTextureRef(new ImTextureID(bitmaps[(int)tilenum].Id)), new ImVec2(pos.X + offX, pos.Y + offY), new ImVec2(pos.X + offX + tileData.Width * _iTileMap.ScaleX, pos.Y + offY + tileData.Height * _iTileMap.ScaleY), uvMin, uvMax);
                        offX += (int)(palette.LargestWidth * _iTileMap.ScaleX);
                    }
                    offY += (int)(palette.LargestHeight * _iTileMap.ScaleY);
                }
            }
        }
        ImGui.EndChild();
    }
}
