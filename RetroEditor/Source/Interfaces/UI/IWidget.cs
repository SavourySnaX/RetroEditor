
public delegate void ChangedEventHandler();

public interface IWidgetItem
{
}

public interface IWidgetEnableable : IWidgetItem
{
    bool Enabled { get; set; }
}

public interface IWidgetHandleable : IWidgetEnableable
{
    ChangedEventHandler? Handler { get; set; }
}

public interface IWidgetCheckable : IWidgetHandleable
{
    bool Checked { get; set; }
}

public interface IWidgetRanged : IWidgetHandleable
{
    int Value { get; set; }
}

public interface IWidget
{
    IWidgetItem AddSeperator();
    IWidgetItem SameLine();

    IWidgetCheckable AddCheckbox(string label, bool initialValue, ChangedEventHandler changed);
    IWidgetRanged AddSlider(string label, int initialValue, int min, int max, ChangedEventHandler changed);

    IWidgetItem AddImageView(IImage image);
}

internal interface IWidgetUpdateDraw
{
    void Update(float seconds);
    void Draw();
}

public interface IPlayerWindowExtension
{
    void ConfigureWidgets(IRomAccess rom, IWidget widget, IPlayerControls playerControls);
}


public interface IUserWindow
{
    public float UpdateInterval { get; }
    void ConfigureWidgets(IRomAccess rom, IWidget widget, IPlayerControls playerControls);
    void OnClose();
}