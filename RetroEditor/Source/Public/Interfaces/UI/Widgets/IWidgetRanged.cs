namespace RetroEditor.Plugins
{
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
}