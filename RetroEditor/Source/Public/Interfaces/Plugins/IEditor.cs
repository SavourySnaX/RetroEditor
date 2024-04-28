namespace RetroEditor.Plugins
{
    /// <summary>
    /// The type of the Log Message, used to categorize the message.
    /// </summary>
    public enum LogType
    {
        /// <summary>
        /// Debug messages are not shown in release builds, and are used for debugging purposes.
        /// </summary>
        Debug,
        /// <summary>
        /// Info messages are used to inform the user of something, but are not considered important.
        /// </summary>
        Info,
        /// <summary>
        /// Warning messages are used to inform the user of something that might be important.
        /// </summary>
        Warning,
        /// <summary>
        /// Error messages are used to inform the user of something that is important.
        /// </summary>
        Error
    }

    /// <summary>
    /// Editor interface - used to interact with the editor
    /// </summary>
    public interface IEditor
    {
        /// <summary>
        /// Create a new window in the editor
        /// </summary>
        /// <param name="name">Name of the window</param>
        /// <param name="window">Objet implementing IUserWindow</param>
        public void OpenUserWindow(string name, IUserWindow window);

        /// <summary>
        /// Write a message to the editor log
        /// </summary>
        /// <remarks>
        /// The message will appear in a section specific to the plugin
        /// </remarks>
        /// <param name="type">LogType severity of message</param>
        /// <param name="message">message</param>
        public void Log(LogType type, string message);
    }
}