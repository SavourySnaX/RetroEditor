internal interface IGamePluginLoader
{
    public List<Type> LoadPlugin();
    public void UnloadPlugin();
}
