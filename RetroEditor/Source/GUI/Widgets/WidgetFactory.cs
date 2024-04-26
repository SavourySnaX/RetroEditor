

using RetroEditor.Plugins;

internal class WidgetFactory : IWidget
{
    internal WidgetFactory()
    {
        widgets = new List<IWidgetUpdateDraw>();
    }

    public IWidgetCheckable AddCheckbox(string label, bool value, ChangedEventHandler changed)
    {
        var t = new Checkbox(label, value, changed);
        widgets.Add(t);
        return t;
    }

    public IWidgetItem AddSeperator()
    {
        var t = new Seperator();
        widgets.Add(t);
        return t;
    }

    public IWidgetRanged AddSlider(string label, int value, int min, int max, ChangedEventHandler changed)
    {
        var t = new Slider(label, value, min, max, changed);
        widgets.Add(t);
        return t;
    }

    public IWidgetItem SameLine()
    {
        var t = new SameLine();
        widgets.Add(t);
        return t;
    }

    public IWidgetItem AddImageView(IImage image)
    {
        var t = new ImageView(image);
        widgets.Add(t);
        return t;
    }

    public IWidgetItem AddBitmapWidget(IBitmapImage image)
    {
        var t = new BitmapWidget(image);
        widgets.Add(t);
        return t;
    }

    public IWidgetItem AddTileMapWidget(ITileMap tileMap)
    {
        var t = new TileMapWidget(tileMap);
        widgets.Add(t);
        return t;
    }

    private List<IWidgetUpdateDraw> widgets;

    public IEnumerable<IWidgetUpdateDraw> Widgets => widgets;
}