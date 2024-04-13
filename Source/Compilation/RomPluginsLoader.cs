
// For now, rom plugins never reload?
public class RomPluginsLoader
{
    private PluginBuilder _iromPlugin;

    public RomPluginsLoader()
    {
        _iromPlugin = new PluginBuilder("RomPlugins");
        var editorReference = Path.Combine(System.AppContext.BaseDirectory, "RetroEditor.dll"); // Todo make a reference assembly of the important bits and remove this
        var referenceAssembliesRoot = Path.Combine(System.AppContext.BaseDirectory, "ReferenceAssemblies");
        _iromPlugin.AddReference(editorReference);
        _iromPlugin.AddReferences(referenceAssembliesRoot);
        _iromPlugin.AddGlobalUsing("System");
    }

    public List<Type> LoadPlugin()
    {
        var result = _iromPlugin.BuildPlugin("Plugins/RomPlugins");
        if (!result.Success)
        {
            Console.WriteLine("Compilation failed!");
            foreach (var diagnostic in result.Diagnostics)
            {
                Console.WriteLine(diagnostic);
            }
            return null;
        }

        var assembly = _iromPlugin.LoadInMemoryPlugin();

        var romPlugins = new List<Type>();
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