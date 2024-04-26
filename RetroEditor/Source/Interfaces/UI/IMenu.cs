

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

    /// <summary>
    /// Interface for creating menus
    /// </summary>
    public interface IMenu
    {
        /// <summary>
        /// Add a menu item to the root of the plugins menu. Represents a new submenu.
        /// </summary>
        /// <param name="name">Name of the menu item</param>
        /// <returns>object representing the new item</returns>
        IMenuItem AddItem(string name);
        /// <summary>
        /// Add a menu parented to another menu item. Represents a new submenu.
        /// </summary>
        /// <param name="parent">Parent to attach new item to</param>
        /// <param name="name">Name of the menu item</param>
        /// <returns>object representing the new item</returns>
        IMenuItem AddItem(IMenuItem parent, string name);
        /// <summary>
        /// Add an interactable menu item to the root of the plugins menu.
        /// </summary>
        /// <param name="name">Name of the menu item</param>
        /// <param name="handler">Handler for the menu item</param>
        /// <returns>object representing the new item</returns>
        IMenuItem AddItem(string name, MenuEventHandler handler);
        /// <summary>
        /// Add an interactable menu item to the parent menu item
        /// </summary>
        /// <param name="parent">Parent to attach new item to</param>
        /// <param name="name">Name of the menu item</param>
        /// <param name="handler">Handler for the menu item</param>
        /// <returns>object representing the new item</returns>
        IMenuItem AddItem(IMenuItem parent, string name, MenuEventHandler handler);
    }

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