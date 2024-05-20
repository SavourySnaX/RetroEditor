
// For now, rom plugins never reload?
using Microsoft.CodeAnalysis;
using RetroEditor.Plugins;

internal class RomPluginsLoader
{
    private PluginBuilder _iromPlugin;
    private IEditorInternal _editor;

    public RomPluginsLoader(IEditorInternal editor)
    {
        _editor = editor;
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
            _editor.Log(LogType.Error, "Compilation", "Compilation failed!");
        }
        foreach (var diagnostic in result.Diagnostics)
        {
            switch (diagnostic.Severity)
            {
                case DiagnosticSeverity.Error:
                    _editor.Log(LogType.Error, "Compilation", diagnostic.ToString());
                    break;
                case DiagnosticSeverity.Warning:
                    _editor.Log(LogType.Warning, "Compilation", diagnostic.ToString());
                    break;
                case DiagnosticSeverity.Hidden:
                    _editor.Log(LogType.Debug, "Compilation", diagnostic.ToString());
                    break;
                default:
                    _editor.Log(LogType.Info, "Compilation", diagnostic.ToString());
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