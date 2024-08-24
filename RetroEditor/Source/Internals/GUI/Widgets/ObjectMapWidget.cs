using System.Numerics;
using ImGuiNET;
using RetroEditor.Plugins;

internal class ObjectMapWidget : IWidgetItem, IWidgetUpdateDraw
{

    IObjectMap _objectMap;
    public ObjectMapWidget(IObjectMap objectMap)
    {
        _objectMap = objectMap;
    }

    public void Update(IWidgetLog logger, float seconds)
    {
        _objectMap.FetchPalette().Update(seconds);
    }
    
    public void Draw(IWidgetLog logger)
    {
        var drawList = ImGui.GetWindowDrawList();
        var size = new Vector2(_objectMap.Width * _objectMap.ScaleX, _objectMap.Height * _objectMap.ScaleY);
        ImGui.BeginChild($"map", size, 0, 0);
        var pos = ImGui.GetCursorScreenPos();
        var hx = -1;
        var hy = -1;

        var palette = _objectMap.FetchPalette();
        var bitmaps = palette.Bitmaps;
        var tiles = palette.TilePalette.FetchTiles();
        foreach (var obj in _objectMap.FetchObjects)
        {
            var mapData = obj.GetMapData();

/*
            TODO Object picker, TODO object tools mode thing


            var mousePos = ImGui.GetMousePos();
            var localPos = mousePos - pos;

            if (localPos.X >= 0 && localPos.Y >= 0 && localPos.X < size.X && localPos.Y < size.Y)
            {
                var x = (uint)(localPos.X / (palette.LargestWidth * _objectMap.ScaleX));
                var y = (uint)(localPos.Y / (palette.LargestHeight * _objectMap.ScaleY));

                if (x >= 0 && x < _objectMap.Width && y >= 0 && y < _objectMap.Height)
                {
                    hx= (int)x;
                    hy = (int)y;
                    if (palette.TilePalette.SelectedTile >= 0 && ImGui.IsMouseDown(ImGuiMouseButton.Left))
                    {
                        try
                        {
                            // TODO layer.SetTile(x, y, (uint)palette.TilePalette.SelectedTile);
                        }
                        catch (Exception e)
                        {
                            logger.Log(LogType.Error, $"Failed to set tile at {x},{y} - {e.Message}");
                        }
                    }
                    else if (palette.TilePalette.SelectedTile >= 0 && ImGui.IsMouseDown(ImGuiMouseButton.Right))
                    {
//                        var tilenum = mapData[(int)(y * layer.Width + x)];
//                        palette.TilePalette.SelectedTile = (int)tilenum;
                    }
                }
            }*/
            var offY = obj.Y;
            for (uint y = 0; y < obj.Height; y++)
            {
                var offX = obj.X;
                for (uint x = 0; x < obj.Width; x++)
                {
                    var tilenum = mapData[(int)(y * obj.Width + x)];
                    var tileData = tiles[(int)tilenum];

                    drawList.AddImage((nint)bitmaps[(int)tilenum].Id, new Vector2(pos.X + offX, pos.Y + offY), new Vector2(pos.X + offX + tileData.Width * _objectMap.ScaleX, pos.Y + offY + tileData.Height * _objectMap.ScaleY));
                    offX += (uint)(palette.LargestWidth * _objectMap.ScaleX);
                }
                offY += (uint)(palette.LargestHeight * _objectMap.ScaleY);
            }
        }
        ImGui.EndChild();
    }
}

