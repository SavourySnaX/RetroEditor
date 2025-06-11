
namespace RetroEditor.Plugins
{
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
        /// Add a label to the window
        /// </summary>
        /// <param name="label">Label text</param>
        /// <returns>Widget</returns>
        IWidgetLabel AddLabel(string label);

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
        /// Adds a palette widget to the window
        /// </summary>
        /// <param name="palette">Object implementing the IBitmapPalette interface</param>
        /// <returns>Widget</returns>
        IWidgetItem AddPaletteWidget(IBitmapPalette palette);

        /// <summary>
        /// Adds a bitmap image editor to the window
        /// </summary>
        /// <param name="image">Object implmenting the IBitmapImage interface</param>
        /// <returns>Widget</returns>
        IWidgetItem AddBitmapWidget(IBitmapImage image);

        /// <summary>
        /// Adds a tile palette editor to the window
        /// </summary>
        /// <param name="tilePalette">TilePaletteStore object initialized with an ITilePalette object</param>
        /// <returns>Widget</returns>
        IWidgetItem AddTilePaletteWidget(TilePaletteStore tilePalette);

        /// <summary>
        /// Adds a tile map editor to the window
        /// </summary>
        /// <param name="tileMap">Object implementing the ITileMap interface</param>
        /// <returns>Widget</returns>
        IWidgetItem AddTileMapWidget(ITileMap tileMap);

        /// <summary>
        /// Adds an object map editor to the window
        /// </summary>
        /// <param name="objectMap">Object implemented IObjectMap interface</param>
        /// <returns>Widget</returns>
        IWidgetItem AddObjectMapWidget(IObjectMap objectMap);

        /// <summary>
        /// Adds a render widget to the window
        /// </summary>
        /// <param name="renderWidget">Object implementing the IRender3DWidget interface</param>
        /// <returns>Widget</returns>
        IWidgetItem AddRenderWidget(IRender3DWidget renderWidget);

    }
}