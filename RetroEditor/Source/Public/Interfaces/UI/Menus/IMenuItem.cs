namespace RetroEditor.Plugins
{
    /// <summary>
    /// Callback for menu events
    /// </summary>
    /// <param name="editorInterface">editor interface</param>
    /// <param name="menu">menu item that was picked</param>
    public delegate void MenuEventHandler(IEditor editorInterface, IMenuItem menu);

    /// <summary>
    /// Represents a menu item
    /// </summary>
    public interface IMenuItem
    {
        /// <summary>
        /// Enable or disable the menu item
        /// </summary>
        bool Enabled { get; set; }
        /// <summary>
        /// Name of the menu item to display
        /// </summary>
        string Name { get; set; }
        /// <summary>
        /// Handler event for the menu item
        /// </summary>
        MenuEventHandler? Handler { get; set; }
    }
}