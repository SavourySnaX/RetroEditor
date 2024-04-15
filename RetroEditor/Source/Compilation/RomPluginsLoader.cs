
// For now, rom plugins never reload?
using Microsoft.CodeAnalysis;

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
            Editor.Log(LogType.Error, "Compilation", "Compilation failed!");
        }
        foreach (var diagnostic in result.Diagnostics)
        {
            switch (diagnostic.Severity)
            {
                case DiagnosticSeverity.Error:
                    Editor.Log(LogType.Error, "Compilation", diagnostic.ToString());
                    break;
                case DiagnosticSeverity.Warning:
                    Editor.Log(LogType.Warning, "Compilation", diagnostic.ToString());
                    break;
                case DiagnosticSeverity.Hidden:
                    Editor.Log(LogType.Debug, "Compilation", diagnostic.ToString());
                    break;
                default:
                    Editor.Log(LogType.Info, "Compilation", diagnostic.ToString());
                    break;
            }
        }

        var assembly = _iromPlugin.LoadInMemoryPlugin();
        if (assembly == null)
        {
            return romPlugins;
        }

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