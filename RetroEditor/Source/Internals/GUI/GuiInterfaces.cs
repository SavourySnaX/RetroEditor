
/// <summary>
/// Interface for a window that can be displayed in the editor.
/// </summary>
public interface IWindow
{
    /// <summary>
    /// Initialise the window.
    /// </summary>
    /// <returns>false if there was a problem in initialisation, otherwise true.</returns>
    bool Initialise();
    /// <summary>
    /// Update the window.
    /// </summary>
    /// <param name="seconds"></param>
    void Update(float seconds);

    /// <summary>
    /// The update interval for the window. In seconds, 1.0f/30.0f would be 30fps.
    /// </summary>
    float UpdateInterval { get; }

    /// <summary>
    /// Draw the window
    /// </summary>
    /// <returns>true if the window should close</returns>
    bool Draw();

    /// <summary>
    /// Called when the window is closed, to allow for cleanup.
    /// </summary>
    void Close();
}