using RetroEditor.Plugins;
using Xunit.Abstractions;

namespace RetroEditor.Integration.Tests;

public class Plugins
{
    private readonly ITestOutputHelper _testOutputHelper;
    public Plugins(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public static IEnumerable<object[]> GetRomPluginNames()
    {
        var type = typeof(ISystemPlugin);
        var assemblies = AppDomain.CurrentDomain.Load("Plugins");
        var types = assemblies.GetTypes();
        var pluginTypes = types .Where(p => type.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);
        foreach (var t in pluginTypes)
        {
            var instance = (ISystemPlugin)Activator.CreateInstance(t)!;
            yield return new object[] { instance.LibRetroPluginName };
        }
    }

    [Theory]
    [MemberData(nameof(GetRomPluginNames))]
    public void EnsurePluginsDownload(string pluginName)
    {
        var editor = new Editor(new Editor.EditorSettings(), new Helpers.TestLogger(_testOutputHelper));
        using var instance = editor.GetLibRetroInstance(pluginName, null);
        Assert.NotNull(instance);
    }
}