using MyMGui;

namespace RetroEditor.Source.Internals.ReverseEngineering.Platform.ZXSpectrum;

/// <summary>
/// ZX Spectrum Z80 CPU state manager
/// </summary>
internal class ZXSpectrumZ80StateManager : ICpuStateManager
{
    private bool interruptMode = false;

    public void UpdateStateFromInstruction(ICpuState state, Instruction instruction)
    {
        // Z80 decoding in Z80Disassembler is not state-dependent here.
        // This could be expanded for IM modes if needed.
    }

    public string GetTraceFormat()
    {
        // Basic Z80 trace format including registers and PC
        return "\"AF=%04X|BC=%04X|DE=%04X|HL=%04X|IX=%04X|IY=%04X|SP=%04X|PC=%04X|\",af,bc,de,hl,ix,iy,sp,pc";
    }

    public ICpuState FetchStateFromUI()
    {
        return new Z80State { InterruptMode = interruptMode };
    }

    public void UpdateUIFromState(ICpuState state)
    {
        if (state is Z80State z80)
        {
            interruptMode = z80.InterruptMode;
        }
    }

    public void RenderUI()
    {
        ImGui.SameLine();
        ImGui.Checkbox("Interrupt Mode", ref interruptMode);
    }

    public bool InstructionTerminatesAutoDisassembly(Instruction instruction)
    {
        // RET instructions generally terminate a basic block
        return instruction.IsBasicBlockTerminator;
    }

    public string GetCPUArchitecture()
    {
        return "Zilog Z80";
    }
}
