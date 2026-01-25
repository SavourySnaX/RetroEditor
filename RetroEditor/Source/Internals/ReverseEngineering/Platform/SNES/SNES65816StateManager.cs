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

    public ICpuState ParseDebuggerState(LibMameDebugger debugger)
    {
        var pc = GetCPUState(debugger, "PC");
        var e = GetCPUState(debugger, "E");
        var p = GetCPUState(debugger, "P");

        return new SNES65816State
        {
            EmulationMode = e == 1,
            Accumulator8Bit = (p & 0x20) == 0x20,
            Index8Bit = (p & 0x10) == 0x10
        };
    }

    public void UpdateStateFromInstruction(ICpuState state, Instruction instruction)
    {
        // The SNES65816Disassembler already handles state updates during decoding
        // This method could be used for additional state tracking if needed
    }

    public string GetTraceFormat()
    {
        return "\"E=%02X|P=%02X|DB=%02X|D=%04X|X=%04X|Y=%04X|S=%04X|\",e,p,db,d,x,y,s";
    }

    public ICpuState CreateStateFromRegisters(UInt64 pc, Dictionary<string, UInt64> registers)
    {
        var e = registers.GetValueOrDefault("E", 1UL);
        var p = registers.GetValueOrDefault("P", 0x34UL);

        return new SNES65816State
        {
            EmulationMode = e == 1,
            Accumulator8Bit = (p & 0x20) == 0x20,
            Index8Bit = (p & 0x10) == 0x10
        };
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


    private UInt64 GetCPUState(LibMameDebugger debugger, string register)
    {
        var view = OpenCPUView(debugger, "maincpu");
        try
        {
            return ParseState(view, register);
        }
        finally
        {
            CloseView(debugger, view);
        }
    }

    private LibMameDebugger.DView OpenCPUView(LibMameDebugger debugger, string cpuName)
    {
        var view = new LibMameDebugger.DView(debugger.AllocView(LibMameDebuggerRetroPlugin.debug_view_type.State), 0, 0, 32, 32, "");

        // Find ROM source index
        int sourceCount = debugger.GetSourcesCount(ref view);
        var sources = debugger.GetSourcesList(ref view);

        for (int i = 0; i < sourceCount; i++)
        {
            if (sources[i].Contains(cpuName))
            {
                debugger.SetSource(ref view, i);
                break;
            }
        }

        return view;
    }

    private void CloseView(LibMameDebugger debugger, LibMameDebugger.DView view)
    {
        debugger.FreeView(view.view);
    }

    private UInt64 ParseState(LibMameDebugger.DView view, string register)
    {
        int bytesPerLine = view.view.W * 2; // Each character is 2 bytes (char + attribute)

        for (int y = 0; y < view.view.H; y++)
        {
            int lineStart = y * bytesPerLine;
            int x = 0;

            // Skip initial spaces
            while (x < view.view.W && (char)view.state[lineStart + x * 2] == ' ')
                x++;

            // Verify register name matches
            StringBuilder registerStr = new StringBuilder();
            while (x < view.view.W && (char)view.state[lineStart + x * 2] != ' ')
            {
                registerStr.Append((char)view.state[lineStart + x * 2]);
                x++;
            }
            if (registerStr.ToString() != register)
            {
                continue;
            }
            // Skip spaces between name and value
            while (x < view.view.W && (char)view.state[lineStart + x * 2] == ' ')
                x++;

            // Fetch Value
            registerStr.Clear();
            while (x < view.view.W && (char)view.state[lineStart + x * 2] != ' ')
            {
                registerStr.Append((char)view.state[lineStart + x * 2]);
                x++;
            }
            if (registerStr.Length > 0)
            {
                return UInt64.Parse(registerStr.ToString(), NumberStyles.HexNumber);
            }
        }
        return 0;
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