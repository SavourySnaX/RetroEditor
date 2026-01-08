using MyMGui;
using RetroEditor.Plugins;

internal class BitmapWidget : IWidgetItem, IWidgetUpdateDraw
{
    private IBitmapImage _iBitmap;

    public BitmapWidget(IBitmapImage iBitmap)
    {
        _iBitmap = iBitmap;
    }

    public void Update(IWidgetLog logger, float seconds)
    {
    }

    public void Draw(IWidgetLog logger)
    {
        var palette = _iBitmap.Palette.GetPalette();
        var pixels = _iBitmap.GetImageData(0.0f);
        var pixelWidth = _iBitmap.PixelWidth;
        var pixelHeight = _iBitmap.PixelHeight;

        var drawList = ImGui.GetWindowDrawList();
        var size = new ImVec2(_iBitmap.Width * pixelWidth, _iBitmap.Height * pixelHeight);
        if (ImGui.BeginChild($"pixels", size, 0, 0))
        {
            var hx = -1;
            var hy = -1;
            var pos = ImGui.GetCursorScreenPos();
            {
                var mousePos = ImGui.GetMousePos();
                var localPos = mousePos - pos;
                if (localPos.X >= 0 && localPos.Y >= 0 && localPos.X < size.X && localPos.Y < size.Y)
                {
                    var x = (uint)(localPos.X / pixelWidth);
                    var y = (uint)(localPos.Y / pixelHeight);

                    if (x >= 0 && x < _iBitmap.Width && y >= 0 && y < _iBitmap.Height)
                    {
                        hx= (int)x;
                        hy = (int)y;
                        if (_iBitmap.Palette.SelectedColour >= 0 && ImGui.IsMouseDown(ImGuiMouseButton.Left))
                        {
                            _iBitmap.SetPixel(x, y, (uint)_iBitmap.Palette.SelectedColour);
                        }
                        else if (ImGui.IsMouseDown(ImGuiMouseButton.Right))
                        {
                            _iBitmap.Palette.SelectedColour = (int)pixels[(int)(x + y * _iBitmap.Width)];
                        }
                    }
                }
            }
            uint yOffs = 0;
            for (int y=0;y<_iBitmap.Height;y++)
            {
                uint xOffs = 0;
                for (int x=0;x<_iBitmap.Width;x++)
                {
                    var pixel = pixels[(int)(x + y * _iBitmap.Width)];
                    var colour = palette[(int)pixel];
                    if (hx == x && hy == y)
                    {
                        if (pixelWidth > 2 && pixelHeight > 2)
                        {
                            drawList.AddRectFilled(new ImVec2(pos.X + xOffs + 1, pos.Y + yOffs + 1), new ImVec2(pos.X + xOffs + pixelWidth - 1, pos.Y + yOffs + pixelHeight - 1), ImGuiHelper.MakeColour(colour.Red, colour.Green, colour.Blue, colour.Alpha).InvertColor());
                        }
                        drawList.AddRect(new ImVec2(pos.X + xOffs, pos.Y + yOffs), new ImVec2(pos.X + xOffs + pixelWidth, pos.Y + yOffs + pixelHeight), ImGuiHelper.MakeColour(colour.Red, colour.Green, colour.Blue, colour.Alpha).InvertColor());
                    }
                    else
                    {
                        drawList.AddRectFilled(new ImVec2(pos.X + xOffs, pos.Y + yOffs), new ImVec2(pos.X + xOffs + pixelWidth, pos.Y + yOffs + pixelHeight), ImGuiHelper.MakeColour(colour.Red, colour.Green, colour.Blue, colour.Alpha));
                    }
                    xOffs += pixelWidth;
                }
                yOffs += pixelHeight;
            }
        }
        ImGui.EndChild();
    }


}
