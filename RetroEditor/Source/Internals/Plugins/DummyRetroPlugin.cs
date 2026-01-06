
using RetroEditor.Plugins;

internal class DummyRetroPlugin : IRetroPlugin
{
    public static string Name => throw new NotImplementedException();

    public string RomPluginName => throw new NotImplementedException();

    public bool RequiresAutoLoad => throw new NotImplementedException();

    public bool AutoLoadCondition(IMemoryAccess romAccess)
    {
        throw new NotImplementedException();
    }

    public bool CanHandle(string path)
    {
        throw new NotImplementedException();
    }

    public ISave Export(IMemoryAccess romAcess)
    {
        throw new NotImplementedException();
    }

    public void SetupGameTemporaryPatches(IMemoryAccess romAccess)
    {
        throw new NotImplementedException();
    }
}