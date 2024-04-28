namespace RetroEditor.Plugins
{
    /// <summary>
    /// Interface for providing menus
    /// Allows a plugin to provide a set of menus to the editor
    /// </summary>
    public interface IMenuProvider
    {
        /// <summary>
        /// Configure the menu for the editor
        /// </summary>
        /// <param name="rom">Memory access interface</param>
        /// <param name="menu">Menu interface</param>
        void ConfigureMenu(IMemoryAccess rom, IMenu menu);
    }
}