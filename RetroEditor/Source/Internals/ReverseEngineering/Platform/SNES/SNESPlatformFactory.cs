
namespace RetroEditor.Source.Internals.ReverseEngineering.Platform.SNES;

/// <summary>
/// SNES platform factory
/// </summary>
internal class SNESPlatformFactory : IPlatformFactory
{
    public IDisassembler CreateDisassembler()
    {
        return new SNES65816Disassembler();
    }

    public IMemoryMapper CreateMemoryMapper()
    {
        return new SNESMemoryMapper();
    }

    public ICpuStateManager CreateCpuStateManager()
    {
        return new SNES65816StateManager();
    }

    public ITraceParser CreateTraceParser()
    {
        return new SNESTraceParser();
    }

    public IHardwareRegisterProvider CreateHardwareRegisterProvider()
    {
        return new SNESHardwareRegisterProvider();
    }
}