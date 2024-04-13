
// For now, rom plugins never reload?
public class RomPluginsLoader
{
    private PluginBuilder _iromPlugin;

    public RomPluginsLoader()
    {
        _iromPlugin = new PluginBuilder("RomPlugins");
        var referenceAssembliesRoot = Path.Combine(System.AppContext.BaseDirectory, "ReferenceAssemblies");
        _iromPlugin.AddReferences(referenceAssembliesRoot);
        _iromPlugin.AddGlobalUsing("System");
    }

    public List<Type> LoadPlugin()
    {
        var romPlugins = new List<Type>();
        var result = _iromPlugin.BuildPlugin("Plugins/RomPlugins");
        if (!result.Success)
        {
            Console.WriteLine("Compilation failed!");
            foreach (var diagnostic in result.Diagnostics)
            {
                Console.WriteLine(diagnostic);
            }
            return romPlugins;
        }

        var assembly = _iromPlugin.LoadInMemoryPlugin();

        foreach (var type in assembly.GetTypes())
        {
            if (type.GetInterface("IRomPlugin") != null)
            {
                romPlugins.Add(type);
            }
        }

        return romPlugins;
    }
}