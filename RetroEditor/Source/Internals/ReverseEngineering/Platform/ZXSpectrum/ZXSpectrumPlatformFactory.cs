using RetroEditor.Plugins;

namespace RetroEditor.Source.Internals.ReverseEngineering.Platform.ZXSpectrum;

/// <summary>
/// ZX Spectrum platform factory
/// </summary>
internal class ZXSpectrumPlatformFactory : IPlatformFactory
{
    public IDisassembler CreateDisassembler()
    {
        return new Z80Disassembler();
    }

    public IMemoryMapper CreateMemoryMapper()
    {
        return new ZXSpectrumMemoryMapper();
    }

    public ICpuStateManager CreateCpuStateManager()
    {
        return new ZXSpectrumZ80StateManager();
    }

    public ITraceParser CreateTraceParser()
    {
        return new ZXSpectrumTraceParser();
    }

    public IHardwareSymbolsProvider CreateHardwareSymbolsProvider()
    {
        return new ZXSpectrumHardwareRegisterProvider();
    }

    public IMemoryInformationProvider CreateMemoryInformationProvider()
    {
        return new ZXSpectrumMemoryInformationProvider();
    }
}
