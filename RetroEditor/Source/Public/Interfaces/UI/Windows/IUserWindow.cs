namespace RetroEditor.Plugins
{
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