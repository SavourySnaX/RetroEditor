namespace RetroEditor.Plugins
{
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
}