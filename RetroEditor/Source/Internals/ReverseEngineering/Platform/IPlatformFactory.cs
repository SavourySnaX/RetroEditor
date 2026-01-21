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
    IHardwareRegisterProvider CreateHardwareRegisterProvider();
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
    ICpuState ParseDebuggerState(LibMameDebugger debugger);
    void UpdateStateFromInstruction(ICpuState state, Instruction instruction);
    string GetTraceFormat();
    ICpuState CreateStateFromRegisters(UInt64 pc, Dictionary<string, UInt64> registers);
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
/// Interface for providing platform-specific hardware registers
/// </summary>
internal interface IHardwareRegisterProvider
{
    void InitializeRegisters(IRomDataParser romData);
}