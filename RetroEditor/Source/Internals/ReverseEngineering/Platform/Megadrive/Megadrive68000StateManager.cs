using System.Globalization;
using System.Text;
using MyMGui;
using RetroEditor.Plugins;

namespace RetroEditor.Source.Internals.ReverseEngineering.Platform.Megadrive;

/// <summary>
/// Megadrive/Genesis 68000 CPU state manager
/// </summary>
internal class Megadrive68000StateManager : ICpuStateManager
{
    private bool cpu_68000_Supervisor = true;

    public void UpdateStateFromInstruction(ICpuState state, Instruction instruction)
    {
        // The 68000 doesn't have state-dependent instruction decoding like the 65816
        // This method could be used for additional state tracking if needed
    }

    public string GetTraceFormat()
    {
        // Format for MAME trace output for 68000
        // This will need to be adjusted based on actual MAME output format
        return "\"D0=%08X|D1=%08X|D2=%08X|D3=%08X|D4=%08X|D5=%08X|D6=%08X|D7=%08X|A0=%08X|A1=%08X|A2=%08X|A3=%08X|A4=%08X|A5=%08X|A6=%08X|A7=%08X|SR=%04X|\",d0,d1,d2,d3,d4,d5,d6,d7,a0,a1,a2,a3,a4,a5,a6,a7,sr";
    }

    public ICpuState FetchStateFromUI()
    {
        return new Megadrive68000State
        {
            SupervisorMode = cpu_68000_Supervisor
        };
    }

    public void UpdateUIFromState(ICpuState state)
    {
        if (state is Megadrive68000State mdState)
        {
            cpu_68000_Supervisor = mdState.SupervisorMode;
        }
    }
    
    public void RenderUI()
    {
        // CPU-specific UI controls would be rendered by platform-specific components
        // For now, keep the existing UI but these should be abstracted later
        ImGui.SameLine();
        ImGui.Checkbox("68000 Supervisor Mode", ref cpu_68000_Supervisor);
    }

    public bool InstructionTerminatesAutoDisassembly(Instruction instruction)
    {
        // On Megadrive/Genesis, entering Supervisor mode typically involves a system call
        // which should terminate auto-disassembly
        if (instruction.Mnemonic == "TRAP" || instruction.Mnemonic == "TRAPV")
        {
            return true;
        }
        return false;
    }
}
