namespace RetroEditor.Plugins
{
    /// <summary>
    /// Interface for libretro emulator player
    /// </summary>
    public interface IPlayerControls
    {
        /// <summary>
        /// Reset the emulator
        /// </summary>
        void Reset();
    }
}