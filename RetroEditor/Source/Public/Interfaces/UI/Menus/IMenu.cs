

namespace RetroEditor.Plugins
{
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

}