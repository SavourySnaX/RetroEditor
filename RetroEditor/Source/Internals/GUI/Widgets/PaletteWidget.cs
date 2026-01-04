
using System.Numerics;
using ImGuiNET;
using RetroEditor.Plugins;
using RetroEditor.Source.Internals.GUI;

internal class PaletteWidget : IWidgetItem, IWidgetUpdateDraw
{
    private IBitmapPalette _iPalette;

    public PaletteWidget(IBitmapPalette iBitmap)
    {
        this._iPalette = iBitmap;
        _iPalette.SelectedColour = -1;
    }

    public void Update(IWidgetLog logger, float seconds)
    {
    }

    public void Draw(IWidgetLog logger)
    {
        var palette = _iPalette.GetPalette();
        var width = _iPalette.Width;
        var height = _iPalette.Height;
        var coloursPerRow = _iPalette.ColoursPerRow;

        // Compute the size of the palette
        var availColours = palette.Length;
        var rows = (availColours + coloursPerRow - 1) / coloursPerRow;

        var size = new Vector2(width * coloursPerRow, height * rows);
        var remainder = availColours == coloursPerRow ? availColours : availColours % coloursPerRow;
        AbiSafe_ImGuiWrapper.BeginChild($"palette", size, 0, 0);

        var drawList = ImGui.GetWindowDrawList();
        Vector2 pos = ImGui.GetCursorScreenPos();

        int a = 0;
        uint yOffs = 0;
        for (int y = 0; y < rows; y++)
        {
            uint xOffs = 0;
            var numColours = (y == rows - 1) ? remainder : coloursPerRow;
            for (int x = 0; x < numColours; x++)
            {
                var colour = palette[a];
                var mousePos = ImGui.GetMousePos();
                var localPos = mousePos - pos;
                if (localPos.X >= xOffs && localPos.Y >= yOffs && localPos.X < xOffs + width && localPos.Y < yOffs + height && ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    _iPalette.SelectedColour = a;
                }
                AbiSafe_ImGuiWrapper.DrawList_AddRectFilled(drawList, new Vector2(pos.X + xOffs + 4, pos.Y + yOffs + 4), new Vector2(pos.X + xOffs + width - 4, pos.Y + yOffs + height - 4), ImGuiHelper.MakeColour(colour.Red, colour.Green, colour.Blue, colour.Alpha));
                if (_iPalette.SelectedColour == a)
                {
                    AbiSafe_ImGuiWrapper.DrawList_AddRect(drawList, new Vector2(pos.X + xOffs, pos.Y + yOffs), new Vector2(pos.X + xOffs + width, pos.Y + yOffs + height), ImGuiHelper.MakeColour(255, 255, 255, 255));
                }
                a++;
                xOffs += width;
            }
            yOffs += height;
        }

        ImGui.EndChild();
    }
}

