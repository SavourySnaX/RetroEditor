using System.Numerics;
using ImGuiNET;
using RetroEditor.Plugins;

internal class ObjectMapWidget : IWidgetItem, IWidgetUpdateDraw
{

    IObjectMap _objectMap;
    int selectedObject = -1;

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

        // Object picker
        var mousePos = ImGui.GetMousePos();
        var localPos = mousePos - pos;

        var palette = _objectMap.FetchPalette();
        var bitmaps = palette.Bitmaps;
        var tiles = palette.TilePalette.FetchTiles();
        int currentObject = 0;

        if (localPos.X >= 0 && localPos.Y >= 0 && localPos.X < size.X && localPos.Y < size.Y)
        {
            var x = (uint)(localPos.X / _objectMap.ScaleX);
            var y = (uint)(localPos.Y / _objectMap.ScaleY);

            if (x >= 0 && x < _objectMap.Width && y >= 0 && y < _objectMap.Height)
            {
                hx = (int)x;
                hy = (int)y;

                int nSelectedObject = -1;
                currentObject = 0;
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    foreach (var obj in _objectMap.FetchObjects)
                    {
                        if (hx >= obj.X && hx < obj.X + obj.Width * palette.LargestWidth && hy >= obj.Y && hy < obj.Y + obj.Height * palette.LargestHeight)
                        {
                            if (currentObject == selectedObject)
                            {
                                selectedObject = -1;
                            }
                            else
                            {
                                nSelectedObject = currentObject;
                            }
                            break;
                        }
                        currentObject++;
                    }
                }
                if (nSelectedObject != -1)
                {
                    selectedObject = nSelectedObject;
                }
            }
        }

        foreach (var obj in _objectMap.FetchObjects)
        {
            var mapData = obj.GetMapData();

            var offY = obj.Y;
            for (uint y = 0; y < obj.Height; y++)
            {
                var offX = obj.X;
                for (uint x = 0; x < obj.Width; x++)
                {
                    var tilenum = mapData[(int)(y * obj.Width + x)];
                    var tileData = tiles[(int)tilenum];

                    drawList.AddImage((nint)bitmaps[(int)tilenum].Id, new Vector2(pos.X + offX, pos.Y + offY), new Vector2(pos.X + offX + tileData.Width * _objectMap.ScaleX, pos.Y + offY + tileData.Height * _objectMap.ScaleY));
                    if (currentObject == selectedObject)
                    {
                        drawList.AddRectFilled(new Vector2(pos.X + offX, pos.Y + offY), new Vector2(pos.X + offX + tileData.Width * _objectMap.ScaleX, pos.Y + offY + tileData.Height * _objectMap.ScaleY), 0x80808080);
                    }
                    offX += (uint)(palette.LargestWidth * _objectMap.ScaleX);
                }
                offY += (uint)(palette.LargestHeight * _objectMap.ScaleY);
            }
            currentObject++;
        }
        ImGui.EndChild();
    }
}

