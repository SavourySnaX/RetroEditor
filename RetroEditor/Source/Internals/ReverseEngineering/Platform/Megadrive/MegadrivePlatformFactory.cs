using RetroEditor.Plugins;

namespace RetroEditor.Source.Internals.ReverseEngineering.Platform.Megadrive;

/// <summary>
/// Megadrive/Genesis platform factory
/// </summary>
internal class MegadrivePlatformFactory : IPlatformFactory
{
    public IDisassembler CreateDisassembler()
    {
        return new Megadrive68000Disassembler();
    }

    public IMemoryMapper CreateMemoryMapper()
    {
        return new MegadriveMemoryMapper();
    }

    public ICpuStateManager CreateCpuStateManager()
    {
        return new Megadrive68000StateManager();
    }

    public ITraceParser CreateTraceParser()
    {
        return new MegadriveTraceParser();
    }

    public IHardwareSymbolsProvider CreateHardwareSymbolsProvider()
    {
        return new MegadriveHardwareRegisterProvider();
    }

    public IMemoryInformationProvider CreateMemoryInformationProvider()
    {
        return new MegadriveMemoryInformationProvider();
    }
}
