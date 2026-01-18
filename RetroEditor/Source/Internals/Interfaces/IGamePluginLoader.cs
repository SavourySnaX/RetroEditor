internal interface IGamePluginLoader
{
    public void AddAssembly(string assemblyPath);
    public List<Type> LoadPlugin();
    public void UnloadPlugin();
}
