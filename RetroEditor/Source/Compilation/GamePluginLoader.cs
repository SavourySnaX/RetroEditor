public class GamePluginLoader
{
    private PluginBuilder _plugin;
    private string _pathToPlugin;

    public GamePluginLoader(string path)
    {
        var directoryName = new DirectoryInfo(path).Name;
        _plugin = new PluginBuilder(directoryName);
        var imguiReference = Path.Combine(System.AppContext.BaseDirectory, "ImGui.NET.dll"); // Todo make an EditorUI assembly and remove direct imgui access
        var referenceAssembliesRoot = Path.Combine(System.AppContext.BaseDirectory, "ReferenceAssemblies");
        _plugin.AddReference(imguiReference);
        _plugin.AddReferences(referenceAssembliesRoot);
        _plugin.AddGlobalUsing("System");
        _plugin.AddGlobalUsing("System.IO");
        _plugin.AddGlobalUsing("System.Linq");
        _plugin.AddGlobalUsing("System.Collections.Generic");
        _pathToPlugin = path;
    }

    public List<Type> LoadPlugin()
    {
        var retroPlugins = new List<Type>();
        var result = _plugin.BuildPlugin(_pathToPlugin);
        if (!result.Success)
        {
            Console.WriteLine("Compilation failed!");
            foreach (var diagnostic in result.Diagnostics)
            {
                Console.WriteLine(diagnostic);
            }
            return retroPlugins;
        }

        var assembly = _plugin.LoadInMemoryPlugin();

        foreach (var type in assembly.GetTypes())
        {
            if (type.GetInterface("IRetroPlugin") != null)
            {
                retroPlugins.Add(type);
            }
        }

        return retroPlugins;
    }

    public void UnloadPlugin()
    {
        _plugin.Unload();
    }
}
