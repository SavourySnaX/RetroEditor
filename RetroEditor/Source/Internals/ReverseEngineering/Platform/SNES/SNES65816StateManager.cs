using System.Globalization;
using System.Text;
using MyMGui;

namespace RetroEditor.Source.Internals.ReverseEngineering.Platform.SNES;

//
// Technically this is entirely 65816 specific, should be factored out later
//

/// <summary>
/// SNES/65816 CPU state manager
/// </summary>
internal class SNES65816StateManager : ICpuStateManager
{
    private bool cpu_emulationMode = true;
    private bool cpu_8bitAccumulator = true;
    private bool cpu_8bitIndex = true;

    public void UpdateStateFromInstruction(ICpuState state, Instruction instruction)
    {
        // The SNES65816Disassembler already handles state updates during decoding
        // This method could be used for additional state tracking if needed
    }

    public string GetTraceFormat()
    {
        return "\"E=%02X|P=%02X|DB=%02X|D=%04X|X=%04X|Y=%04X|S=%04X|\",e,p,db,d,x,y,s";
    }

    public ICpuState FetchStateFromUI()
    {
        return new SNES65816State
        {
            EmulationMode = cpu_emulationMode,
            Accumulator8Bit = cpu_8bitAccumulator,
            Index8Bit = cpu_8bitIndex
        };
    }
    public void UpdateUIFromState(ICpuState state)
    {
        if (state is SNES65816State snesState)
        {
            cpu_emulationMode = snesState.EmulationMode;
            cpu_8bitAccumulator = snesState.Accumulator8Bit;
            cpu_8bitIndex = snesState.Index8Bit;
        }
    }

    public void RenderUI()
    {
        // CPU-specific UI controls would be rendered by platform-specific components
        // For now, keep the existing UI but these should be abstracted later
        ImGui.SameLine();
        ImGui.Checkbox("EmulationMode", ref cpu_emulationMode);
        ImGui.SameLine();
        if (cpu_emulationMode)
        {
            ImGui.BeginDisabled();
        }
        ImGui.Checkbox("8Bit Accumulator", ref cpu_8bitAccumulator);
        ImGui.SameLine();
        ImGui.Checkbox("8Bit Index", ref cpu_8bitIndex);
        if (cpu_emulationMode)
        {
            ImGui.EndDisabled();
        }
    }

    public bool InstructionTerminatesAutoDisassembly(Instruction instruction)
    {
        // On SNES, the XCE instruction changes the CPU mode which should terminate auto-disassembly
        if (instruction.Mnemonic == "XCE")
        {
            return true;
        }
        return false;
    }

}