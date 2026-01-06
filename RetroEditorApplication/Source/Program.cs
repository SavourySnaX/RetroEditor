
Console.OutputEncoding = System.Text.Encoding.UTF8;


// Testing
var editor = new Editor();

var iRomPlugin = new RomPluginsLoader(editor);
var romPlugins = iRomPlugin.LoadPlugin();

var editorPlugins = new List<GamePluginLoader>();

foreach (var dir in Directory.GetDirectories("Plugins/Source/GamePlugins"))
{
    var pluginLoader = new GamePluginLoader(editor, dir);
    editorPlugins.Add(pluginLoader);
}


editor.InitialisePlugins(editorPlugins.ToArray(), romPlugins.ToArray());
editor.RenderRun();
