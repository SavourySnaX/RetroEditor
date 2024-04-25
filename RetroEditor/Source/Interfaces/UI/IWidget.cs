
namespace RetroEditor.Plugins
{

    /// <summary>
    /// Delegate for a changed event. Would be called if a value changes in a widget that supports it
    /// </summary>
    public delegate void ChangedEventHandler();

    /// <summary>
    /// Interface for a widget item that can be added to a window
    /// </summary>
    public interface IWidgetItem
    {
    }

    /// <summary>
    /// Interface for a widget that can be enabled or disabled
    /// </summary>
    public interface IWidgetEnableable : IWidgetItem
    {
        /// <summary>
        /// Is the widget enabled
        /// </summary>
        bool Enabled { get; set; }
    }

    /// <summary>
    /// Interface for a widget that can notify of a value change
    /// </summary>
    public interface IWidgetHandleable : IWidgetEnableable
    {
        /// <summary>
        /// Handler for the changed event
        /// </summary>
        ChangedEventHandler? Handler { get; set; }
    }

    /// <summary>
    /// Interface for a widget that can be checked or unchecked
    /// </summary>
    public interface IWidgetCheckable : IWidgetHandleable
    {
        /// <summary>
        /// Is the widget checked
        /// </summary>
        bool Checked { get; set; }
    }

    /// <summary>
    /// Interface for a widget that has a ranged value
    /// </summary>
    public interface IWidgetRanged : IWidgetHandleable
    {
        /// <summary>
        /// The value of the widget
        /// </summary>
        int Value { get; set; }
    }

    /// <summary>
    /// Interface for creating widgets
    /// </summary>
    public interface IWidget
    {
        /// <summary>
        /// Add a seperator bar to the window
        /// </summary>
        /// <returns>Widget</returns>
        IWidgetItem AddSeperator();
        /// <summary>
        /// Causes the next widget to be placed on the same line as the previous widget
        /// </summary>
        /// <returns>Widget</returns>
        IWidgetItem SameLine();

        /// <summary>
        /// Add a checkbox to the window
        /// </summary>
        /// <param name="label">Label of the checkbox</param>
        /// <param name="initialValue">Initial value of the checkbox</param>
        /// <param name="changed">Delegate called when the value is changed</param>
        /// <returns>Widget</returns>
        IWidgetCheckable AddCheckbox(string label, bool initialValue, ChangedEventHandler changed);
        /// <summary>
        /// Add a slider to the window
        /// </summary>
        /// <param name="label">Lavel of the slider</param>
        /// <param name="initialValue">Initial value of the slider</param>
        /// <param name="min">Minimum value of the slider</param>
        /// <param name="max">Maximum value of the slider</param>
        /// <param name="changed">Delegate called when the value is changed</param>
        /// <returns>Widget</returns>
        IWidgetRanged AddSlider(string label, int initialValue, int min, int max, ChangedEventHandler changed);

        /// <summary>
        /// Adds an image view to the window
        /// </summary>
        /// <param name="image">Object implementing the IImage interface</param>
        /// <returns>Widget</returns>
        IWidgetItem AddImageView(IImage image);
        /// <summary>
        /// Adds a bitmap image editor to the window
        /// </summary>
        /// <param name="image">Object implmenting the IBitmapImage interface</param>
        /// <returns>Widget</returns>
        IWidgetItem AddBitmapWidget(IBitmapImage image);
        /// <summary>
        /// Adds a tile map editor to the window
        /// </summary>
        /// <param name="tileMap">Object implementing the ITileMap interface</param>
        /// <returns>Widget</returns>
        IWidgetItem AddTileMapWidget(ITileMap tileMap);
    }

    internal interface IWidgetUpdateDraw
    {
        void Update(float seconds);
        void Draw();
    }

    /// <summary>
    /// Interface to allow a plugin to add widgets to the player window.
    /// </summary>
    public interface IPlayerWindowExtension
    {
        /// <summary>
        /// Called when the player window is opened, to allow the plugin to define its widgets.
        /// </summary>
        /// <param name="rom">memory interface</param>
        /// <param name="widget">widget interface</param>
        /// <param name="playerControls">player controls interface</param>
        void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls);
    }

    /// <summary>
    /// Interface for a window that can be opened by the user.
    /// </summary>
    public interface IUserWindow
    {
        /// <summary>
        /// Update interval for the window in seconds.
        /// </summary>
        public float UpdateInterval { get; }
        /// <summary>
        /// Called when the window is opened, to allow the window to define its widgets
        /// </summary>
        /// <param name="rom">memory interface</param>
        /// <param name="widget">widget interface</param>
        /// <param name="playerControls">player controls inteface</param>
        void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls);

        /// <summary>
        /// Called when the window is closed, to allow the window to clean up any resources
        /// </summary>
        void OnClose();
    }
}