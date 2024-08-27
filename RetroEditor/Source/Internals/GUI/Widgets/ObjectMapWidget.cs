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

    bool dragging = false;

    public void Interaction(IWidgetLog logger, Vector2 size, Vector2 pos)
    {
        // Object picker
        var mousePos = ImGui.GetMousePos();
        var localPos = mousePos - pos;

        var palette = _objectMap.FetchPalette();
        int currentObject = 0;

        ImGui.SetMouseCursor(ImGuiMouseCursor.Arrow);
        if (localPos.X >= 0 && localPos.Y >= 0 && localPos.X < size.X && localPos.Y < size.Y)
        {
            var x = (uint)(localPos.X / _objectMap.ScaleX);
            var y = (uint)(localPos.Y / _objectMap.ScaleY);

            if (selectedObject!=-1 && ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                if (!dragging)
                {
                    if (x >= 0 && x < _objectMap.Width && y >= 0 && y < _objectMap.Height)
                    {
                        var obj = _objectMap.FetchObjects.ElementAt(selectedObject);
                        if (x >= obj.X && x < obj.X + obj.Width * palette.LargestWidth && y >= obj.Y && y < obj.Y + obj.Height * palette.LargestHeight)
                        {
                            dragging = true;
                        }
                    }
                }
                else
                {
                    _objectMap.ObjectMove(_objectMap.FetchObjects.ElementAt(selectedObject), x, y);
                }
            }
            else
            {
                dragging = false;
            }

            if (x >= 0 && x < _objectMap.Width && y >= 0 && y < _objectMap.Height)
            {
                int nSelectedObject = -1;
                currentObject = 0;

                foreach (var obj in _objectMap.FetchObjects)
                {
                    if (x >= obj.X && x < obj.X + obj.Width * palette.LargestWidth && y >= obj.Y && y < obj.Y + obj.Height * palette.LargestHeight)
                    {
                        ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);

                        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        {
                            nSelectedObject = currentObject;
                            break;
                        }
                    }
                    currentObject++;
                }
                if (nSelectedObject != -1)
                {
                    selectedObject = nSelectedObject;
                }
                else
                {
                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        selectedObject = -1;
                    }
                }
            }
        }
    }

    public void Draw(IWidgetLog logger)
    {
        var drawList = ImGui.GetWindowDrawList();
        var size = new Vector2(_objectMap.Width * _objectMap.ScaleX, _objectMap.Height * _objectMap.ScaleY);
        ImGui.BeginChild($"map", size, 0, 0);

        var pos = ImGui.GetCursorScreenPos();
        Interaction(logger, size, pos);
        var palette = _objectMap.FetchPalette();
        var bitmaps = palette.Bitmaps;
        var tiles = palette.TilePalette.FetchTiles();
        var currentObject = 0;
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
                        drawList.AddRectFilled(new Vector2(pos.X + offX, pos.Y + offY), new Vector2(pos.X + offX + tileData.Width * _objectMap.ScaleX, pos.Y + offY + tileData.Height * _objectMap.ScaleY), 0x80000000);
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

