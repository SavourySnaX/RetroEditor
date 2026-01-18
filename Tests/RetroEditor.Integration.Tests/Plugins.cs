using Microsoft.CodeAnalysis.CSharp.Syntax;
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

    public static IEnumerable<object[]> GetRomPluginTypes()
    {
        var type = typeof(ISystemPlugin);
        var assemblies = AppDomain.CurrentDomain.Load("Plugins");
        var types = assemblies.GetTypes();
        var pluginTypes = types .Where(p => type.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);
        foreach (var t in pluginTypes)
        {
            yield return new object[] { t };
        }
    }

    public static IEnumerable<object[]> GetRomPluginNames()
    {
        var pluginTypes = GetRomPluginTypes();
        foreach (var t in pluginTypes)
        {
            var instance = (ISystemPlugin)Activator.CreateInstance((Type)t[0])!;
            yield return new object[] { instance.LibRetroPluginName };
        }
    }

    [Theory]
    [MemberData(nameof(GetRomPluginNames))]
    public void EnsurePluginsDownload(string pluginName)
    {
        var settings = new Editor.EditorSettings();
        settings.RetroCoreFolder = Path.Combine(Directory.GetCurrentDirectory(), "Core", Path.GetRandomFileName());
        try
        {
            var editor = new Editor(settings, new Helpers.TestLogger(_testOutputHelper));
            using var instance = editor.GetLibRetroInstance(pluginName, null, out _);
            Assert.NotNull(instance);
        }
        finally
        {
            Directory.Delete(settings.RetroCoreFolder, true);
        }
    }

    // A fake plugin to verify project creation works for all rom plugins
    internal struct FakeGamePlugin<T> : IRetroPlugin where T : ISystemPlugin
    {
        public static string Name => "FakePlugin";

        public string RomPluginName => T.Name;

        public bool RequiresAutoLoad => false;

        public bool AutoLoadCondition(IMemoryAccess romAccess)
        {
            throw new NotImplementedException();
        }

        public bool CanHandle(string path)
        {
            return true;
        }

        public ISave Export(IMemoryAccess romAcess)
        {
            throw new NotImplementedException();
        }

        public void SetupGameTemporaryPatches(IMemoryAccess romAccess)
        {
        }
    }

    internal struct FakeGamePluginLoader : IGamePluginLoader
    {
        private IEditorInternal _editor;
        Type _pluginKind;
        public FakeGamePluginLoader(IEditorInternal editor, Type pluginKind)
        {
            _editor = editor;
            _pluginKind = pluginKind;
        }

        public void AddAssembly(string assemblyPath)
        {
            // No-op
        }

        public List<Type> LoadPlugin()
        {
            return new List<Type> { _pluginKind };
        }

        public void UnloadPlugin()
        {
            throw new NotImplementedException();
        }
    }

    [Theory]
    [MemberData(nameof(GetRomPluginTypes))]
    public void CheckProjectInitialisesCorrectly(Type pluginKind)
    {
        var settings = new Editor.EditorSettings();
        settings.RetroCoreFolder = Path.Combine(Directory.GetCurrentDirectory(), "Core", Path.GetRandomFileName());
        settings.ProjectLocation = Path.Combine(Directory.GetCurrentDirectory(), "Projects", Path.GetRandomFileName());
        try
        {
            var pluginName = "FakePlugin";
            Assert.NotNull(pluginName);
            var tmpFile = System.IO.Path.GetTempFileName();
            File.WriteAllBytes(tmpFile, new byte[] { 0x00, 0x01, 0x02, 0x03 }); // Dummy file (shouldn't matter for this test)
            var editor = new Editor(settings, new Helpers.TestLogger(_testOutputHelper));
            var fakePluginLoader = new FakeGamePluginLoader(editor, typeof(FakeGamePlugin<>).MakeGenericType(pluginKind));
            editor.InitialisePlugins([fakePluginLoader], [pluginKind]);
            var projectName = $"TestProject_{pluginName}_{pluginKind.Name}";
            var playable = editor.CreateNewProject(projectName, editor.Settings.ProjectLocation, tmpFile, pluginName, out _);
            Assert.NotNull(playable);
            playable.Close();
            playable.systemPlugin.Dispose();
            File.Delete(tmpFile);
        }
        finally
        {
            Directory.Delete(settings.RetroCoreFolder, true);
            Directory.Delete(settings.ProjectLocation, true);
        }
    }
}