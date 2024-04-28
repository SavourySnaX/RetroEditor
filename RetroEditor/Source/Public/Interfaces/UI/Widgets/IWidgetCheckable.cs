namespace RetroEditor.Plugins
{
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
}