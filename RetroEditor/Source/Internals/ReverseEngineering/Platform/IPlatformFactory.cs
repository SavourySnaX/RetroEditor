using MyMGui;

namespace RetroEditor.Source.Internals.ReverseEngineering.Platform;

/// <summary>
/// Factory interface for creating platform-specific components
/// </summary>
internal interface IPlatformFactory
{
    IDisassembler CreateDisassembler();
    IMemoryMapper CreateMemoryMapper();
    ICpuStateManager CreateCpuStateManager();
    ITraceParser CreateTraceParser();
    IHardwareSymbolsProvider CreateHardwareSymbolsProvider();
    IMemoryInformationProvider CreateMemoryInformationProvider();
}

/// <summary>
/// Memory region types that can be mapped
/// </summary>
internal enum MemoryRegion
{
    ROM,
    RAM,
    SRAM,
    IO,
    Invalid
}

/// <summary>
/// Interface for mapping between CPU addresses and ROM addresses
/// </summary>
internal interface IMemoryMapper
{
    UInt64 MapCpuToRegion(UInt64 cpuAddress, out MemoryRegion region);
    UInt64 MapRomToCpu(UInt64 romAddress);
    UInt64 MapHardwareAddressToCpu(UInt64 linearAddress);
    UInt64 MapCpuToHardwareAddress(UInt64 address, out MemoryRegion region);
}

/// <summary>
/// Interface for managing CPU state from debugger information
/// </summary>
internal interface ICpuStateManager
{
    void UpdateStateFromInstruction(ICpuState state, Instruction instruction);
    string GetTraceFormat();
    void RenderUI();
    bool InstructionTerminatesAutoDisassembly(Instruction instruction);
    ICpuState FetchStateFromUI();
    void UpdateUIFromState(ICpuState state);
}

/// <summary>
/// Parsed trace entry from execution trace
/// </summary>
internal struct TraceEntry
{
    public UInt64 Address;
    public ICpuState CpuState;
    public ICpuRegisterState RegisterState;
    public bool IsValid;
}

/// <summary>
/// Interface for parsing trace output
/// </summary>
internal interface ITraceParser
{
    bool TryParseTraceLine(string line, out TraceEntry entry);
}

/// <summary>
/// Interface for providing platform-specific hardware symbols
/// </summary>
internal interface IHardwareSymbolsProvider
{
    void InitializeSymbols(IRomDataParser romData);
}

/// <summary>
/// Interface for providing memory information
/// </summary>
internal interface IMemoryInformationProvider
{
    IEnumerable<IMemoryInformation> GetMemoryRegions();
}

/// <summary>
/// Type of memory region content
/// </summary>
internal enum MemoryRegionType
{
    Code,     // Contains executable code
    Data,     // Contains only data
    Mixed     // Can contain both
}

/// <summary>
/// Interface for describing a memory region
/// </summary>
internal interface IMemoryInformation
{
    /// <summary>
    /// Name used to identify this region in MAME debugger views
    /// </summary>
    string MameViewName { get; }
    
    /// <summary>
    /// Human-readable display name for UI (optional, defaults to MameViewName)
    /// </summary>
    string DisplayName => MameViewName;
    
    /// <summary>
    /// Whether this region contains physical data (true for ROM) or virtual data (false for RAM)
    /// Used to determine if data can be read directly
    /// </summary>
    bool HasPhysicalData => true;
    
    /// <summary>
    /// Type of content this region typically contains
    /// </summary>
    MemoryRegionType Type => MemoryRegionType.Mixed;
}
