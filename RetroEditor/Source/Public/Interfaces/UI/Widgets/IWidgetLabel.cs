namespace RetroEditor.Plugins
{
    /// <summary>
    /// Interface for a widget that can display a label
    /// </summary>
    public interface IWidgetLabel : IWidgetItem
    {
        /// <summary>
        /// Name (used to allow changing the label dynamically)
        /// </summary>
        string Name { get; set; }
    }
}