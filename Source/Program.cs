
Console.OutputEncoding = System.Text.Encoding.UTF8;


// Testing

var iRomPlugin = new RomPluginsLoader();
var romPlugins = iRomPlugin.LoadPlugin();

var editorPlugins = new List<GamePluginLoader>();

foreach (var dir in Directory.GetDirectories("Plugins/Source/GamePlugins"))
{
    var pluginLoader = new GamePluginLoader(dir);
    editorPlugins.Add(pluginLoader);
}

var render = new Editor(editorPlugins.ToArray(), romPlugins.ToArray());

render.RenderRun();
