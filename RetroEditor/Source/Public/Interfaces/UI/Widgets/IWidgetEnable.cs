namespace RetroEditor.Plugins
{
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
}