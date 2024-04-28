namespace RetroEditor.Plugins
{
    /// <summary>
    /// Delegate for a changed event. Would be called if a value changes in a widget that supports it
    /// </summary>
    public delegate void ChangedEventHandler();

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
}