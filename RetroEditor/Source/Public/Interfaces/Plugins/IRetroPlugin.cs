namespace RetroEditor.Plugins
{
    /// <summary>
    /// The primary interface to implement when developing a new plugin
    /// </summary>
    public interface IRetroPlugin
    {

        /// <summary>
        /// Name of the plugin
        /// </summary>
        static abstract string Name { get; }

        /// <summary>
        /// Name of the rom plugin required for this game
        /// </summary>
        string RomPluginName { get; }

        /// <summary>
        /// Does this game require loading to be skipped
        /// For instance games that are loaded from tape, should return true here
        /// and then implement the AutoLoadCondition method to determine when the loading is complete
        /// </summary>
        bool RequiresAutoLoad { get; }

        /// <summary>
        /// This is called when a game is opened, to determine which plugin can handle it
        /// </summary>
        /// <param name="path">Path of file to check</param>
        /// <returns></returns>
        /// <remarks>
        /// At present this cannot be used to determine the specific copy of the game that was loaded, as the plugin is recreated after this call
        /// </remarks>
        bool CanHandle(string path);

        /// <summary>
        /// If auto load is required, this method should determine when the loading is complete
        /// </summary>
        /// <param name="romAccess">memory interface</param>
        /// <returns>true if condition met, else false</returns>
        bool AutoLoadCondition(IMemoryAccess romAccess);

        /// <summary>
        /// This is called to allow initial patches to be applied to the game being editing,
        /// for instance to allow cheats to be applied, or to skip loading screens
        /// </summary>
        /// <param name="romAccess">memory interface</param>
        void SetupGameTemporaryPatches(IMemoryAccess romAccess);

        /// <summary>
        /// This is called when an export is required, it should return a save object that can be used to generate the final modded game
        /// </summary>
        /// <param name="romAcess">memory interface</param>
        /// <returns>Saveable object</returns>
        ISave Export(IMemoryAccess romAcess);
    }
}