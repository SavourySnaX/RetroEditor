namespace RetroEditor.Plugins
{
    /// <summary>
    /// Interface for saving data, intended to be used to generate the final modded game
    /// At present only implemented by ZXSpectrumTape
    /// </summary>
    public interface ISave
    {
        /// <summary>
        /// Save the data to a file
        /// </summary>
        /// <param name="path">path to save file</param>
        public void Save(string path);
    }
}