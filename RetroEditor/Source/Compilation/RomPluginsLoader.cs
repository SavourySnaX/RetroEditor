
// For now, rom plugins never reload?
using Microsoft.CodeAnalysis;
using RetroEditor.Logging;
using RetroEditor.Plugins;

internal class RomPluginsLoader
{
    private PluginBuilder _iromPlugin;

    public RomPluginsLoader()
    {
        _iromPlugin = new PluginBuilder("RomPlugins");
        var referenceAssembliesRoot = "ReferenceAssemblies";
        _iromPlugin.AddReferences(referenceAssembliesRoot);
    }

    public List<Type> LoadPlugin()
    {
        var romPlugins = new List<Type>();
        var result = _iromPlugin.BuildPlugin("Plugins/Source/RomPlugins");
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
            if (type.GetInterface(nameof(ISystemPlugin)) != null)
            {
                romPlugins.Add(type);
            }
        }

        return romPlugins;
    }
}