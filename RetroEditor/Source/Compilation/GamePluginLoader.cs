using Microsoft.CodeAnalysis;

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

        var assembly = _plugin.LoadInMemoryPlugin();

        if (assembly == null)
        {
            return retroPlugins;
        }

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
